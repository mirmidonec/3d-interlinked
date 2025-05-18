using System.Collections;
using UnityEngine;

public class TextShaker : MonoBehaviour
{
    [Header("Shake Settings")]
    [SerializeField] private float _shakeDelay;

    [SerializeField] private float _posXMin;
    [SerializeField] private float _posXMax;

    [SerializeField] private float _posZMin;
    [SerializeField] private float _posZMax;

    private bool _isShaking = true;
    private GameObject _textObject;
    private Coroutine _shakeCoroutine;

    private void Awake()
    {
        if (_textObject == null)
        {
            _textObject = gameObject;
        }
    }

    private void OnEnable()
    {
        StartShaking();
    }

    private void OnDisable()
    {
        StopShaking();
    }

    public void StartShaking()
    {
        if (_shakeCoroutine == null)
        {
            _isShaking = true;
            _shakeCoroutine = StartCoroutine(ShakeRoutine());
        }
    }

    public void StopShaking()
    {
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = null;
        }

        _isShaking = false;

        if (_textObject != null)
        {
            Vector3 localPos = _textObject.transform.localPosition;
            _textObject.transform.localPosition = new Vector3(0f, localPos.y, 0f);
        }
    }

    private IEnumerator ShakeRoutine()
    {
        while (_isShaking)
        {
            float randomX = Random.Range(_posXMin, _posXMax);
            float randomZ = Random.Range(_posZMin, _posZMax);

            if (_textObject != null)
            {
                Vector3 currentPos = _textObject.transform.localPosition;
                _textObject.transform.localPosition = new Vector3(randomX, currentPos.y, randomZ);
            }

            yield return new WaitForSeconds(_shakeDelay);
        }
    }
}
