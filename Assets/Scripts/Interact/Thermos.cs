
public class Thermos : Interactable
{
    public override void OnFocus()
    {
        Message = "Thermos";
        _outline.enabled = true;
    }

    public override void OnInteract()
    {
        Message = "Warm coffee, as usual";
    }

    public override void OnLoseFocus()
    {
        _outline.enabled = false;
    }
}
