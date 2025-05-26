using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Door : Interactable
{
 [Header("Open Settings")]
    [SerializeField] private float _openAngle = 90f;
    [SerializeField] private float _openSpeed = 90f;
    [SerializeField] private Transform _hingeTransform;
    [SerializeField] private AudioClip _openSound;

    private bool _isOpen = false;
    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private Coroutine _doorCoroutine;
    private AudioSource _source;

    public override void OnFocus()
    {
        Message = "Door";
        _outline.enabled = true;
    }

    public override void OnInteract()
    {
        ToggleDoor();
    }

    public override void OnLoseFocus()
    {
        _outline.enabled = false;
    }

    protected override void Awake()
    {
        base.Awake();
        _closedRotation = _hingeTransform.localRotation;
        _openRotation = Quaternion.Euler(
            _closedRotation.eulerAngles.x,
            _closedRotation.eulerAngles.y + _openAngle,
            _closedRotation.eulerAngles.z
        );
        _source = GetComponent<AudioSource>();
    }

    private void ToggleDoor()
    {
        _isOpen = !_isOpen;

        if (_doorCoroutine != null)
            StopCoroutine(_doorCoroutine);

        _doorCoroutine = StartCoroutine(AnimateDoor());

        _source.PlayOneShot(_openSound);
    }

    private IEnumerator AnimateDoor()
    {
        Quaternion targetRotation = _isOpen ? _openRotation : _closedRotation;

        while (Quaternion.Angle(_hingeTransform.localRotation, targetRotation) > 0.1f)
        {
            _hingeTransform.localRotation = Quaternion.RotateTowards(
                _hingeTransform.localRotation,
                targetRotation,
                _openSpeed * Time.deltaTime
            );
            yield return null;
        }

        _hingeTransform.localRotation = targetRotation;
    }
}
