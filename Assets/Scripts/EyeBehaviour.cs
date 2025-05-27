using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeBehaviour : MonoBehaviour
{
    [Header("Eye Structure")]
    [SerializeField] private RectTransform _eyeball;
    [SerializeField] private RectTransform _iris;
    [SerializeField] private RectTransform _pupil;
    [Header("Movement Settings")]
    [SerializeField] private float _irisMoveRadius = 10f;
    [SerializeField] private float _edgeOffset = 5f;
    [SerializeField] private float _smoothTime = 0.1f;
    [Header("Pupil Dilation")]
    [SerializeField] private float _dilationSpeed = 5f;
    [SerializeField] private float _minPupilSize = 0.8f;
    [SerializeField] private float _maxPupilSize = 1.2f;
    [Header("Blinking Settings")]
    [SerializeField] private float _minBlinkInterval = 5f;
    [SerializeField] private float _maxBlinkInterval = 10f;
    [SerializeField] private float _blinkDuration = 0.1f;

    private Vector2 _irisVelocity;
    private float _currentDilation = 0.5f;
    private float _targetDilation = 0.5f;
    private float _eyeballRadius;
    private bool _isBlinking;
    private Vector3 _eyeballOpenScale;

    private void Start()
    {
        _eyeballRadius = Mathf.Min(_eyeball.rect.width, _eyeball.rect.height) * 0.5f;
        _eyeballOpenScale = _eyeball.localScale;
        StartCoroutine(BlinkRoutine());
    }

    private void Update()
    {
        if (_isBlinking)
            return;

        TrackMouse();
        AnimatePupil();
    }

    private void TrackMouse()
    {
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _eyeball,
            Input.mousePosition,
            Camera.main,
            out localMousePos);

        float irisRadius = _iris.rect.width * 0.5f;
        float maxIrisRadius = Mathf.Clamp(_eyeballRadius - irisRadius - _edgeOffset, 0, _irisMoveRadius);
        Vector2 clampedPos = Vector2.ClampMagnitude(localMousePos, maxIrisRadius);

        _iris.anchoredPosition = Vector2.SmoothDamp(_iris.anchoredPosition, clampedPos, ref _irisVelocity, _smoothTime);
        _pupil.anchoredPosition = _iris.anchoredPosition;
    }

   private void AnimatePupil()
{
    _currentDilation = Mathf.Lerp(_currentDilation, _targetDilation, Time.deltaTime * _dilationSpeed);
    _pupil.localScale = new Vector3(_currentDilation, _currentDilation, 1f);
}

    public void SetPupilTarget(float dilation)
    {
        _targetDilation = Mathf.Clamp01(dilation);
    }

    private IEnumerator BlinkRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(_minBlinkInterval, _maxBlinkInterval));
            yield return BlinkAnimation();
        }
    }

    private IEnumerator BlinkAnimation()
    {
        _isBlinking = true;

        Vector3 closedScale = new Vector3(_eyeballOpenScale.x, 0f, _eyeballOpenScale.z);

        float elapsed = 0f;
        while (elapsed < _blinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _blinkDuration;
            _eyeball.localScale = Vector3.Lerp(_eyeballOpenScale, closedScale, t);
            yield return null;
        }
        _eyeball.localScale = closedScale;

        yield return new WaitForSeconds(0.05f);

        elapsed = 0f;
        while (elapsed < _blinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _blinkDuration;
            _eyeball.localScale = Vector3.Lerp(closedScale, _eyeballOpenScale, t);
            yield return null;
        }
        _eyeball.localScale = _eyeballOpenScale;

        _isBlinking = false;
    }
}
