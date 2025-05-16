using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Sounds")]
    public AudioSource footSource;
    public AudioClip[] walkClips;
    public float baseStepSpeed;

    [Header("Camera")]
    public bool invertX;
    public bool invertY;
    public Camera playerCamera;
    public float lookSpeedX;
    public float lookSpeedY;
    public float upperLookLimit;
    public float lowerLookLimit;
    public float tiltSpeed = 7f;
    public Vector3 offset;
    public float smoothRotation = 12f;
    public float maxRotationAmount = 5f;
    public float rotationAmount = 4f;
    public float zoomSpeed = 5f;
    public float tiltAmount = 7f;
    private float defaultYPos;
    private float rotationX;
    private float rotationZ;
    private float defaultFOV;
    private int invertXFactor;
    private int invertYFactor;

    [Space]

    [Header("Bobbing")]
    public float walkBobSpeed = 14f;
    public float walkBobAmount = 0.05f;
    private float headbobEndTimer;


    private CharacterController characterController;
    private Vector3 moveDirection;
    private float verticalInputRaw;
    private float horizontalInputRaw;
    private float horizontalInput;
    private float verticalInput;
    private Vector2 currentInput;
    private Vector2 currentInputRaw;

    // timers
    private float footstepTimer;

    [Header("Movement Parameters")]
    public float gravity = 30f;



    private void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();
        defaultYPos = characterController.center.y + characterController.height / 2f + offset.y;
        defaultFOV = playerCamera.fieldOfView;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        headbobEndTimer -= Time.deltaTime;
        HandleCameraController();
        HandleMouseLook();
        CalculateMovementInput();
        HandleMovementInput();
        HandleHeadbob();
        HandleFootsteps();
        ApplyFinalMovements();
        HandleZoom();
    }

    private void HandleZoom()
    {
        if (Input.GetKey(KeyCode.Z))
        {
            Zoom(20);
        }
        else
        {
            Zoom(70);
        }
    }

    private void HandleFootsteps()
    {
        if (!characterController.isGrounded)
        {
            return;
        }
        if (currentInputRaw == Vector2.zero)
        {
            return;
        }
        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f && currentInputRaw != Vector2.zero)
        {
            footSource.pitch = Random.Range(0.8f, 1.5f);
            footSource.PlayOneShot(walkClips[Random.Range(0, walkClips.Length - 1)]);
            footstepTimer = baseStepSpeed;
        }
    }

    float timer;

    private void HandleHeadbob()
    {

        if (currentInputRaw != new Vector2(0f, 0f))
        {
            timer += Time.deltaTime * walkBobSpeed;
            playerCamera.transform.localPosition = new Vector3(playerCamera.transform.localPosition.x, defaultYPos + Mathf.Sin(timer) * walkBobAmount, playerCamera.transform.localPosition.z);
            return;
        }
    }
    private void HandleMovementInput()
    {
        float y = moveDirection.y;
        moveDirection = transform.TransformDirection(Vector3.forward) * currentInput.x + transform.TransformDirection(Vector3.right) * currentInput.y;
        moveDirection.y = y;
    }

    private void ApplyFinalMovements()
    {

        if (!characterController.isGrounded)
        {
            moveDirection.y = moveDirection.y - gravity * Time.deltaTime;
        }

        characterController.Move(moveDirection * Time.deltaTime);
    }

    private void CalculateMovementInput()
    {
        if (Input.GetKey(KeyCode.D) && (Input.GetKey(KeyCode.A)))
        {
            horizontalInput = 0f;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            horizontalInput = Mathf.Lerp(horizontalInput, 1f, 15f * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.A))
        {
            horizontalInput = Mathf.Lerp(horizontalInput, -1f, 15f * Time.deltaTime);
        }
        else if (Input.GetAxis("Horizontal") != 0f)
        {
            horizontalInput = Input.GetAxis("Horizontal");
        }
        else
        {
            horizontalInput = Mathf.Lerp(horizontalInput, 0f, 15f * Time.deltaTime);
        }
        if ((Input.GetKey(KeyCode.W)) && Input.GetKey(KeyCode.S))
        {
            verticalInput = (verticalInput = 0f);
        }
        else if (Input.GetKey(KeyCode.W))
        {
            verticalInput = Mathf.Lerp(verticalInput, 1f, 15f * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            verticalInput = Mathf.Lerp(verticalInput, -1f, 15f * Time.deltaTime);
        }
        else if (Input.GetAxis("Vertical") != 0f)
        {
            verticalInput = Input.GetAxis("Vertical");
        }
        else
        {
            verticalInput = Mathf.Lerp(verticalInput, 0f, 10f * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.D) && (Input.GetKey(KeyCode.A)))
        {
            horizontalInputRaw = 0f;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            horizontalInputRaw = 1f;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            horizontalInputRaw = -1f;
        }
        else if (Input.GetAxisRaw("Horizontal") != 0f)
        {
            horizontalInputRaw = Input.GetAxisRaw("Horizontal");
        }
        else
        {
            horizontalInputRaw = 0f;
        }
        if ((Input.GetKey(KeyCode.W)) && Input.GetKey(KeyCode.S))
        {
            verticalInputRaw = 0f;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            verticalInputRaw = 1f;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            verticalInputRaw = -1f;
        }
        else if (Input.GetAxisRaw("Vertical") != 0f)
        {
            verticalInputRaw = Input.GetAxisRaw("Vertical");
        }
        else
        {
            verticalInputRaw = 0f;
        }

        currentInput = new Vector2(PlayerEntity.Instance.Speed * verticalInput, PlayerEntity.Instance.Speed * horizontalInput);
        currentInputRaw = new Vector2(PlayerEntity.Instance.Speed * verticalInputRaw, PlayerEntity.Instance.Speed * horizontalInputRaw);
    }
    private void HandleCameraController()
    {
        float num = defaultFOV;
        if (playerCamera.fieldOfView != num)
        {
            Zoom(num);
        }
        float temptiltAmount = (!characterController.isGrounded) ? 0f : ((horizontalInputRaw == 1f) ? (-tiltAmount) : ((horizontalInputRaw == -1f) ? tiltAmount : 0f));
        SideTilt(temptiltAmount);
        float arg_12F_0 = rotationZ;
    }
    private void Zoom(float fov)
    {
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, fov, zoomSpeed * Time.deltaTime);
    }
    private void SideTilt(float temptiltAmount)
    {
        float b = Mathf.Clamp(-Input.GetAxis("Mouse X") * rotationAmount, -maxRotationAmount, maxRotationAmount);
        rotationZ = Mathf.Lerp(rotationZ, temptiltAmount, tiltSpeed * Time.deltaTime) + Mathf.Lerp(transform.localRotation.z, b, Time.deltaTime * smoothRotation);
    }

    private void HandleMouseLook()
    {

        if (invertX)
        {
            invertXFactor = -1;
        }
        else
        {
            invertXFactor = 1;
        }
        if (invertY)
        {
            invertYFactor = -1;
        }
        else
        {
            invertYFactor = 1;
        }


        rotationX -= Input.GetAxis("Mouse Y") * lookSpeedY * (float)invertYFactor;
        transform.rotation *= Quaternion.Euler(0f, Input.GetAxis("Mouse X") * lookSpeedX * (float)invertXFactor, 0f);
        rotationX -= Input.GetAxis("Mouse Y") * lookSpeedY * (float)invertYFactor;
        rotationX = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, rotationZ);
        transform.rotation *= Quaternion.Euler(0f, Input.GetAxis("Mouse X") * lookSpeedX * (float)invertXFactor, 0f);
        if (playerCamera.transform.localPosition.y != defaultYPos)
        {
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, characterController.center + new Vector3(0f, characterController.height / 2f, 0f) + offset, 5f * Time.deltaTime);
            headbobEndTimer = 3f;
            return;
        }
        if (headbobEndTimer < 0f && headbobEndTimer > -0.25f)
        {
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, characterController.center + new Vector3(0f, characterController.height / 2f, 0f) + offset, 8f * Time.deltaTime);
            return;
        }
        if (headbobEndTimer < 0.25f)
        {
            // Устанавливаем положение камеры в позицию стоячего игрока.
            playerCamera.transform.localPosition = characterController.center + new Vector3(0f, characterController.height / 2f, 0f) + offset;
        }
    }


}
