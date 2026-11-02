using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SUPER : MonoBehaviour
{

    [Header("Rotation")]
    [SerializeField] private float rotationAmount = 1.2f;
    [SerializeField] private float rotationSpeed = 0.4f;

    [Header("Position")]
    [SerializeField] private float positionAmount = 0.015f;
    [SerializeField] private float positionSpeed = 0.35f;

    private Quaternion startRotation;
    private Vector3 startPosition;

    private float seedX;
    private float seedY;
    private float seedZ;

    private void Awake()
    {
        startRotation = transform.localRotation;
        startPosition = transform.localPosition;

        seedX = Random.Range(0f, 100f);
        seedY = Random.Range(100f, 200f);
        seedZ = Random.Range(200f, 300f);
    }

    private void LateUpdate()
    {
        float time = Time.time;

        float rotX = (Mathf.PerlinNoise(seedX, time * rotationSpeed) - 0.5f) * 2f;
        float rotY = (Mathf.PerlinNoise(seedY, time * rotationSpeed) - 0.5f) * 2f;
        float rotZ = (Mathf.PerlinNoise(seedZ, time * rotationSpeed * 0.7f) - 0.5f) * 2f;

        Quaternion targetRot = Quaternion.Euler(
            rotX * rotationAmount,
            rotY * rotationAmount,
            rotZ * rotationAmount * 0.5f);

        transform.localRotation = startRotation * targetRot;

        float posX = (Mathf.PerlinNoise(seedX + 50f, time * positionSpeed) - 0.5f) * 2f;
        float posY = (Mathf.PerlinNoise(seedY + 50f, time * positionSpeed) - 0.5f) * 2f;

        Vector3 targetPos = startPosition + new Vector3(
            posX * positionAmount,
            posY * positionAmount,
            0f);

        transform.localPosition = targetPos;
    }
}

