using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTranzition : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform targetTransform;
    public float targetFOV = 30f;

    [Header("Transition Settings")]
    public float transitionSpeed = 5f;
    public KeyCode zoomInKey = KeyCode.Z;
    public KeyCode zoomOutKey = KeyCode.X;

    private Coroutine currentTransition;
    private Camera cam;

     public GameObject crosshair;

    public static bool InPC = false;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("CameraTranzition: No Camera component found!");
            enabled = false;
        }
    }
    public static bool InTransition;
    public IEnumerator TransitionToTarget()
    {
        crosshair.SetActive(false);
        InPC = true;
        InTransition = true;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float startFOV = cam.fieldOfView;

        Vector3 endPos = targetTransform.position;
        Quaternion endRot = targetTransform.rotation;
        float endFOV = targetFOV;

        while (Vector3.Distance(transform.position, endPos) > 0.01f ||
               Quaternion.Angle(transform.rotation, endRot) > 0.1f ||
               Mathf.Abs(cam.fieldOfView - endFOV) > 0.1f)
        {
            transform.position = Vector3.Lerp(transform.position, endPos, Time.deltaTime * transitionSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, endRot, Time.deltaTime * transitionSpeed);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, endFOV, Time.deltaTime * transitionSpeed);
            yield return null;
        }

        transform.position = endPos;
        transform.rotation = endRot;
        cam.fieldOfView = endFOV;
            Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        InTransition = false;

    }

    public IEnumerator TransitionToInitial()
    {
          crosshair.SetActive(true);
        InPC = false;
        InTransition = true;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float startFOV = cam.fieldOfView;

        Vector3 endPos = startPos;
        Quaternion endRot = startRot;
        float endFOV = startFOV;

        if (targetTransform != null)
        {
            endPos = targetTransform.position;
            endRot = targetTransform.rotation;
            endFOV = targetFOV;
        }

        (endPos, startPos) = (startPos, endPos);
        (endRot, startRot) = (startRot, endRot);
        (endFOV, startFOV) = (startFOV, endFOV);

        while (Vector3.Distance(transform.position, endPos) > 0.01f ||
               Quaternion.Angle(transform.rotation, endRot) > 0.1f ||
               Mathf.Abs(cam.fieldOfView - endFOV) > 0.1f)
        {
            transform.position = Vector3.Lerp(transform.position, endPos, Time.deltaTime * transitionSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, endRot, Time.deltaTime * transitionSpeed);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, endFOV, Time.deltaTime * transitionSpeed);
            yield return null;
        }

        transform.position = endPos;
        transform.rotation = endRot;
        cam.fieldOfView = endFOV;
            Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        InTransition = false;

    }
}
