
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
        if (!InventoryManager.Instance.IsEquipped)
        {
            InventoryManager.Instance.Equip(InventoryManager.ItemType.Revolver);
        }
    }

    public override void OnLoseFocus()
    {
        _outline.enabled = false;
    }

}
