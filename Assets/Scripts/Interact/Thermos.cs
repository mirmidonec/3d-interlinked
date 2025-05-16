
public class Thermos : Interactable
{
    public override void OnFocus()
    {
        Message = "Thermos";
        _outline.ActiveOutline();
    }

    public override void OnInteract()
    {
        Message = "Warm coffee, as usual";
        _outline.ActivatedOutline();
    }

    public override void OnLoseFocus()
    {
        _outline.DisableOutline();
    }
}
