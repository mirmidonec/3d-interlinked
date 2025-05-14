using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateAround : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed;

    public enum RotationAxis { X, Y, Z }
    [SerializeField] private RotationAxis _rotationAxis;

    void Update()
    {
        Vector3 axis = Vector3.zero;

        switch (_rotationAxis)
        {
            case RotationAxis.X:
                axis = Vector3.right;
                break;
            case RotationAxis.Y:
                axis = Vector3.up;
                break;
            case RotationAxis.Z:
                axis = Vector3.forward;
                break;
        }

        transform.Rotate(axis, _rotationSpeed * Time.deltaTime);
    }
}
