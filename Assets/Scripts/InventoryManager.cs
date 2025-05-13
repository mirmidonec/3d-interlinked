using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public bool IsEquiped = false;

    public GameObject Revolver;

    public GameObject RevolverDown;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if(!IsEquiped) return;

        if (Input.GetKeyDown(KeyCode.Q))
        {
            Drop();
        }
    }

    public void Pick()
    {
        RevolverDown.SetActive(false);
        Revolver.SetActive(true);
        IsEquiped = true;
    }
    public void Drop()
    {
        RevolverDown.SetActive(true);
        Revolver.SetActive(false);
        IsEquiped = false;
    }





}
