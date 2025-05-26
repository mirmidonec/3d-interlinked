

public class Chair : Interactable
{
    public PlayerController Player;
    public override void OnFocus()
    {
        if (Player == null)
        {
            Message = "Chair";
            _outline.enabled = true;
             return;
        }
        if (Player.isSitting)
        {
            Message = "";
            _outline.enabled = false;
            return;
        }

        Message = "Chair";
        _outline.enabled = true;
    }

    public override void OnInteract()
    {
        if (Player == null)
        {
            Message = "Sit down will trigger the start of interview";
            return;
        }
        if (Player.isSitting)
        {
            Message = "";
           _outline.enabled = false;
            return;
        }

        Message = "Sit down will trigger the start of interview";
        _outline.enabled = true;
    }

    public override void OnInteractAlt()
    {
        if (Player == null)
        {
            Message = "It's not your chair";
            _outline.enabled = true;
            return;
        }

        if (!Player.isSitting)
        {
            Player.HandleSit();
            Message = "";
            _outline.enabled = false;
        }
    }

    public override void OnLoseFocus()
    {
        _outline.enabled = false;
        Message = "";
    }
}
