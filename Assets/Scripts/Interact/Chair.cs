

public class Chair : Interactable
{
    public PlayerController Player;
    public override void OnFocus()
    {
        if (Player.isSitting && Player != null)
        {
            Message = " ";
            return;
        }
        Message = "Chair";
        _outline.ActiveOutline();
    }

    public override void OnInteract()
    {
        if (Player.isSitting && Player != null)
        {
            Message = " ";
            return;
        }
        Message = "Inspect the room before sit down";
        _outline.ActivatedOutline();
    }
    public override void OnInteractAlt()
    {
        if (Player != null && !Player.isSitting)
        {
            Player.HandleSit();
        }
        else
        {
            Message = "It's not your sit";
            _outline.ActivatedOutline();
        }
    }

    public override void OnLoseFocus()
    {
        _outline.DisableOutline();
    }
}
