using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Door : Interactable
{
    [Header("Open Settings")]
    [SerializeField] private float _openAngle;
    [SerializeField] private float _openSpeed;
    [SerializeField] private Transform _hingeTransform;
    [SerializeField] private AudioClip _openSound;

    private bool _isOpen = false;
    private Quaternion _closedRotation;
    private Quaternion _targetRotation;
    private Coroutine _doorCoroutine;
    private AudioSource _source;

    public override void OnFocus()
    {
        Message = "Door";
        _outline.ActiveOutline();
    }

    public override void OnInteract()
    {
        ToggleDoor();
        _outline.ActivatedOutline();
    }

    public override void OnLoseFocus()
    {
        _outline.DisableOutline();
    }

    protected override void Awake()
    {
        base.Awake();
        _closedRotation = _hingeTransform.localRotation;
        _source = GetComponent<AudioSource>();
    }

    private void ToggleDoor()
    {
        _isOpen = !_isOpen;

        if (_isOpen)
        {
            SetOpenRotation();
        }
        else
        {
            _targetRotation = _closedRotation;
        }
        if (_doorCoroutine != null)
            StopCoroutine(_doorCoroutine);

        _doorCoroutine = StartCoroutine(AnimateDoor());
        _source.PlayOneShot(_openSound);
    }

    private void SetOpenRotation()
    {
        Vector3 toPlayer = (Camera.main.transform.position - _hingeTransform.position).normalized;
        Vector3 hingeForward = _hingeTransform.forward;

        float dot = Vector3.Dot(toPlayer, hingeForward);
        float angle = dot > 0 ? -_openAngle : _openAngle;

        _targetRotation = Quaternion.Euler(
            _closedRotation.eulerAngles.x,
            _closedRotation.eulerAngles.y + angle,
            _closedRotation.eulerAngles.z
        );
    }

    private IEnumerator AnimateDoor()
    {
        while (Quaternion.Angle(_hingeTransform.localRotation, _targetRotation) > 0.1f)
        {
            _hingeTransform.localRotation = Quaternion.RotateTowards(
                _hingeTransform.localRotation,
                _targetRotation,
                _openSpeed * Time.deltaTime
            );
            yield return null;
        }

        _hingeTransform.localRotation = _targetRotation;
    }

}
