using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class PlayerInteract : MonoBehaviour
{

    [SerializeField] private Text _interactionText;
    [SerializeField] private float _typingSpeed;


    private Coroutine _typingCoroutine;
    private Camera _mainCamera;
    private Interactable _currentInteractable;

    private void Awake()
    {
        _mainCamera = GetComponent<Camera>();
        if (_interactionText != null)
            _interactionText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Physics.Raycast(_mainCamera.transform.position, _mainCamera.transform.forward, out RaycastHit hit, PlayerEntity.Instance.InteractDistance))
        {
            HandleRaycastHit(hit);
        }
        else
        {
            ClearCurrentInteractable();
        }
    }

    private void HandleRaycastHit(RaycastHit hit)
    {
        Interactable newInteractable = hit.collider.GetComponent<Interactable>();

        if (newInteractable != null)
        {
            if (_currentInteractable != newInteractable)
            {
                if (_currentInteractable != null)
                {
                    _currentInteractable.OnLoseFocus();
                    _currentInteractable.OnTextChanged -= UpdateInteractionText;
                }

                _currentInteractable = newInteractable;
                _currentInteractable.OnFocus();

                _currentInteractable.OnTextChanged += UpdateInteractionText;

                _interactionText.gameObject.SetActive(true);

                if (_typingCoroutine != null)
                    StopCoroutine(_typingCoroutine);

                _typingCoroutine = StartCoroutine(TypeText(_currentInteractable.Message));
            }

            if (Input.GetButtonDown("Fire1"))
            {
                _currentInteractable.OnInteract();
            }
        }
        else
        {
            ClearCurrentInteractable();
        }
    }

    private IEnumerator TypeText(string fullText)
    {
        _interactionText.text = "";
        foreach (char c in fullText)
        {
            _interactionText.text += c;
            yield return new WaitForSeconds(_typingSpeed);
        }
    }

    private void UpdateInteractionText(string newText)
    {
        if (_typingCoroutine != null)
            StopCoroutine(_typingCoroutine);

        _typingCoroutine = StartCoroutine(TypeText(newText));
    }

    private void ClearCurrentInteractable()
    {
        if (_currentInteractable == null) return;

        _currentInteractable.OnLoseFocus();
        _currentInteractable.OnTextChanged -= UpdateInteractionText;

        if (_interactionText != null)
        {
            _interactionText.gameObject.SetActive(false);
            _interactionText.text = "";
        }

        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        _currentInteractable = null;
    }

}