using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Recorder : Interactable
{
    public override void OnFocus()
    {
        if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
        {
            Message = "Service recording device";
        }
        else
        {
            Message = "Служебное записывающее устройство";
        }
        _outline.enabled = true;
    }

    public override void OnInteract()
    {
        if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
        {
            Message = "Will be available in the next updates";
        }
        else
        {
            Message = "Будет доступно в следующих обновлениях";
        }

    }

    public override void OnLoseFocus()
    {
        _outline.enabled = false;
    }

}
