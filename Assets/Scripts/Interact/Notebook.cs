
public class Notebook : Interactable
{
    public override void OnFocus()
    {
        Message = "Book of office";
        _outline.ActiveOutline();
    }

    public override void OnInteract()
    {
        Message = "Stores all interrogation protocols";
        _outline.ActivatedOutline();
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
        _outline.DisableOutline();
    }




}
