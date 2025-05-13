using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : Interactable
{
    public override void OnFocus()
    {
        Message = "Service weapon";
        _outline.ActiveOutline();
    }

    public override void OnInteract()
    {
        Message = "In a critical situation...";
        _outline.ActivatedOutline();
        if (!InventoryManager.Instance.IsEquiped)
        {
            InventoryManager.Instance.Pick();
        }
    }

    public override void OnLoseFocus()
    {
        _outline.DisableOutline();
    }

}
