using UnityEngine;
using System.Collections;
using System;
public class Interactable : MonoBehaviour
{
    private string _text;
    public event Action<string> OnTextChanged;
    protected Outline _outline;

    protected virtual void Awake()
    {
        _outline = GetComponent<Outline>();
    }

    public string Message
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                OnTextChanged?.Invoke(_text);
            }
        }
    }
    public virtual void OnFocus()
    {
        
    }
    public virtual void OnLoseFocus()
    {
        
    }
    public virtual void OnInteract()
    {
        
    }
    public virtual void OnInteractAlt()
    {
        
    }
}