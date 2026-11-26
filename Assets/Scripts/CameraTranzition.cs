
using System.Collections;
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

    [Header("UI")]
    public GameObject crosshair;

    private Camera cam;
    private Coroutine currentTransition;

    // Исходное положение камеры
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float initialFOV;

    public static bool InPC = false;
    public static bool InTransition = false;

    void Awake()
    {
        cam = GetComponent<Camera>();
InPC = false;
InTransition = false;
        if (cam == null)
        {
            Debug.LogError("CameraTranzition: No Camera component found!");
            enabled = false;
            return;
        }

        // Запоминаем исходное положение ОДИН раз
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        initialFOV = cam.fieldOfView;
    }

    public void StartTransitionToTarget()
    {
        if (currentTransition != null)
            StopCoroutine(currentTransition);

        currentTransition = StartCoroutine(TransitionToTarget());
    }

    public void StartTransitionToInitial()
    {
        if (currentTransition != null)
            StopCoroutine(currentTransition);

        currentTransition = StartCoroutine(TransitionToInitial());
    }

   public IEnumerator TransitionToTarget()
{
    if (targetTransform == null)
        yield break;

    // Каждый раз обновляем точку возврата
    initialPosition = transform.position;
    initialRotation = transform.rotation;
    initialFOV = cam.fieldOfView;

    InPC = true;
    InTransition = true;

    if (crosshair != null)
        crosshair.SetActive(false);

    Vector3 endPos = targetTransform.position;
    Quaternion endRot = targetTransform.rotation;
    float endFOV = targetFOV;

    while (Vector3.Distance(transform.position, endPos) > 0.01f ||
           Quaternion.Angle(transform.rotation, endRot) > 0.1f ||
           Mathf.Abs(cam.fieldOfView - endFOV) > 0.1f)
    {
        transform.position = Vector3.Lerp(
            transform.position, endPos, Time.deltaTime * transitionSpeed);

        transform.rotation = Quaternion.Slerp(
            transform.rotation, endRot, Time.deltaTime * transitionSpeed);

        cam.fieldOfView = Mathf.Lerp(
            cam.fieldOfView, endFOV, Time.deltaTime * transitionSpeed);

        yield return null;
    }

    transform.position = endPos;
    transform.rotation = endRot;
    cam.fieldOfView = endFOV;

    Cursor.lockState = CursorLockMode.Confined;
    Cursor.visible = true;

    InTransition = false;
    currentTransition = null;
}

    public IEnumerator TransitionToInitial()
    {
        InTransition = true;

        // Возвращаем прицел
        if (crosshair != null)
            crosshair.SetActive(true);

        // Всегда возвращаемся в сохранённую точку
        while (Vector3.Distance(transform.position, initialPosition) > 0.01f ||
               Quaternion.Angle(transform.rotation, initialRotation) > 0.1f ||
               Mathf.Abs(cam.fieldOfView - initialFOV) > 0.1f)
        {
            transform.position = Vector3.Lerp(
                transform.position, initialPosition, Time.deltaTime * transitionSpeed);

            transform.rotation = Quaternion.Slerp(
                transform.rotation, initialRotation, Time.deltaTime * transitionSpeed);

            cam.fieldOfView = Mathf.Lerp(
                cam.fieldOfView, initialFOV, Time.deltaTime * transitionSpeed);

            yield return null;
        }

        // Точно устанавливаем исходные значения
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        cam.fieldOfView = initialFOV;

        InPC = false;
        InTransition = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
          if (DialogueSystem.instance != null)
                {
                    if(DialogueSystem.instance.isFinaleStart){
                     Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;

                    }
                }

        currentTransition = null;
    }
}
