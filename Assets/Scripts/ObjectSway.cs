using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSway : MonoBehaviour
{
    [Header("Sway Settings")]
    public float swayAmount = 0.02f;
    public float maxSwayAmount = 0.06f;
    public float swaySmooth = 4f;

    [Header("Tilt Settings")]
    public float tiltAmount = 3f;
    public float maxTiltAmount = 5f;
    public float tiltSmooth = 4f;
    public bool tiltX = true;
    public bool tiltY = true;
    public bool tiltZ = true;

    [Header("Bob Settings")]
    public GameObject player;
    public float walkBobAmount = 0.05f;


    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
    }

    private void Update()
    {
        WeaponSway();
        TiltSway();
        ItemMovement();
    }

    private void WeaponSway()
    {
        float inputX = -Input.GetAxis("Mouse X");
        float inputY = -Input.GetAxis("Mouse Y");

        float swayX = Mathf.Clamp(inputX * swayAmount, -maxSwayAmount, maxSwayAmount);
        float swayY = Mathf.Clamp(inputY * swayAmount, -maxSwayAmount, maxSwayAmount);

        Vector3 targetPosition = initialPosition + new Vector3(swayX, swayY, 0f);
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * swaySmooth);
    }

    private void TiltSway()
    {
        float inputX = -Input.GetAxis("Mouse X");
        float inputY = -Input.GetAxis("Mouse Y");

        float tiltXVal = Mathf.Clamp(inputY * tiltAmount, -maxTiltAmount, maxTiltAmount);
        float tiltYVal = Mathf.Clamp(inputX * tiltAmount, -maxTiltAmount, maxTiltAmount);
        float tiltZVal = tiltYVal;

        Quaternion targetRotation = Quaternion.Euler(
            tiltX ? tiltXVal : 0f,
            tiltY ? tiltYVal : 0f,
            tiltZ ? tiltZVal : 0f
        );

        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation * initialRotation, Time.deltaTime * tiltSmooth);
    }

    private void ItemMovement()
    {
        if (player == null) return;

        PlayerController fpc = player.GetComponent<PlayerController>();
        if (fpc == null) return;

        Vector2 input = fpc.currentInputRaw;
        float bobAmount = walkBobAmount;
        float bob = Mathf.Sin(fpc.timer) * bobAmount;

        if (input != Vector2.zero)
        {
            transform.localPosition = new Vector3(
                transform.localPosition.x,
                initialPosition.y + bob,
                transform.localPosition.z
            );
        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, initialPosition, Time.deltaTime * swaySmooth);
        }
    }
}
