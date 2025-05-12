
public class Notebook : Interactable
{
    private Outline _outline;
    
    private void Awake()
    {
        _outline = GetComponent<Outline>();
    }

    public override void OnFocus()
    {
        Message = "My notebook";
        _outline.ActiveOutline();
    }

    public override void OnInteract()
    {
        Message = "He keeps many secrets";
        _outline.ActivatedOutline();
    }

    public override void OnLoseFocus()
    {
    }

    


}
