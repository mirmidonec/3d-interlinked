
public class Notebook : Interactable
{
    public override void OnFocus()
    {
        if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
        {
            Message = "Book of office";
        }
        else
        {
            Message = "Служебная книга";
        }
        _outline.enabled = true;
    }

    public override void OnInteract()
    {
        if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
        {
            Message = "Stores all interrogation protocols";
        }
        else
        {
            Message = "Хранит все протоколы допросов";
        }
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
