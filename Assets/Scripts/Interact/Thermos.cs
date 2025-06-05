
public class Thermos : Interactable
{
    public override void OnFocus()
    {
        if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
        {
            Message = "Thermos";
        }
        else
        {
            Message = "Термос";
        }
        _outline.enabled = true;
    }

    public override void OnInteract()
    {
        if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
        {
            Message = "Warm coffee, as usual";
        }
        else
        {
            Message = "Теплый кофе, как обычно";
        }
    }

    public override void OnLoseFocus()
    {
        _outline.enabled = false;
    }
}
