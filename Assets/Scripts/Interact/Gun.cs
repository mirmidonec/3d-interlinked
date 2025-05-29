
public class Gun : Interactable
{
    public override void OnFocus()
    {
        Message = "Service weapon";
        _outline.enabled = true;
    }

    public override void OnInteract()
    {
        Message = "In a critical situation...";

    }

    public override void OnInteractAlt()
    {
        if (QuestManager.Instance.IsQuestCompleted(0) == false && QuestManager.Instance.IsQuestCompletedBefore(0) == false)
        {
            Message = "Not now";
            QuestManager.Instance.CompleteStep(1);
            return;
        }
        else
        {
           Message = "Not now"; 
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
