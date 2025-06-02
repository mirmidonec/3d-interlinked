using EZCameraShake;
using UnityEngine;
using System.Collections;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class PlayerShoot : MonoBehaviour
{

    [Header("References")]
    [SerializeField] private Transform _weaponTransform;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private TMP_Text _ammoText;

    [Header("Bob Settings")]
    [SerializeField] private float _bobSpeed = 6f;
    [SerializeField] private float _bobAmount = 0.05f;

    [Header("Tilt Settings")]
    [SerializeField] private float _tiltAmount = 5f;
    [SerializeField] private float _tiltSpeed = 6f;

    [Header("Sway Settings")]
    [SerializeField] private float _swayAmount = 0.03f;
    [SerializeField] private float _swaySmooth = 4f;

    [Header("Recoil Settings")]
    [SerializeField] private float _recoilAmount = 0.1f;
    [SerializeField] private float _recoilSpeed = 10f;

    [Header("Fire Settings")]
    [SerializeField] private float _fireRate = 0.2f;

    [Header("Ammo Settings")]
    [SerializeField] private int _maxAmmo = 6;
    [SerializeField] private AudioClip _emptyClip;
    [SerializeField] private AudioClip _shootClip;
    [SerializeField] private AudioClip _enableClip;

    [Header("Camera Shake Settings")]
    [SerializeField] private float _shakeAmount = 3f;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;

    private CharacterController _characterController;
    private Vector3 _initialLocalPosition;
    private Quaternion _initialLocalRotation;

    private Vector3 _swayOffset;
    private float _bobTimer;
    private Vector3 _currentRecoilOffset;

    private float _nextFireTime;

    private int _currentAmmo;

    private Coroutine _recoilCoroutine;

    public bool IsEquipped => InventoryManager.Instance.EquippedItem == InventoryManager.ItemType.Revolver;

    public int CurrentAmmo => _currentAmmo;

    public PlayerController player;


    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();

        if (_weaponTransform == null)
            Debug.LogWarning("Weapon Transform is not assigned.");

        if (_audioSource == null)
            Debug.LogWarning("AudioSource is not assigned.");

        _currentAmmo = _maxAmmo;
    }

    private void Start()
    {

        _initialLocalPosition = _weaponTransform.localPosition;
        _initialLocalRotation = _weaponTransform.localRotation;

        UpdateAmmoUI();

    }

    private void Update()
    {
         if (CameraTranzition.InPC)
        {
            return;
        }
        if (!IsEquipped)
            return;

        UpdateAmmoUI();

        HandleBob();
        HandleSway();
        HandleTilt();
        HandleFireInput();
    }


    private void HandleFireInput()
    {
        if (Input.GetButtonDown("Fire1") && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + _fireRate;

            if (_currentAmmo > 0)
            {
                Fire();
            }
            else
            {
                PlayEmptyClick();
            }
        }
    }

    private void Fire()
    {
        _currentAmmo--;
        UpdateAmmoUI();

        if (_recoilCoroutine != null)
            StopCoroutine(_recoilCoroutine);

        _recoilCoroutine = StartCoroutine(DoRecoil());

        PlayShootSound();
        ShakeCamera();
        ShootRaycast();
    }
    private void ShootRaycast()
    {
        Ray ray = new Ray(_cameraTransform.position, _cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, 100f))
        {
            if (hitInfo.collider.TryGetComponent<NpcBehaviour>(out var npc))
            {
                npc.TakeDamage();
            }
        }
    }

    private IEnumerator DoRecoil()
    {
        float t = 0f;
        Vector3 startOffset = _currentRecoilOffset;
        Vector3 recoilTarget = new Vector3(0f, 0f, -_recoilAmount);

        while (t < 1f)
        {
            t += Time.deltaTime * _recoilSpeed;
            _currentRecoilOffset = Vector3.Lerp(startOffset, recoilTarget, t);
            yield return null;
        }

        t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * _recoilSpeed;
            _currentRecoilOffset = Vector3.Lerp(recoilTarget, Vector3.zero, t);
            yield return null;
        }

        _currentRecoilOffset = Vector3.zero;
    }

    private void PlayEmptyClick()
    {
        if (_emptyClip != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_emptyClip);
        }
    }

    private void PlayShootSound()
    {
        if (_shootClip != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(_shootClip);
        }
    }

    private void ShakeCamera()
    {
        CameraShaker.Instance.ShakeOnce(_shakeAmount, 2f, 0.1f, 0.5f, Vector3.zero, Vector3.one);
    }

    private void HandleBob()
    {
        if (_characterController.velocity.magnitude > 0.1f && _characterController.isGrounded && _characterController.enabled)
        {
            _bobTimer += Time.deltaTime * _bobSpeed;

            float offsetX = Mathf.Sin(_bobTimer) * _bobAmount;
            float offsetY = Mathf.Cos(_bobTimer * 2f) * (_bobAmount * 0.5f);

            Vector3 bobOffset = new Vector3(offsetX, offsetY, 0f);
            Vector3 targetPosition = _initialLocalPosition + bobOffset + _swayOffset + _currentRecoilOffset;

            _weaponTransform.localPosition = Vector3.Lerp(_weaponTransform.localPosition, targetPosition, Time.deltaTime * _bobSpeed);
        }
        else
        {
            _bobTimer = 0f;
            Vector3 targetPosition = _initialLocalPosition + _swayOffset + _currentRecoilOffset;
            _weaponTransform.localPosition = Vector3.Lerp(_weaponTransform.localPosition, targetPosition, Time.deltaTime * _bobSpeed);
        }
    }

    private void HandleSway()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        Vector3 targetOffset = new Vector3(-mouseX, -mouseY, 0f) * _swayAmount;
        _swayOffset = Vector3.Lerp(_swayOffset, targetOffset, Time.deltaTime * _swaySmooth);
    }

    private void HandleTilt()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        Quaternion targetTilt = Quaternion.Euler(0f, 0f, -horizontalInput * _tiltAmount);
        _weaponTransform.localRotation = Quaternion.Lerp(_weaponTransform.localRotation, _initialLocalRotation * targetTilt, Time.deltaTime * _tiltSpeed);
    }

    private void UpdateAmmoUI()
    {
        if (_ammoText != null)
        {
            _ammoText.text = $"{_currentAmmo}/{_maxAmmo}";
        }
    }

    public void Reload()
    {
        _currentAmmo = _maxAmmo;
        UpdateAmmoUI();
    }
}

