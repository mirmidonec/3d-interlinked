using EZCameraShake;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;


[RequireComponent(typeof(CharacterController))]
public class WeaponController : MonoBehaviour
{
    [Header("References")]
    public Transform weaponTransform;
    public Transform cameraTransform;

    [Header("Bob Settings")]
    public float bobSpeed = 6f;
    public float bobAmount = 0.05f;

    [Header("Tilt Settings")]
    public float tiltAmount = 5f;
    public float tiltSpeed = 6f;

    [Header("Sway Settings")]
    public float swayAmount = 0.03f;
    public float swaySmooth = 4f;

    [Header("Recoil Settings")]
    public float recoilAmount = 0.1f;
    public float recoilSpeed = 10f;

    [Header("Fire Settings")]
    public float fireRate = 0.2f;
    [Header("Ammo Settings")]
    public int maxAmmo = 6;
    public int currentAmmo;
    public AudioClip emptyClip;

    private CharacterController controller;
    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;
    private Vector3 swayOffset;
    private float bobTimer;
    private Vector3 currentRecoilOffset;
    private float nextFireTime = 0f;

    public AudioSource source;
    public AudioClip shootClip;
    public AudioClip enableClip;

    public float shakeAmount;

    public TMP_Text text;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        initialLocalPos = weaponTransform.localPosition;
        initialLocalRot = weaponTransform.localRotation;
        currentAmmo = maxAmmo; // полная обойма при старте
    }

    void Update()
    {
        if (!InventoryManager.Instance.IsEquiped)
            return;

        text.text = currentAmmo + "/" + maxAmmo;

        HandleBob();
        HandleSway();
        HandleTilt();
        HandleFireInput();
    }

    void HandleFireInput()
    {
        if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;

            if (currentAmmo > 0)
            {
                Fire();
            }
            else
            {
                PlayEmptyClick();
            }
        }
    }

    void PlayEmptyClick()
    {
        if (emptyClip != null)
        {
            source.PlayOneShot(emptyClip);
        }
    }

    void HandleBob()
    {
        if (controller.velocity.magnitude > 0.1f && controller.isGrounded)
        {
            bobTimer += Time.deltaTime * bobSpeed;

            float offsetX = Mathf.Sin(bobTimer) * bobAmount;
            float offsetY = Mathf.Cos(bobTimer * 2f) * (bobAmount * 0.5f);

            Vector3 bobOffset = new Vector3(offsetX, offsetY, 0f);
            weaponTransform.localPosition = Vector3.Lerp(weaponTransform.localPosition, initialLocalPos + bobOffset + swayOffset + currentRecoilOffset, Time.deltaTime * bobSpeed);
        }
        else
        {
            bobTimer = 0;
            weaponTransform.localPosition = Vector3.Lerp(weaponTransform.localPosition, initialLocalPos + swayOffset + currentRecoilOffset, Time.deltaTime * bobSpeed);
        }
    }

    void HandleSway()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        Vector3 targetOffset = new Vector3(-mouseX, -mouseY, 0f) * swayAmount;
        swayOffset = Vector3.Lerp(swayOffset, targetOffset, Time.deltaTime * swaySmooth);
    }

    void HandleTilt()
    {
        float horizontal = Input.GetAxis("Horizontal");
        Quaternion targetTilt = Quaternion.Euler(0f, 0f, -horizontal * tiltAmount);
        weaponTransform.localRotation = Quaternion.Lerp(weaponTransform.localRotation, initialLocalRot * targetTilt, Time.deltaTime * tiltSpeed);
    }

    void Fire()
    {
        currentAmmo--; // уменьшаем патроны
        StopAllCoroutines();
        StartCoroutine(DoRecoil());
    }

    IEnumerator DoRecoil()
    {
        float t = 0f;
        Vector3 start = currentRecoilOffset;
        Vector3 recoilVector = new Vector3(0f, 0f, -recoilAmount);

        source.PlayOneShot(shootClip);
        CameraShaker.Instance.ShakeOnce(shakeAmount, 2f, 0.1f, 0.5f, Vector3.zero, Vector3.one);

        while (t < 1f)
        {
            t += Time.deltaTime * recoilSpeed;
            currentRecoilOffset = Vector3.Lerp(start, recoilVector, t);
            yield return null;
        }
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * recoilSpeed;
            currentRecoilOffset = Vector3.Lerp(recoilVector, Vector3.zero, t);
            yield return null;
        }

        currentRecoilOffset = Vector3.zero;
    }

    // Можно добавить метод перезарядки
    public void Reload()
    {
        currentAmmo = maxAmmo;
    }
}
