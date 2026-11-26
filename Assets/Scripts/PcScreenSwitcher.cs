using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Переключает экраны на мониторе ПОСЛЕ регистрации: текстовый журнал и анализатор голоса.
/// Регистрацию не трогает (пока Connection.panel активна, скрипт ничего не делает).
///
/// Повесь на ОТДЕЛЬНЫЙ всегда активный объект (например, на корень canvas монитора),
/// НЕ на logScreen и НЕ на voiceScreen: их он выключает.
/// </summary>
public class PcScreenSwitcher : MonoBehaviour
{
    public static PcScreenSwitcher Instance { get; private set; }

    public enum Screen { Log, Voice }

    [Header("Screens")]
    [Tooltip("Журнал ответов: тот же объект, что Connection.logdialog")]
    [SerializeField] private GameObject logScreen;
    [Tooltip("Корневой объект анализатора голоса (kalupashka + VoiceAnalyzer + тексты)")]
    [SerializeField] private GameObject voiceScreen;

    [Header("Registration")]
    [Tooltip("Нужен только чтобы понять, что регистрация пройдена (Connection.panel)")]
    [SerializeField] private Connection connection;

    [Header("UI (необязательно)")]
    [Tooltip("Подсказка внизу монитора, должна лежать ВНЕ logScreen и voiceScreen")]
    [SerializeField] private TMP_Text tabHint;
    [SerializeField] private Button logTabButton;
    [SerializeField] private Button voiceTabButton;

    [Header("Controls")]
    [SerializeField] private KeyCode switchKey = KeyCode.Tab;

    private Screen _current = Screen.Log;
    private bool _pendingVoice;
    private bool _wasInPc;
    private float _denyTimer;

    // Регистрация пройдена, когда панель регистрации выключена.
    private bool LoggedIn => connection != null && connection.panel != null && !connection.panel.activeSelf;

    // Анализ голоса: только с кассетой и только пока идёт финальный выбор
    // (после вердикта анализ бесполезен, а бонус можно было бы "добыть" задним числом).
    public bool VoiceAvailable =>
        GameState.TapeLoaded &&
        DialogueSystem.instance != null &&
        DialogueSystem.instance.isFinaleStart;

    private void Awake()
    {
        Instance = this;

        if (voiceScreen != null) voiceScreen.SetActive(false);

        if (logTabButton != null) logTabButton.onClick.AddListener(() => Show(Screen.Log));
        if (voiceTabButton != null) voiceTabButton.onClick.AddListener(TryShowVoice);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Вызывается кнопкой "Прослушать запись": при входе за ПК сразу откроется анализатор.</summary>
    public void RequestVoice()
    {
        _pendingVoice = true;
    }

    private void Update()
    {
        bool inPc = CameraTranzition.InPC;

        if (inPc && !_wasInPc)
        {
            Show(_pendingVoice && VoiceAvailable ? Screen.Voice : Screen.Log);
            _pendingVoice = false;
        }
        else if (!inPc && _wasInPc)
        {
            Show(Screen.Log);
            _pendingVoice = false;
        }
        _wasInPc = inPc;

        bool interactive = inPc && !CameraTranzition.InTransition && LoggedIn;

        if (tabHint != null)
        {
            tabHint.gameObject.SetActive(interactive);
            if (interactive) tabHint.text = BuildHint();
        }

        if (!interactive) return;

        if (_denyTimer > 0f) _denyTimer -= Time.deltaTime;

        if (Input.GetKeyDown(switchKey))
        {
            if (_current == Screen.Log) TryShowVoice();
            else Show(Screen.Log);
        }
    }

    private void TryShowVoice()
    {
        if (VoiceAvailable)
        {
            Show(Screen.Voice);
        }
        else
        {
            _denyTimer = 2f;   // на 2 секунды показываем причину отказа
        }
    }

    private void Show(Screen screen)
    {
        _current = screen;

        // До регистрации ничего не включаем, иначе журнал появится поверх формы входа.
        if (!LoggedIn)
        {
            if (voiceScreen != null) voiceScreen.SetActive(false);
            return;
        }

        if (logScreen != null) logScreen.SetActive(screen == Screen.Log);
        if (voiceScreen != null) voiceScreen.SetActive(screen == Screen.Voice);
    }

    private string BuildHint()
    {
        if (_current == Screen.Voice)
            return Loc.T($"[{switchKey}] Interrogation log", $"[{switchKey}] Журнал допроса");

        if (VoiceAvailable)
            return Loc.T($"[{switchKey}] Voice analysis", $"[{switchKey}] Анализ голоса");

        if (_denyTimer > 0f)
        {
            if (!GameState.TapeLoaded)
                return Loc.T("No tape was recorded", "Запись не велась: кассета не была вставлена");
            return Loc.T("Available before the verdict", "Доступно перед вердиктом");
        }

        return Loc.T($"[{switchKey}] Voice analysis (locked)", $"[{switchKey}] Анализ голоса (недоступно)");
    }
}
