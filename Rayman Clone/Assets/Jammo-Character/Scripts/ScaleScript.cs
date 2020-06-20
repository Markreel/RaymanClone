using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleScript : MonoBehaviour
{
    [SerializeField] private bool loop;

    [SerializeField] private bool doX;
    [SerializeField] private bool doY;
    [SerializeField] private bool doZ;

    [SerializeField] private Vector2 scaleX;
    [SerializeField] private Vector2 scaleY;
    [SerializeField] private Vector2 scaleZ;

    [SerializeField] private float durationX;
    [SerializeField] private float durationY;
    [SerializeField] private float durationZ;

    [SerializeField] private AnimationCurve scaleXCurve;
    [SerializeField] private AnimationCurve scaleYCurve;
    [SerializeField] private AnimationCurve scaleZCurve;

    private Vector3 newScale;

    private Coroutine xRoutine;
    private Coroutine yRoutine;
    private Coroutine zRoutine;

    private void OnEnable()
    {
        newScale = transform.lossyScale;

        if (doX) StartScale(scaleX, scaleXCurve, durationX, xRoutine, 0);
        if (doY) StartScale(scaleY, scaleYCurve, durationY, yRoutine, 1);
        if (doZ) StartScale(scaleZ, scaleZCurve, durationZ, zRoutine, 2);
    }

    private void StartScale(Vector2 _scale, AnimationCurve _curve, float _duration, Coroutine _routine, int _index) {

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(IEScale(_scale, _curve, _duration, _routine, _index));
    }

    private IEnumerator IEScale(Vector2 _scale, AnimationCurve _curve, float _duration, Coroutine _routine, int _index) {

        float _timeValue = 0;

        while (_timeValue < 1) {

            _timeValue += Time.deltaTime / _duration;

            float _timeKey = _curve.Evaluate(_timeValue);
            float _newScale = Mathf.Lerp(_scale.x, _scale.y, _timeKey);

            switch (_index) {
                default:
                case 0:
                    newScale = new Vector3(_newScale, newScale.y, newScale.z);
                    break;
                case 1:
                    newScale = new Vector3(newScale.x, _newScale, newScale.z);
                    break;
                case 2:
                    newScale = new Vector3(newScale.x, newScale.y, _newScale);
                    break;
            }

            transform.localScale = newScale;

            yield return null;
        }

        if (loop) StartScale(_scale, _curve, _duration, _routine, _index);

        yield return null;
    }
}
