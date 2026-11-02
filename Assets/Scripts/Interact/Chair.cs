
public class Chair : Interactable
{
    public PlayerController Player;
    public override void OnFocus()
    {
        if (Player == null)
        {
            if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
            {
                Message = "Chair";
            }
            else
            {
                Message = "Стул";
            }
            _outline.enabled = true;
            return;
        }
        if (Player.isSitting)
        {
            Message = "";
            _outline.enabled = false;
            return;
        }

        if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
        {
            Message = "Chair";
        }
        else
        {
            Message = "Стул";
        }
        _outline.enabled = true;
    }

    public override void OnInteract()
    {
        if (Player == null)
        {
            if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
            {
                Message = "After you sit down, the interrogation will begin";
            }
            else
            {
                Message = "После того, как вы сядете, начнется допрос";
            }
            return;
        }
        if (Player.isSitting)
        {
            Message = "";
            _outline.enabled = false;
            return;
        }

        if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
        {
            Message = "After you sit down, the interrogation will begin";
        }
        else
        {
            Message = "После того, как вы сядете, начнется допрос";
        }
        _outline.enabled = true;
    }

    public override void OnInteractAlt()
    {
        if (Player == null)
        {
            if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
            {
                Message = "It's not your chair";
            }
            else
            {
                Message = "Это не ваш стул";
            }
            _outline.enabled = true;
            return;
        }

        bool questCompleted = QuestManager.Instance.IsQuestCompleted(0);
        bool questStarted = QuestManager.Instance.IsQuestCompletedBefore(0);

        if (!Player.isSitting)
        {
            if (questCompleted || questStarted)
            {
                Player.HandleSit();
                Message = "";
                _outline.enabled = false;
            }
            else
            {
                if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
                {
                    Message = "Inspect room before sit";
                }
                else
                {
                    Message = "Изучите комнату перед тем как сесть";
                }
                _outline.enabled = true;
            }
        }
    }
    public override void OnLoseFocus()
    {
        _outline.enabled = false;
        Message = "";
    }
}
