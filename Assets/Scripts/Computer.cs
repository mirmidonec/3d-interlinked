using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Computer : Interactable
{
    public CameraTranzition tranz;
    public PlayerController player;
    public override void OnFocus()
    {
        if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
        {
            Message = "My personal computer";
        }
        else
        {
            Message = "Мой личный компьютер";
        }
        _outline.enabled = true;
    }
    public override void OnInteract()
    {
        if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
        {
            Message = "I use it to compile reports";
        }
        else
        {
            Message = "Я использую его для составления отчетов";
        }
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
            if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
            {
                Message = "First, I need to sit down";
            }
            else
            {
                Message = "Сначала мне нужно присесть";
            }
            return;
        }
        if (player.IsDialogue || CameraTranzition.InPC)
        {
            return;
        }

        StartCoroutine(tranz.TransitionToTarget());
    }
}
