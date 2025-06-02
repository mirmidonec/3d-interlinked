using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Computer : Interactable
{
    public CameraTranzition tranz;
    public PlayerController player;
    public override void OnFocus()
    {
        Message = "My personal computer";
        _outline.enabled = true;
    }
    public override void OnInteract()
    {
        Message = "I use it to compile reports.";
    }
    public override void OnLoseFocus()
    {
        _outline.enabled = false;
    }

    public override void OnInteractAlt()
    {
        if (CameraTranzition.InTransition)
        {
            return;
        }
        if (!player.isSitting)
        {
            Message = "First, I need to sit down.";
            return;
        }
        if (player.IsDialogue || CameraTranzition.InPC)
        {
            return;
        }

        StartCoroutine(tranz.TransitionToTarget());
    }
}
