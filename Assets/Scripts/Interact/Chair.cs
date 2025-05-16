

public class Chair : Interactable
{
    public override void OnFocus()
    {
        Message = "Chair";
        _outline.ActiveOutline();
    }

    public override void OnInteract()
    {
        Message = "Inspect the room before sit down";
        _outline.ActivatedOutline();
    }

    public override void OnLoseFocus()
    {
        _outline.DisableOutline();
    }
}
