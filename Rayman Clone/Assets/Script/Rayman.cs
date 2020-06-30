using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public enum SuperPower { None, MultiFist, ShockFist }
public enum SwingState { Weak, Normal, Charged }

public class Rayman : MonoBehaviour, IMovementInput, IInteractInput, IFireInput, ITargetInput, IToggleLeftInput, IToggleRightInput
{
    [SerializeField] CameraController cameraController;
    [SerializeField] UIManager uiManager;
    [SerializeField] MovementInput movementInput;

    [SerializeField] Hand leftHand;
    [SerializeField] Hand rightHand;
    private Hand currentHand;

    [SerializeField] GameObject handPrefab;

    [Header("Settings: ")]
    [Header("ArmSwing: ")]
    [SerializeField] float slowSwingDuration;
    [SerializeField] float fastSwingDuration;
    [SerializeField] float durationBeforeChargedSwing;
    [SerializeField, Range(0, 1)] float normalSwingThreshhold;
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
    private SwingState currentSwingState = SwingState.Weak;
    private SuperPower currentSuperPower = SuperPower.None;

    private float trailWidth;

    private List<Target> targets = new List<Target>();
    private List<Target> markedTargets = new List<Target>();
    private Target currentTarget;

    private Coroutine leftHandThrowRoutine;
    private Coroutine rightHandThrowRoutine;
    private Coroutine swingLeftHandRoutine;
    private Coroutine swingRightHandRoutine;

    private PlayerInputActions inputActions;

    public Vector3 MoveDirection { get; private set; }
    public bool IsPressingInteractButton { get; private set; }
    public bool IsPressingFireButton { get; private set; }
    public bool IsPressingTargetButton { get; private set; }
    public bool IsPressingToggleLeftButton { get; private set; }
    public bool IsPressingToggleRightButton { get; private set; }

    private void Awake()
    {
        inputActions = new PlayerInputActions();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        swingDuration = slowSwingDuration;

        leftHand.TrailRenderer.enabled = true;
        rightHand.TrailRenderer.enabled = true;
        currentHand = rightHand;

        trailWidth = leftHand.TrailRenderer.widthMultiplier;
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Interact.performed += OnInteractButton;
        inputActions.Player.Fire.performed += OnFireButton;
        inputActions.Player.Target.performed += OnTargetButton;
        inputActions.Player.Target.canceled += OnTargetButton;
        inputActions.Player.ToggleLeft.performed += OnToggleLeftButton;
        inputActions.Player.ToggleRight.performed += OnToggleRightButton;
    }

    private void OnDisable()
    {
        inputActions.Player.Interact.performed -= OnInteractButton;
        inputActions.Player.Fire.performed -= OnFireButton;
        inputActions.Player.Target.performed -= OnTargetButton;
        inputActions.Player.ToggleLeft.performed -= OnToggleLeftButton;
        inputActions.Player.ToggleRight.performed -= OnToggleRightButton;
        inputActions.Disable();
    }

    #region HandleInput

    private bool OnButtonPerformed(InputAction.CallbackContext _context, UnityAction _onDown, UnityAction _onUp)
    {
        var _value = _context.ReadValue<float>() >= InputSystem.settings.defaultDeadzoneMin;
        if (_value) { _onDown?.Invoke(); }
        else { _onUp?.Invoke(); }

        return _value;
    }

    private void OnInteractButton(InputAction.CallbackContext _context)
    {
        IsPressingInteractButton = OnButtonPerformed(_context, OnInteractButtonDown, OnInteractButtonUp);
    }
    private void OnInteractButtonDown()
    {
        MarkTarget(currentTarget);
    }
    private void OnInteractButtonUp()
    {

    }

    private void OnFireButton(InputAction.CallbackContext _context)
    {
        IsPressingFireButton = OnButtonPerformed(_context, OnFireButtonDown, OnFireButtonUp);
    }
    private void OnFireButtonDown()
    {
        isCharging = true;
    }
    private void OnFireButtonUp()
    {
        ThrowHand();
        ResetHandSwing();
        SwitchHand();
    }


    //?????

    private void OnTargetButton(InputAction.CallbackContext _context)
    {
        IsPressingTargetButton = OnButtonPerformed(_context, OnTargetButtonDown, OnTargetButtonUp);
    }
    private void OnTargetButtonDown()
    {
        TargetClosestTarget();
        if (currentTarget != null) { movementInput.blockRotationPlayer = true; uiManager.SetTargetUI(currentTarget.gameObject); }
        if (currentTarget != null) { cameraController.Focus(); }
    }
    private void OnTargetButtonUp()
    {
        ClearTargets();
        ClearMarketTargets();
        movementInput.blockRotationPlayer = false;
        uiManager.DisableTargetUI();
        cameraController.FreeLook();
    }

