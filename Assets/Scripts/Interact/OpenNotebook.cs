using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenNotebook : MonoBehaviour
{
    [SerializeField] private float closedX;
    [SerializeField] private float openedX;
    [SerializeField] private float speed;

    [SerializeField] private AudioClip _turnPage;


    private AudioSource _source;
    private bool isOpened = false;
    private float targetX;

    private void OnEnable()
    {
        _source.PlayOneShot(_turnPage);
    }


    private void Awake()
    {
        _source = GetComponent<AudioSource>();

    }

    private void Start()
    {
        targetX = closedX;
        SetRotation(targetX);
    }

    private void Update()
    {
        HandleInput();
        SmoothRotate();
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _source.PlayOneShot(_turnPage);
            isOpened = !isOpened;
            targetX = isOpened ? openedX : closedX;
        }
    }

    private void SmoothRotate()
    {
        float currentX = transform.localEulerAngles.x;
        float newX = Mathf.MoveTowardsAngle(currentX, targetX, speed * Time.deltaTime);
        SetRotation(newX);
    }

    private void SetRotation(float x)
    {
        Vector3 rot = transform.localEulerAngles;
        rot.x = x;
        transform.localEulerAngles = rot;
    }
}
