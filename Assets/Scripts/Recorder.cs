using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Recorder : Interactable
{
    public override void OnFocus()
    {
        Message = "Service recording device";
        _outline.enabled = true;
    }

    public override void OnInteract()
    {
        Message = "Will be availble soon";

    }

    public override void OnLoseFocus()
    {
        _outline.enabled = false;
    }

}