    private void OnToggleLeftButton(InputAction.CallbackContext _context)
    {
        IsPressingToggleLeftButton = OnButtonPerformed(_context, OnToggleLeftButtonDown, OnToggleLeftButtonUp);
    }
    private void OnToggleLeftButtonDown()
    {
        TargetClosestTargetFromCurrentTargetToDirection(-transform.right);
    }
    private void OnToggleLeftButtonUp()
    {

    }

    private void OnToggleRightButton(InputAction.CallbackContext _context)
    {
        IsPressingToggleRightButton = OnButtonPerformed(_context, OnToggleRightButtonDown, OnToggleRightButtonUp);
    }
    private void OnToggleRightButtonDown()
    {
        TargetClosestTargetFromCurrentTargetToDirection(transform.right);
    }
    private void OnToggleRightButtonUp()
    {

    }


    #endregion

    private void Update()
    {
        //verzien hier ook ff een nettere oplossing voor
        if (IsPressingTargetButton && currentTarget != null)
        {
            float _hor = Input.GetAxis("Horizontal");

            if (_hor == 0) { currentAimMode = UIManager.ArrowAimMode.Middle; }
            else if (_hor > horizontalMarginBeforeSideAim) { currentAimMode = UIManager.ArrowAimMode.Right; }
            else if (_hor < -horizontalMarginBeforeSideAim) { currentAimMode = UIManager.ArrowAimMode.Left; }
            uiManager.SetTargetUI(currentTarget.gameObject, currentAimMode);
        }

        if (movementInput.blockRotationPlayer && currentTarget != null) { transform.LookAt(currentTarget.transform); }

        if (isCharging) { SwingHand(); }

    }

    #region COMBAT
    private void SwingHand()
    {
        //Swing the arm
        if (swingLerpTime < 1)
        {
            swingLerpTime += Time.deltaTime / swingDuration;
            currentHand.SetShoulderLocalRotation(Vector3.Lerp(Vector3.zero, -Vector3.right * 360, swingLerpTime));
        }
        else { swingLerpTime = 0; }

        //Charge the swing
        if (chargeLerpTime < 1)
        {
            chargeLerpTime += Time.deltaTime / durationBeforeChargedSwing;

            float _evaluatedChargeLerpTime = chargeCurve.Evaluate(chargeLerpTime);
            swingDuration = Mathf.Lerp(slowSwingDuration, fastSwingDuration, _evaluatedChargeLerpTime);
        }

        //Determine current swing state
        if (chargeLerpTime >= 1) { currentSwingState = SwingState.Charged; }
        else if (chargeLerpTime >= normalSwingThreshhold) { currentSwingState = SwingState.Normal; }
        else { currentSwingState = SwingState.Weak; }
    }

    private void ResetHandSwing()
    {
        isCharging = false;
        swingLerpTime = chargeLerpTime = 0;
        swingDuration = slowSwingDuration;

        leftHand.SetShoulderLocalRotation(Vector3.left * 45);
        rightHand.SetShoulderLocalRotation(Vector3.left * 45);
    }

    private void ThrowHand()
    {
        StartCoroutine(IEThrowHand(currentAimMode));
    }

