using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    public Camera playerCamera;

    public Image crosshairPanel;

    public Color32 crosshairColor;

    public InventoryManager inv;

    private void Update()
    {
        if (inv.IsEquipped)
        {
            crosshairColor.a = 0;
            return;
        }
        crosshairPanel.color = crosshairColor;
        RaycastHit raycastHit;
        if (!Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out raycastHit, PlayerEntity.Instance.InteractDistance))
        {
            crosshairColor.a = 110;
            return;
        }
        if (raycastHit.collider.GetComponent<Interactable>())
        {
            crosshairColor.a = 255;
            return;
        }
        crosshairColor.a = 110;
    }
}
