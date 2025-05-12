using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class PlayerInteract : MonoBehaviour
{
    private Camera _mainCamera;
    private IInteractable _currentInteractable;
    public Text InteractionText;

    private void Awake()
    {
        _mainCamera = GetComponent<Camera>();
        if (InteractionText != null)
            InteractionText.gameObject.SetActive(false);
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
        IInteractable newInteractable = hit.collider.GetComponent<IInteractable>();

        if (newInteractable != null)
        {
            if (_currentInteractable != newInteractable)
            {
                _currentInteractable?.OnLoseFocus();
                _currentInteractable = newInteractable;
                _currentInteractable.OnFocus();
            }

            if (InteractionText != null)
                InteractionText.gameObject.SetActive(true);

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

    private void ClearCurrentInteractable()
    {
        if (_currentInteractable == null) return;

        _currentInteractable.OnLoseFocus();
        if (InteractionText != null)
            InteractionText.gameObject.SetActive(false);

        _currentInteractable = null;
    }
}
