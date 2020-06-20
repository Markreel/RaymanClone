using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rayman : MonoBehaviour
{
    [SerializeField] CameraController cameraController;
    [SerializeField] UIManager uiManager;
    [SerializeField] MovementInput movementInput;

    [SerializeField] GameObject leftShoulder;
    [SerializeField] GameObject leftHand;
    [SerializeField] GameObject leftHandPos;

    [SerializeField] GameObject rightShoulder;
    [SerializeField] GameObject rightHand;
    [SerializeField] GameObject rightHandPos;

    [Header("Settings: ")]
    [Header("ArmSwing: ")]
    [SerializeField] float slowSwingDuration;
    [SerializeField] float fastSwingDuration;
    [SerializeField] float durationBeforeFullyCharged;
    [SerializeField] AnimationCurve chargeCurve;
    private float swingDuration;

    [Header("HandThrow: ")]
    [SerializeField] float handThrowDistance;
    [SerializeField] float handThrowDuration;
    [SerializeField] AnimationCurve handThrowCurve;

    [Header("Target")]
    [SerializeField] LayerMask targetLayer;
    [SerializeField] float targetScanRange;
    [SerializeField] float horizontalMarginBeforeSideAim = 0.15f;

    bool isLeft = false;

    private float swingLerpTime = 0;
    private float chargeLerpTime = 0;
    private bool isCharging;
    private UIManager.ArrowAimMode currentAimMode = UIManager.ArrowAimMode.Middle;

    private float trailWidth;

    private List<GameObject> targets = new List<GameObject>();
    private GameObject currentTarget;

    private Coroutine leftHandThrowRoutine;
    private Coroutine rightHandThrowRoutine;
    private Coroutine swingLeftHandRoutine;
    private Coroutine swingRightHandRoutine;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        swingDuration = slowSwingDuration;

        leftHand.GetComponent<TrailRenderer>().enabled = true;
        rightHand.GetComponent<TrailRenderer>().enabled = true;

        trailWidth = leftHand.GetComponent<TrailRenderer>().widthMultiplier;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isCharging = true;
            ResetHand(isLeft, isLeft ? leftHand : rightHand);
            //leftHand.GetComponent<TrailRenderer>().enabled = isLeft ? true : false;
            //rightHand.GetComponent<TrailRenderer>().enabled = isLeft ? false : true;

            leftShoulder.transform.localEulerAngles = rightShoulder.transform.localEulerAngles = Vector3.left * 45;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            ResetHand(isLeft, isLeft ? leftHand : rightHand);
            swingLerpTime = chargeLerpTime = 0;
            swingDuration = slowSwingDuration;
            leftShoulder.transform.localEulerAngles = rightShoulder.transform.localEulerAngles = Vector3.left * 45;

            isCharging = false;
            ThrowHand(isLeft ? true : false);
            SwitchHand();
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            TargetClosestTarget();
            if (currentTarget != null) { movementInput.blockRotationPlayer = true; uiManager.SetTargetUI(currentTarget); }
            //if (currentTarget != null) { cameraController.FocusOnTarget(currentTarget.transform); }
        }
        if (Input.GetKey(KeyCode.LeftShift) && currentTarget != null)
        {
            float _hor = Input.GetAxis("Horizontal");

            if (_hor == 0) { currentAimMode = UIManager.ArrowAimMode.Middle; }
            else if (_hor > horizontalMarginBeforeSideAim) { currentAimMode = UIManager.ArrowAimMode.Right; }
            else if (_hor < -horizontalMarginBeforeSideAim) { currentAimMode = UIManager.ArrowAimMode.Left; }
            uiManager.SetTargetUI(currentTarget, currentAimMode);
        }
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            ClearTargets();
            movementInput.blockRotationPlayer = false;
            uiManager.DisableTargetUI();
            //cameraController.FreeLook();
        }

        if (movementInput.blockRotationPlayer && currentTarget != null) { transform.LookAt(currentTarget.transform); }


        if (isCharging == true)
        {
            SwingHand();
        }
    }

    #region COMBAT
    private void SwingHand()
    {
        GameObject _targetShoulder = isLeft ? leftShoulder : rightShoulder;
        if (swingLerpTime < 1)
        {
            swingLerpTime += Time.deltaTime / swingDuration;
            _targetShoulder.transform.localEulerAngles = Vector3.Lerp(Vector3.zero, -Vector3.right * 360, swingLerpTime);
        }
        else
        {
            swingLerpTime = 0;
        }

        if (chargeLerpTime < 1)
        {
            chargeLerpTime += Time.deltaTime / durationBeforeFullyCharged;

            float _evaluatedChargeLerpTime = chargeCurve.Evaluate(chargeLerpTime);
            swingDuration = Mathf.Lerp(slowSwingDuration, fastSwingDuration, _evaluatedChargeLerpTime);
        }
    }

    private void ThrowHand(bool _isLeft)
    {
        //gooi hand naar target (maak los van lichaam en yeet op basis van power)

        Vector3 _targetPos;
        if (currentTarget == null) { _targetPos = transform.position + transform.forward * handThrowDistance; }
        else { _targetPos = currentTarget.transform.position; }

        if (_isLeft)
        {
            if (leftHandThrowRoutine != null) { StopCoroutine(leftHandThrowRoutine); }
            leftHandThrowRoutine = StartCoroutine(IEThrowHand(_isLeft, leftHand, _targetPos, currentAimMode));
        }
        else
        {
            if (rightHandThrowRoutine != null) { StopCoroutine(rightHandThrowRoutine); }
            rightHandThrowRoutine = StartCoroutine(IEThrowHand(_isLeft, rightHand, _targetPos, currentAimMode));
        }
    }

    private IEnumerator IEThrowHand(bool _isLeft, GameObject _hand, Vector3 _targetPos, UIManager.ArrowAimMode _aimMode)
    {
        Vector3 _startPos = _hand.transform.position;
        Vector3 _endPos = _startPos + transform.forward * 5;

        _hand.transform.parent = null;

        float _lerpTime = 0;
        while (_lerpTime < 1)
        {
            _lerpTime += Time.deltaTime / ((handThrowDuration / 2f) * (_aimMode == UIManager.ArrowAimMode.Middle ? 1f : 2f));
            float _evaluatedLerpTime = handThrowCurve.Evaluate(_lerpTime);

            Vector3 p1 = (_startPos / 2) + (_targetPos / 2);

            switch (_aimMode)
            {
                default:
                case UIManager.ArrowAimMode.Middle:
                    _hand.transform.position = Vector3.Lerp(_startPos, _targetPos, _evaluatedLerpTime);
                    break;
                case UIManager.ArrowAimMode.Left:
                    p1 += -transform.right * 5;
                    _hand.transform.position = CalcQuadraticBezierCurve(_startPos, p1, _targetPos, _evaluatedLerpTime);
                    break;
                case UIManager.ArrowAimMode.Right:
                    p1 += transform.right * 5;
                    _hand.transform.position = CalcQuadraticBezierCurve(_startPos, p1, _targetPos, _evaluatedLerpTime);
                    break;
            }

            yield return null;
        }

        _lerpTime = 0;
        while (_lerpTime < 1)
        {
            _hand.GetComponent<TrailRenderer>().widthMultiplier = trailWidth / 3f;

            _lerpTime += Time.deltaTime / (handThrowDuration / 2f);
            float _evaluatedLerpTime = handThrowCurve.Evaluate(_lerpTime);

            _hand.transform.position = Vector3.Lerp(_targetPos, _isLeft ? leftHandPos.transform.position : rightHandPos.transform.position, _evaluatedLerpTime);
            yield return null;
        }

        _hand.transform.parent = _isLeft ? leftHandPos.transform : rightHandPos.transform;
        _hand.transform.localPosition = _hand.transform.localEulerAngles = Vector3.zero;

        yield return null;
    }

    private void ResetHand(bool _isLeft, GameObject _hand)
    {
        _hand.GetComponent<TrailRenderer>().enabled = false;
        _hand.transform.parent = _isLeft ? leftHandPos.transform : rightHandPos.transform;
        _hand.transform.localPosition = _hand.transform.localEulerAngles = Vector3.zero;
        _hand.GetComponent<TrailRenderer>().Clear();
        _hand.GetComponent<TrailRenderer>().enabled = true;
        _hand.GetComponent<TrailRenderer>().widthMultiplier = trailWidth;
    }

    private void SwitchHand()
    {
        //zet nieuwe hand terug op plek
        isLeft = isLeft ? false : true;
    }
    #endregion

    private void ClearTargets()
    {
        targets.Clear();
        currentTarget = null;
    }

    private void TargetClosestTarget()
    {
        ScanForTargets();

        float _lowestDistance = Mathf.Infinity;

        foreach (var _target in targets)
        {
            float _dis = Vector3.Distance(transform.position, _target.transform.position);

            if (_dis < _lowestDistance) { currentTarget = _target; _lowestDistance = _dis; }
        }
    }

    private List<GameObject> ScanForTargets()
    {
        RaycastHit[] _hits = Physics.SphereCastAll(transform.position, targetScanRange, transform.forward, targetScanRange, targetLayer.value);

        targets.Clear();

        foreach (var _hit in _hits)
        {
            targets.Add(_hit.transform.gameObject);
        }

        return targets;
    }

    private Vector3 CalcQuadraticBezierCurve(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        Vector3 p = uu * p0;
        p += 2 * u * t * p1;
        p += tt * p2;
        return p;
    }
}
