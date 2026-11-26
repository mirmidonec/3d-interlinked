using UnityEngine;

/// <summary>
/// Служебный диктофон. ПКМ — вставить кассету (до включения устройства допроса).
/// Кассета тратится при вставке. Во время допроса горит лампочка записи.
/// </summary>
public class Recorder : Interactable
{
    [Header("Visuals / Audio (все необязательны)")]
    [SerializeField] private AudioSource _source;
    [SerializeField] private AudioClip _insertClip;
    [SerializeField] private GameObject _recLight;       // красная лампочка "REC"
    [SerializeField] private GameObject _cassetteModel;  // модель кассеты в диктофоне

    private void Update()
    {
        bool loaded = GameState.TapeLoaded;
        bool recording = loaded && Device.Instance != null && Device.Instance.IsOn;

        if (_cassetteModel != null && _cassetteModel.activeSelf != loaded)
            _cassetteModel.SetActive(loaded);

        if (_recLight != null && _recLight.activeSelf != recording)
            _recLight.SetActive(recording);
    }

    public override void OnFocus()
    {
        Message = Loc.T("Service recorder", "Служебный диктофон");
        _outline.enabled = true;
    }

    public override void OnInteract()
    {
        if (GameState.TapeLoaded)
        {
            Message = Loc.T("The tape is in. It records the interrogation", "Кассета вставлена. Допрос будет записан");
        }
        else
        {
            Message = Loc.T(
                $"Tapes: {GameState.Cassettes}. [RMB] to insert",
                $"Кассет: {GameState.Cassettes}. [ПКМ] — вставить");
        }
    }

    public override void OnInteractAlt()
    {
        if (GameState.TapeLoaded)
        {
            Message = Loc.T("A tape is already inside", "Кассета уже вставлена");
            return;
        }

        if (Device.Instance != null && Device.Instance.IsOn)
        {
            Message = Loc.T("Too late, the interrogation has started", "Поздно, допрос уже начался");
            return;
        }

        if (GameState.Cassettes <= 0)
        {
            Message = Loc.T("No tapes left. Buy more between days", "Кассет нет. Купи между днями");
            return;
        }

        if (GameState.TryLoadTape())
        {
            if (_source != null && _insertClip != null)
                _source.PlayOneShot(_insertClip);

            Message = Loc.T("Tape inserted", "Кассета вставлена");
        }
    }

    public override void OnLoseFocus()
    {
        _outline.enabled = false;
    }
}