    private IEnumerator IEThrowHand(UIManager.ArrowAimMode _aimMode)
    {
        //Save current hand and target so things don't get screwed up when they are changed mid throw
        Hand _currentHand = currentHand;
        Target _target = currentTarget;

        //Instantiate the hand projectile
        GameObject _handProjectile = Instantiate(handPrefab, _currentHand.GameObject.transform.position, handPrefab.transform.rotation);

        Vector3 _startPos = _handProjectile.transform.position;
        Vector3 _curHandPos = _handProjectile.transform.position;
        Vector3 _targetPos;

        if (currentTarget == null) { _targetPos = transform.position + transform.forward * handThrowDistance; }
        else { _targetPos = _target.transform.position; }

        float _distanceToTarget = Vector3.Distance(_startPos, _targetPos);

        float _lerpTime = 0;
        while (_lerpTime < 1)
        {
            _lerpTime += Time.deltaTime / ((handThrowDuration / 2f) * (_aimMode == UIManager.ArrowAimMode.Middle ? 1f : 2f));
            float _evaluatedLerpTime = handThrowCurve.Evaluate(_lerpTime);

            Vector3 p1 = (_startPos / 2f) + (_targetPos / 2f);

            switch (_aimMode)
            {
                default:
                case UIManager.ArrowAimMode.Middle:
                    _handProjectile.transform.position = Vector3.Lerp(_startPos, _targetPos, _evaluatedLerpTime);
                    break;
                case UIManager.ArrowAimMode.Left:
                    p1 += -transform.right * _distanceToTarget;
                    _handProjectile.transform.position = CalcQuadraticBezierCurve(_startPos, p1, _targetPos, _evaluatedLerpTime);
                    break;
                case UIManager.ArrowAimMode.Right:
                    p1 += transform.right * _distanceToTarget;
                    _handProjectile.transform.position = CalcQuadraticBezierCurve(_startPos, p1, _targetPos, _evaluatedLerpTime);
                    break;
            }

            yield return null;
        }

        _target?.GetHit();

        switch (currentSuperPower)
        {
            default:
            case SuperPower.None:
                break;
            case SuperPower.MultiFist:
                if (markedTargets.Count != 0)
                {
                    //Create a reversed list so we hit the targets in the right order
                    List<Target> _reversedTargets = new List<Target>();
                    _reversedTargets.AddRange(markedTargets);
                    _reversedTargets.Reverse();

                    //Hit each marked target
                    foreach (var _markedTarget in _reversedTargets)
                    {
                        //Prevent hitting the original target again
                        if(_markedTarget == _target) { continue; }

                        //Reset values
                        _curHandPos = _handProjectile.transform.position;
                        _lerpTime = 0;

                        //Go to marked target
                        while (_lerpTime < 1)
                        {
                            _lerpTime += Time.deltaTime / (handThrowDuration / markedTargets.Count);
                            float _evaluatedLerpTime = handThrowCurve.Evaluate(_lerpTime);

                            _handProjectile.transform.position = Vector3.Lerp(_curHandPos, _markedTarget.transform.position, _evaluatedLerpTime);
                            yield return null;
                        }

                        //Hit the target
                        _markedTarget.GetHit();
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                break;
            case SuperPower.ShockFist:
                break;
        }

        _curHandPos = _handProjectile.transform.position;
        _lerpTime = 0;

        while (_lerpTime < 1)
        {
            _handProjectile.GetComponent<TrailRenderer>().widthMultiplier = trailWidth / 3f;

            _lerpTime += Time.deltaTime / (handThrowDuration / 2f);
            float _evaluatedLerpTime = handThrowCurve.Evaluate(_lerpTime);

            _handProjectile.transform.position = Vector3.Lerp(_curHandPos, _currentHand.GameObject.transform.position, _evaluatedLerpTime);
            yield return null;
        }

        Destroy(_handProjectile);
        yield return null;
    }

    private void SwitchHand()
    {
        isLeft = isLeft ? false : true;
        currentHand = isLeft ? leftHand : rightHand;
    }
    #endregion

    private void ClearTargets()
    {
        targets.Clear();
        currentTarget = null;
    }

    private void ClearMarketTargets()
    {
        foreach (var _target in markedTargets)
        {
            _target.UnMark();
        }

        markedTargets.Clear();
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

    private List<Target> ScanForTargets()
    {
        RaycastHit[] _hits = Physics.SphereCastAll(transform.position, targetScanRange, transform.forward, targetScanRange, targetLayer.value);

        targets.Clear();

        foreach (var _hit in _hits)
        {
            Target _target = _hit.transform.GetComponent<Target>();
            if (_target != null) { targets.Add(_target); }
        }

        return targets;
    }

    private void TargetClosestTargetFromCurrentTargetToDirection(Vector3 _direction)
    {
        if (currentTarget == null) { return; }

        RaycastHit[] _hits = Physics.SphereCastAll(currentTarget.transform.position + _direction, 3, _direction, 1, targetLayer.value);
        foreach (var _hit in _hits)
        {
            Target _target = _hit.transform.GetComponent<Target>();
            if (_target != null && _target != currentTarget)
            {
                currentTarget = _target; return;
            }
        }
    }

    private void MarkTarget(Target _target)
    {
        if (!markedTargets.Contains(_target)) { _target.Mark(); markedTargets.Add(_target); }
    }

    private void OnDrawGizmos()
    {
        if (currentTarget == null) { return; }

        Gizmos.color = Color.red;
        Gizmos.DrawLine(currentTarget.transform.position + -transform.right, currentTarget.transform.position + -transform.right + (-transform.right * 1));

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(currentTarget.transform.position + transform.right, currentTarget.transform.position + transform.right + (transform.right * 1));
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

    private void OnTriggerEnter(Collider other)
    {
        SuperPowerCan _can = other.GetComponent<SuperPowerCan>();

        Debug.Log("Collision with " + other.gameObject.name);

        if(_can != null) { currentSuperPower = _can.SuperPower; }     
    }
}

[System.Serializable]
public class Hand
{
    public GameObject Shoulder;
    public GameObject GameObject;
    public TrailRenderer TrailRenderer;

    public void SetShoulderLocalRotation(Vector3 _eulerAngles)
    {
        Shoulder.transform.localEulerAngles = _eulerAngles;
    }
}
