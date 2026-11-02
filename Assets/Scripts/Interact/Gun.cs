
public class Gun : Interactable
{
    public override void OnFocus()
    {
        if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
        {
            Message = "Service weapon";
        }
        else
        {
            Message = "Табельное оружие";
        }
        _outline.enabled = true;
    }

    public override void OnInteract()
    {
        if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
        {
            Message = "Only for critical situations";
        }
        else
        {
            Message = "Только для критических ситуаций";
        }
    }

    public override void OnInteractAlt()
    {
        if (QuestManager.Instance.IsQuestCompleted(0) == false && QuestManager.Instance.IsQuestCompletedBefore(0) == false)
        {
            if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
            {
                Message = "The pistol is in place. Good";
            }
            else
            {
                Message = "Не сейчас";
            }
            QuestManager.Instance.CompleteStep(1);
            return;
        }
        else
        {
            if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
            {
                Message = "Not now";
            }
            else
            {
                Message = "Не сейчас";
            }
        }
        if (!InventoryManager.Instance.IsEquipped && QuestManager.Instance.CurrentIndex == 2 && QuestManager.Instance.IsQuestCompleted(2) == false && QuestManager.Instance.IsQuestCompletedBefore(2) == false)
        {
            InventoryManager.Instance.Equip(InventoryManager.ItemType.Revolver);
            QuestManager.Instance.CompleteStep(0);
        }
    }

    public override void OnLoseFocus()
    {
        _outline.enabled = false;
    }

}
