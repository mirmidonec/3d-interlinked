
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

    }

    public override void OnInteractAlt()
    {
        if (!InventoryManager.Instance.IsEquipped)
        {
            InventoryManager.Instance.Equip(InventoryManager.ItemType.Revolver);
        }
    }

    public override void OnLoseFocus()
    {
        _outline.DisableOutline();
    }

}
