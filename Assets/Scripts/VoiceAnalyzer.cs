using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Мини-игра "анализ голоса" на экране компьютера.
/// Крутишь частоту (колесо мыши или A/D, Shift = точнее). Когда сигнал пойман,
/// число перестаёт прыгать и показывает вероятность репликанта.
/// Повесь на объект в world-space canvas монитора и назначь ссылки.
/// </summary>
public class VoiceAnalyzer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private kalupashka wave;
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text percentText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text hintText;

    [Header("Controls")]
    [SerializeField] private float wheelStep = 0.02f;
    [SerializeField] private float keySpeed = 0.35f;

    [Header("Lock-on")]
    [SerializeField] private float lockThreshold = 0.9f;
    [SerializeField] private float lockTime = 1.2f;

    [Header("Audio (необязательно)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip lockClip;

    private float _truthPercent;
    private float _lockTimer;
    private bool _locked;

    private void Start()
    {
        bool isReplicant = NpcChoiser.Instance != null
                           && NpcChoiser.Instance.currentNpc != null
                           && NpcChoiser.Instance.currentNpc.IsReplicant;

        // В 25% случаев запись неоднозначна: игроку придётся опираться на пульс и зрачок.
        if (Random.value < 0.25f)
            _truthPercent = Random.Range(40f, 60f);
        else
            _truthPercent = isReplicant ? Random.Range(68f, 94f) : Random.Range(6f, 32f);

        float ideal = Random.Range(0.15f, 0.85f);
        wave.SetIdeal(ideal);

        float start;
        do { start = Random.value; } while (Mathf.Abs(start - ideal) < 0.3f);
        slider.normalizedValue = start;

        if (hintText != null)
            hintText.text = Loc.T(
                "[A/D / wheel] frequency   [Shift] fine   [Q] back",
                "[A/D / колесо] частота   [Shift] точнее   [Q] назад");
    }

    private void Update()
    {
        if (!CameraTranzition.InPC || CameraTranzition.InTransition) return;

        if (!GameState.TapeLoaded)
        {
            percentText.text = "--";
            statusText.text = Loc.T("NO TAPE", "НЕТ ЗАПИСИ");
            return;
        }

        if (!_locked) HandleInput();

        float q = wave.MatchQuality;
        float shown;

        if (_locked)
        {
            shown = _truthPercent;
        }
        else
        {
            float noise = Mathf.PerlinNoise(Time.time * 6f, 3.7f) * 100f;
            shown = Mathf.Lerp(noise, _truthPercent, q * q);

            if (q >= lockThreshold)
            {
                _lockTimer += Time.deltaTime;
                if (_lockTimer >= lockTime) LockOn();
            }
            else
            {
                _lockTimer = 0f;
            }
        }

        string col = "#FFFFFF";
        if (_locked) col = shown >= 60f ? "#FF6B6B" : (shown <= 40f ? "#7CFC9A" : "#FFC857");

        percentText.text = $"<color={col}>{shown:F0}%</color>";
        statusText.text = _locked
            ? Loc.T("SIGNAL LOCKED. Replicant probability", "СИГНАЛ ЗАХВАЧЕН. Вероятность репликанта")
            : Loc.T($"Signal {q * 100f:F0}%", $"Сигнал {q * 100f:F0}%");
    }

    private void HandleInput()
    {
        float delta = Input.mouseScrollDelta.y * wheelStep;

        float axis = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) axis -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) axis += 1f;

        float fine = Input.GetKey(KeyCode.LeftShift) ? 0.25f : 1f;
        delta += axis * keySpeed * fine * Time.deltaTime;

        if (!Mathf.Approximately(delta, 0f))
            slider.normalizedValue = Mathf.Clamp01(slider.normalizedValue + delta);
    }

    private void LockOn()
    {
        _locked = true;
        GameState.TapeAnalyzed = true;
        if (audioSource != null && lockClip != null)
            audioSource.PlayOneShot(lockClip);
    }
}
