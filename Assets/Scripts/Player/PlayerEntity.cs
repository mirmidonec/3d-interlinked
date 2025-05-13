using UnityEngine;

public class PlayerEntity : MonoBehaviour
{
    public float Speed;
    public float InteractDistance;
    public static PlayerEntity Instance;
    private void Awake()
    {
        Instance = this;
    }
}
