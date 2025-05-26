
public class Notebook : Interactable
{
    public override void OnFocus()
    {
        Message = "Book of office";
        _outline.enabled = true;
    }

    public override void OnInteract()
    {
        Message = "Stores all interrogation protocols";
    }
    public override void OnInteractAlt()
    {
        if (!InventoryManager.Instance.IsEquipped)
        {
            InventoryManager.Instance.Equip(InventoryManager.ItemType.Notebook);
        }
    }

    public override void OnLoseFocus()
    {
        _outline.enabled = false;
    }




}
