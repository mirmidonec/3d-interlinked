using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class PlayerInteract : MonoBehaviour
{
    [SerializeField] public Text _interactionText;
    [SerializeField] public TMP_Text _guidText;
    [SerializeField] private float _typingSpeed;

    public PlayerController Player;


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
        if (CameraTranzition.InTransition || CameraTranzition.InPC)
        {
            _interactionText.text = "";
            _guidText.text = " ";
            return;
        }
        if (Player.IsDialogue)
        {
            _guidText.text = " ";
        }
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
                if (newInteractable.GetComponent<Chair>() && Player.isSitting)
                {
                    _guidText.gameObject.SetActive(false);
                }
                else
                {
                    _guidText.gameObject.SetActive(true);
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
            else if (Input.GetButtonDown("Fire2"))
            {
                _currentInteractable.OnInteractAlt();
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

        _guidText.gameObject.SetActive(false);

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