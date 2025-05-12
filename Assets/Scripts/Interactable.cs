using UnityEngine;
using System.Collections;
using System;
public abstract class Interactable : MonoBehaviour
{
    private string _text;
    public event Action<string> OnTextChanged;

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
    public abstract void OnFocus();
    public abstract void OnLoseFocus();
    public abstract void OnInteract();
}