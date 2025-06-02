using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Box : Interactable
{
    [SerializeField] private float _openZ;
    [SerializeField] private float _closedZ;
    [SerializeField] private float _duration;
    [SerializeField] private AudioClip _openSound;

    private bool _isOpen = false;
    private Coroutine _currentCoroutine;
    private AudioSource _source;


    protected override void Awake()
    {
        base.Awake();
        _source = GetComponentInParent<AudioSource>();

    }

    public override void OnFocus()
    {
        Message = "Shelf";
        _outline.enabled = true;
    }

    public override void OnInteract()
    {
        ToggleDrawer();
        _source.PlayOneShot(_openSound);
    }

     public override void OnLoseFocus()
    {
        _outline.enabled = false;
    }

    public void ToggleDrawer()
    {
        if (_currentCoroutine != null)
            StopCoroutine(_currentCoroutine);

        float targetZ = _isOpen ? _closedZ : _openZ;
        _currentCoroutine = StartCoroutine(MoveDrawer(targetZ));
        _isOpen = !_isOpen;
    }

    private IEnumerator MoveDrawer(float endZ)
    {
        float elapsed = 0f;
        Vector3 startPos = transform.localPosition;
        Vector3 endPos = new Vector3(startPos.x, startPos.y, endZ);

        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _duration);
            transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        transform.localPosition = endPos;
        _currentCoroutine = null;
    }
}
