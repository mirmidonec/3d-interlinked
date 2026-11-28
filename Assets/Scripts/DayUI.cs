using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Вся UI системы дней. Строится кодом в Awake, руками собирать ничего не нужно.
/// Повесь на пустой объект "DayUI" в игровой сцене и назначь шрифт с кириллицей.
///
/// Конец дня запускается ОТСЮДА: FinishDay(задержка). Его вызывают вердикт "человек",
/// выстрел, нехватка патронов. Цепочка "NPC уходит / The end / затухание" больше не нужна.
/// </summary>
public class DayUI : MonoBehaviour
{
    public static DayUI Instance { get; private set; }

    [Header("Look")]
    [Tooltip("Тот же TMP-шрифт, что и в остальной игре (должен содержать кириллицу)")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Color accent = new Color(0.25f, 0.85f, 1f);

    [Header("References (найдутся сами, если пусто)")]
    [SerializeField] private PlayerController player;

    [Header("Audio (необязательно)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip tickSound;
    [SerializeField] private AudioClip goodSound;
    [SerializeField] private AudioClip badSound;
    [SerializeField] private AudioClip buySound;
    [SerializeField] private AudioClip denySound;

    public TMP_FontAsset Font => font;

    private const string Green = "#7CFC9A";
    private const string Red = "#FF6B6B";
    private const string Amber = "#FFC857";
    private const string Dim = "#7F8A96";

    private static readonly Color GreenC = new Color(0.49f, 0.99f, 0.60f);
    private static readonly Color RedC = new Color(1f, 0.42f, 0.42f);
    private static readonly Color AmberC = new Color(1f, 0.78f, 0.34f);
    private static readonly Color PanelBg = new Color(0.03f, 0.05f, 0.08f, 0.82f);
    private static readonly Color RowBg = new Color(0.08f, 0.11f, 0.15f, 0.95f);
    private static readonly Color TextMain = new Color(0.90f, 0.93f, 0.96f);
    private static readonly Color TextDim = new Color(0.55f, 0.60f, 0.67f);
    private static readonly Color Overlay = new Color(0.02f, 0.03f, 0.05f, 0.97f);
    private static readonly Color PipOff = new Color(1f, 1f, 1f, 0.12f);

    private Canvas _canvas;
    private CanvasGroup _hudGroup, _cardGroup, _toastGroup, _reportGroup, _shopGroup, _endGroup, _fadeGroup, _flashGroup;
    private RectTransform _hudRoot, _cardLine;
    private TMP_Text _hudTitle, _hudMoney;
    private RectTransform _recPill;
    private Image _recDot;
    private TMP_Text _recText;
    private StatRow _ammoRow, _tapeRow, _strikeRow;
    private RectTransform _questPanel;
    private CanvasGroup _questGroup;
    private TMP_Text _questHeader, _questName;
    private Image _questStrip;
    private TMP_Text _cardText, _toastText;
    private TMP_Text _reportSub, _reportTitle;
    private RectTransform _reportRows;
    private Button _reportButton;
    private TMP_Text _shopMoney, _shopNotice, _shopInfo;
    private ShopRow _shopAmmo, _shopTape;
    private Button _shopNextBtn;
    private CanvasGroup _shopTitleGroup, _shopNextGroup;
    private TMP_Text _endTitle, _endBody;
    private Button _endButton;

    private class StatRow
    {
        public TMP_Text label, value;
        public Image[] pips;
    }

    private class ShopRow
    {
        public CanvasGroup group;
        public TMP_Text name, owned;
        public Button buy;
    }

    private class QRow
    {
        public RectTransform root;
        public Image box, tick;
        public TMP_Text label;
        public bool done;
        public float k;
    }

    private const int MaxQuestRows = 6;
    private readonly List<QRow> _qRows = new List<QRow>();
    private readonly List<string> _vTexts = new List<string>();
    private readonly List<bool> _vDone = new List<bool>();
    private string _questKey = "";

    private bool _locked;       // true после конца дня: HUD скрыт, UI дня занят
    private bool _finishing;
    private bool _shopOpen;
    private int _shopSig = -1;
    private float _noticeTimer;
    private float _shownMoney = -1f;
    private int _lastMoney = -1, _lastAmmo = -1, _lastTapes = -1, _lastStrikes = -1;

    // ====================================================================
    // Жизненный цикл
    // ====================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Статики переживают перезагрузку сцены. Если Ending не запустит Start (объект выключен),
        // HUD, квесты и пауза остались бы скрытыми навсегда. Поэтому сбрасываем здесь.
        Ending.IsEnding = false;
        Time.timeScale = 1f;

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        Build();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (player == null) player = FindObjectOfType<PlayerController>();

        GameState.BeginDay();
        StartCoroutine(BootstrapRoutine());
        StartCoroutine(DayStartRoutine());
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        bool dialogue = player != null && player.IsDialogue;
        bool show = !_locked && !IntroText.isIntro && !Ending.IsEnding && !CameraTranzition.InPC && !dialogue;
        _hudGroup.alpha = Mathf.MoveTowards(_hudGroup.alpha, show ? 1f : 0f, dt * 4f);
        _hudRoot.anchoredPosition = new Vector2(Mathf.Lerp(0f, 40f, UiFx.EaseOutCubic(_hudGroup.alpha)), -40f);

        UpdateHud(dt);
        UpdateQuest(dt);

        // Магазин сам подхватывает любые изменения счётчиков.
        if (_shopOpen)
        {
            if (ShopSignature() != _shopSig) RefreshShop();
            float target = GameState.Money;
            _shownMoney = Mathf.MoveTowards(_shownMoney, target, Mathf.Max(80f, Mathf.Abs(target - _shownMoney) * 5f) * dt);
            SetText(_shopMoney, $"{Loc.T("Balance", "Баланс")}   <color={Green}>${Mathf.RoundToInt(_shownMoney)}</color>");
        }

        if (_noticeTimer > 0f)
        {
            _noticeTimer -= dt;
            if (_noticeTimer <= 0f) _shopNotice.text = "";
        }
    }

    // ====================================================================
    // Публичное API
    // ====================================================================

    /// <summary>
    /// Закончить день: через delay секунд открывается отчёт. Безопасно вызывать несколько раз.
    /// Вызывают: вердикт "человек", смерть подозреваемого, кончились патроны, Ending.StartEnd.
    /// </summary>
    public void FinishDay(float delay = 0f)
    {
        if (_finishing) return;
        _finishing = true;
        Ending.IsEnding = true;   // блокирует паузу и HUD до перезагрузки сцены
        StartCoroutine(FinishRoutine(delay));
    }

    private IEnumerator FinishRoutine(float delay)
    {
        try
        {
            Connection con = FindObjectOfType<Connection>();
            if (con != null) con.SaveProgress();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DayUI] SaveProgress failed: " + e);
        }

        LockPlayer();

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;

        yield return new WaitForSecondsRealtime(delay);
        ShowDayEnd();
    }

    public void ShowDayEnd()
    {
        if (_locked) return;
        _locked = true;
        _finishing = true;
        Ending.IsEnding = true;
        Debug.Log("[DayUI] Показываю отчёт дня");
        StartCoroutine(ReportRoutine());
    }

    public void Toast(string text, float time = 3f)
    {
        StartCoroutine(ToastRoutine(text, time));
    }

    // ====================================================================
    // Старт дня
    // ====================================================================

    private IEnumerator DayStartRoutine()
    {
        string title = $"{Loc.T("DAY", "ДЕНЬ")} {GameState.Day}/{GameState.TotalDays}";
        string sub = Loc.T("One suspect. One night.", "Один подозреваемый. Одна ночь.");

        if (GameState.Day > 1)
        {
            _cardText.text = $"{title}\n<size=42%><color={Dim}>{sub}</color></size>";
            SetGroup(_cardGroup, true);
            _cardLine.sizeDelta = new Vector2(0f, 3f);

            float t = 0f;
            while (t < 0.7f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.7f);
                _cardText.transform.localScale = Vector3.one * Mathf.LerpUnclamped(1.25f, 1f, UiFx.EaseOutCubic(k));
                _cardLine.sizeDelta = new Vector2(Mathf.Lerp(0f, 560f, UiFx.EaseOutCubic(k)), 3f);
                yield return null;
            }
            yield return new WaitForSecondsRealtime(1.3f);
            yield return Fade(_cardGroup, 1f, 0f, 0.7f);
            SetGroup(_cardGroup, false);
        }
        else
        {
            // День 1: поверх идёт интро, поэтому просто плашка после него.
            yield return null;
            yield return new WaitUntil(() => !IntroText.isIntro);
            yield return new WaitForSecondsRealtime(3.5f);
            yield return ToastRoutine($"{title}\n<size=60%>{sub}</size>", 3f);
        }
    }

    private IEnumerator ToastRoutine(string text, float time)
    {
        _toastText.text = text;
        RectTransform rt = (RectTransform)_toastGroup.transform;
        Vector2 basePos = new Vector2(0f, -110f);

        float t = 0f;
        while (t < 0.45f)
        {
            t += Time.unscaledDeltaTime;
            float k = UiFx.EaseOutCubic(t / 0.45f);
            _toastGroup.alpha = k;
            rt.anchoredPosition = basePos + new Vector2(0f, Mathf.Lerp(40f, 0f, k));
            yield return null;
        }
        _toastGroup.alpha = 1f;
        rt.anchoredPosition = basePos;

        yield return new WaitForSecondsRealtime(time);
        yield return Fade(_toastGroup, 1f, 0f, 0.6f);
    }

    // ====================================================================
    // Конец дня: отчёт
    // ====================================================================

    private IEnumerator ReportRoutine()
    {
        DayReport r = GameState.SettleDay();

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;

        for (int i = _reportRows.childCount - 1; i >= 0; i--)
            Destroy(_reportRows.GetChild(i).gameObject);

        _reportSub.text = "";
        _reportTitle.text = "";
        _reportButton.gameObject.SetActive(false);

        SetGroup(_reportGroup, true);
        _reportGroup.alpha = 0f;
        yield return Fade(_reportGroup, 0f, 1f, 0.35f);

        // 1. Кто был подозреваемым
        string who = GameState.SuspectIsReplicant ? Loc.T("REPLICANT", "РЕПЛИКАНТ") : Loc.T("HUMAN", "ЧЕЛОВЕК");
        string whoCol = GameState.SuspectIsReplicant ? Red : Green;
        _reportSub.text = $"{Loc.T("The suspect was", "Подозреваемый")}: <color={whoCol}>{who}</color>";
        StartCoroutine(UiFx.Punch(_reportSub.transform, 0.12f, 0.4f));
        yield return new WaitForSecondsRealtime(0.9f);

        // 2. Верно ли
        if (!GameState.VerdictMade)
            _reportTitle.text = $"<color={Red}>{Loc.T("NO VERDICT", "ВЕРДИКТ НЕ ВЫНЕСЕН")}</color>";
        else if (r.Correct)
            _reportTitle.text = $"<color={Green}>{Loc.T("VERDICT CONFIRMED", "ВЕРДИКТ ВЕРЕН")}</color>";
        else
            _reportTitle.text = $"<color={Red}>{Loc.T("VERDICT WRONG", "ВЕРДИКТ ОШИБОЧЕН")}</color>";

        StartCoroutine(UiFx.Punch(_reportTitle.transform, 0.22f, 0.5f));
        StartCoroutine(FlashRoutine(r.Correct ? GreenC : RedC, 0.28f));
        if (!r.Correct) StartCoroutine(UiFx.Shake((RectTransform)_reportTitle.transform.parent, 14f, 0.45f));
        Play(r.Correct ? goodSound : badSound);
        yield return new WaitForSecondsRealtime(0.8f);

        // 3. Деньги построчно
        foreach (ReportLine line in r.Lines)
        {
            string col = line.Amount >= 0 ? Green : Red;
            string sign = line.Amount >= 0 ? "+" : "-";
            AddRow(line.Label, $"<color={col}>{sign}${Mathf.Abs(line.Amount)}</color>");
            Play(tickSound);
            yield return new WaitForSecondsRealtime(0.38f);
        }

        // 3b. Пояснения: какой бонус не получен и почему
        foreach (string note in r.Missed)
        {
            AddNote(note);
            yield return new WaitForSecondsRealtime(0.3f);
        }

        // 4. Итог дня и баланс: сначала сколько заработано за день, потом сколько стало всего
        int delta = r.MoneyAfter - r.MoneyBefore;
        string dCol = delta >= 0 ? Green : Red;
        string dSign = delta >= 0 ? "+" : "-";
        AddRow($"<b>{Loc.T("Day total", "Итого за день")}</b>", $"<color={dCol}><b>{dSign}${Mathf.Abs(delta)}</b></color>");
        Play(tickSound);
        yield return new WaitForSecondsRealtime(0.45f);

        TMP_Text balanceValue;
        AddRow(Loc.T("Balance", "Баланс"), "", out balanceValue);
        string was = $"<size=58%><color={Dim}>{Loc.T("was", "было")} ${r.MoneyBefore}</color></size>";
        float t = 0f;
        while (t < 0.9f)
        {
            t += Time.unscaledDeltaTime;
            int cur = Mathf.RoundToInt(Mathf.Lerp(r.MoneyBefore, r.MoneyAfter, UiFx.EaseOutCubic(t / 0.9f)));
            balanceValue.text = $"<b>${cur}</b>   {was}";
            yield return null;
        }
        balanceValue.text = $"<b>${r.MoneyAfter}</b>   {was}";
        StartCoroutine(UiFx.Punch(balanceValue.transform, 0.12f, 0.35f));
        yield return new WaitForSecondsRealtime(0.4f);

        // 5. Ошибки
        var strikes = new StringBuilder();
        for (int i = 0; i < GameState.MaxStrikes; i++)
            strikes.Append(i < GameState.Strikes ? $"<color={Red}>X</color> " : $"<color={Dim}>-</color> ");
        AddRow(Loc.T("Mistakes", "Ошибки"), strikes.ToString().TrimEnd());
        Play(tickSound);
        yield return new WaitForSecondsRealtime(0.5f);

        if (r.GameOver)
        {
            AddRow($"<color={Red}>{Loc.T("The department is closing your case.", "Отдел закрывает ваше дело.")}</color>", "");
            yield return new WaitForSecondsRealtime(0.5f);
        }

        SetLabel(_reportButton, (r.GameOver || r.RunComplete)
            ? Loc.T("Continue", "Далее")
            : Loc.T("To the shop", "В магазин"));
        _reportButton.gameObject.SetActive(true);
        CanvasGroup bg = _reportButton.GetComponent<CanvasGroup>();
        if (bg != null) StartCoroutine(UiFx.Appear(bg, 0f, 0.35f));
    }

    private CanvasGroup AddRow(string left, string right)
    {
        TMP_Text unused;
        return AddRow(left, right, out unused);
    }

    private CanvasGroup AddRow(string left, string right, out TMP_Text rightText)
    {
        RectTransform row = NewRect("Row", _reportRows);
        var bg = row.gameObject.AddComponent<Image>();
        bg.sprite = UiFx.Rounded(10);
        bg.type = Image.Type.Sliced;
        bg.color = new Color(1f, 1f, 1f, 0.05f);
        bg.raycastTarget = false;

        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(22, 22, 6, 6);
        h.spacing = 16f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;
        UiFx.Layout(row.gameObject, minH: 52f, prefH: 52f);

        TMP_Text l = MakeText(row, "Label", 32, TextAlignmentOptions.MidlineLeft, TextMain);
        l.text = left;
        UiFx.Layout(l.gameObject, flexW: 1f);

        rightText = MakeText(row, "Value", 32, TextAlignmentOptions.MidlineRight, Color.white);
        rightText.text = right;
        UiFx.Layout(rightText.gameObject, minW: 340f, prefW: 340f);

        var cg = row.gameObject.AddComponent<CanvasGroup>();
        StartCoroutine(UiFx.Appear(cg, 0f, 0.28f));
        return cg;
    }

    private IEnumerator FlashRoutine(Color c, float peak)
    {
        _flashGroup.GetComponent<Image>().color = c;
        float t = 0f;
        while (t < 0.55f)
        {
            t += Time.unscaledDeltaTime;
            _flashGroup.alpha = peak * (1f - UiFx.EaseOutCubic(t / 0.55f));
            yield return null;
        }
        _flashGroup.alpha = 0f;
    }

    private void OnReportContinue()
    {
        DayReport r = GameState.SettleDay();
        SetGroup(_reportGroup, false);

        if (r.GameOver) ShowEnd(false, r);
        else if (r.RunComplete) ShowEnd(true, r);
        else OpenShop();
    }

    // ====================================================================
    // Магазин
    // ====================================================================

    private int ShopSignature()
    {
        return GameState.Money * 10000 + GameState.Ammo * 100 + GameState.Cassettes + GameState.Day * 7919;
    }

    private void OpenShop()
    {
        _shopOpen = true;
        _shownMoney = GameState.Money;
        SetGroup(_shopGroup, true);
        _shopNotice.text = "";
        RefreshShop();

        StartCoroutine(UiFx.Appear(_shopTitleGroup, 0.00f, 0.4f));
        StartCoroutine(UiFx.Appear(_shopAmmo.group, 0.12f, 0.4f));
        StartCoroutine(UiFx.Appear(_shopTape.group, 0.24f, 0.4f));
        StartCoroutine(UiFx.Appear(_shopNextGroup, 0.36f, 0.4f));
    }

    private void RefreshShop()
    {
        _shopSig = ShopSignature();

        SetText(_shopAmmo.name,
            $"{Loc.T("Bullet", "Патрон")}\n<size=60%><color={Dim}>{Loc.T("For the service revolver", "Для служебного револьвера")}</color></size>");
        SetText(_shopAmmo.owned, $"{GameState.Ammo} / {GameState.MaxAmmo}");
        SetBuyLabel(_shopAmmo.buy, GameState.AmmoPrice, GameState.CanBuyAmmo);

        SetText(_shopTape.name,
            $"{Loc.T("Cassette", "Кассета")}\n<size=60%><color={Dim}>{Loc.T("Records the interrogation, unlocks voice analysis", "Запись допроса и анализ голоса")}</color></size>");
        SetText(_shopTape.owned, $"{GameState.Cassettes} / {GameState.MaxCassettes}");
        SetBuyLabel(_shopTape.buy, GameState.CassettePrice, GameState.CanBuyCassette);

        SetLabel(_shopNextBtn,
            $"{Loc.T("Next day", "Следующий день")}  ({GameState.Day + 1}/{GameState.TotalDays})");
    }

    private void SetBuyLabel(Button b, int price, bool can)
    {
        string text = $"{Loc.T("BUY", "КУПИТЬ")}   ${price}";
        SetLabel(b, can ? text : $"<color={Dim}>{text}</color>");
    }

    private void Buy(bool ammo)
    {
        bool ok = ammo ? GameState.TryBuyAmmo() : GameState.TryBuyCassette();
        Play(ok ? buySound : denySound);

        ShopRow row = ammo ? _shopAmmo : _shopTape;
        if (ok)
        {
            Notice(ammo ? Loc.T("+1 bullet", "+1 патрон") : Loc.T("+1 cassette", "+1 кассета"), Green);
            StartCoroutine(UiFx.Punch(row.owned.transform, 0.25f, 0.3f));
        }
        else
        {
            bool full = ammo ? GameState.Ammo >= GameState.MaxAmmo : GameState.Cassettes >= GameState.MaxCassettes;
            Notice(full ? Loc.T("You can't carry more", "Больше не поместится")
                        : Loc.T("Not enough money", "Не хватает денег"), Red);
            StartCoroutine(UiFx.Shake((RectTransform)row.group.transform, 8f, 0.3f));
        }

        RefreshShop();
        Debug.Log($"[Shop] ok={ok} money={GameState.Money} ammo={GameState.Ammo} tapes={GameState.Cassettes}");
    }

    private void Notice(string text, string color)
    {
        _shopNotice.text = $"<color={color}>{text}</color>";
        _noticeTimer = 1.8f;
        StartCoroutine(UiFx.Punch(_shopNotice.transform, 0.12f, 0.25f));
    }

    private void OnNextDay()
    {
        _shopNextBtn.interactable = false;
        StartCoroutine(NextDayRoutine());
    }

    private IEnumerator NextDayRoutine()
    {
        SetGroup(_fadeGroup, true);
        _fadeGroup.alpha = 0f;
        yield return Fade(_fadeGroup, 0f, 1f, 0.8f);

        GameState.NextDay();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ====================================================================
    // Конец игры / победа
    // ====================================================================

    private void ShowEnd(bool victory, DayReport r)
    {
        _shopOpen = false;
        SetGroup(_endGroup, true);
        StartCoroutine(UiFx.Appear(_endGroup, 0f, 0.5f));
        StartCoroutine(FlashRoutine(victory ? GreenC : RedC, 0.3f));

        _endTitle.text = victory
            ? $"<color={Green}>{Loc.T("CASE CLOSED", "ДЕЛО ЗАКРЫТО")}</color>"
            : $"<color={Red}>{Loc.T("YOU ARE FIRED", "ВЫ УВОЛЕНЫ")}</color>";
        StartCoroutine(UiFx.Punch(_endTitle.transform, 0.2f, 0.6f));

        string reason = "";
        if (!victory)
        {
            reason = r.OutOfStrikes
                ? Loc.T("Three wrong verdicts.", "Три ошибочных вердикта.")
                : Loc.T("The department ran out of money.", "У отдела закончились деньги.");
            reason += "\n\n";
        }

        _endBody.text = reason +
            $"{Loc.T("Days worked", "Дней отработано")}: {GameState.Day}\n" +
            $"{Loc.T("Correct verdicts", "Верных вердиктов")}: {GameState.CorrectCount}/{GameState.Day}\n" +
            $"{Loc.T("Final balance", "Итоговый баланс")}: ${GameState.Money}";

        SetLabel(_endButton, Loc.T("Exit", "Выход"));
    }

    private void OnEndButton()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ====================================================================
    // HUD
    // ====================================================================

    private void UpdateHud(float dt)
    {
        SetText(_hudTitle, $"{Loc.T("DAY", "ДЕНЬ")} {GameState.Day} / {GameState.TotalDays}");

        // деньги: плавный счётчик + вспышка цвета при изменении
        if (_shownMoney < 0f) _shownMoney = GameState.Money;
        if (!_shopOpen)
            _shownMoney = Mathf.MoveTowards(_shownMoney, GameState.Money,
                Mathf.Max(60f, Mathf.Abs(GameState.Money - _shownMoney) * 4f) * dt);
        SetText(_hudMoney, $"$ {Mathf.RoundToInt(_shownMoney)}");
        _hudMoney.color = Color.Lerp(_hudMoney.color, GreenC, dt * 3f);

        if (_lastMoney >= 0 && GameState.Money != _lastMoney)
        {
            _hudMoney.color = GameState.Money > _lastMoney ? Color.white : RedC;
            StartCoroutine(UiFx.Punch(_hudMoney.transform, 0.18f, 0.35f));
        }
        _lastMoney = GameState.Money;

        // патроны
        string ammoCol = GameState.Ammo <= 0 ? Red : "#FFFFFF";
        SetText(_ammoRow.label, Loc.T("AMMO", "ПАТРОНЫ"));
        SetText(_ammoRow.value, $"<color={ammoCol}>{GameState.Ammo}</color> / {GameState.MaxAmmo}");
        SetPips(_ammoRow, GameState.Ammo, new Color(1f, 1f, 1f, 0.95f), _lastAmmo);
        _lastAmmo = GameState.Ammo;

        // кассеты
        SetText(_tapeRow.label, Loc.T("TAPES", "КАССЕТЫ"));
        SetText(_tapeRow.value, $"{GameState.Cassettes} / {GameState.MaxCassettes}");
        SetPips(_tapeRow, GameState.Cassettes, AmberC, _lastTapes);
        _lastTapes = GameState.Cassettes;

        // плашка "кассета в диктофоне" в шапке
        bool loaded = GameState.TapeLoaded;
        if (_recPill.gameObject.activeSelf != loaded)
        {
            _recPill.gameObject.SetActive(loaded);
            if (loaded) StartCoroutine(UiFx.Punch(_recPill, 0.15f, 0.35f));
        }
        if (loaded)
        {
            SetText(_recText, Loc.T("TAPE LOADED", "КАССЕТА ВСТАВЛЕНА"));
            _recDot.color = new Color(AmberC.r, AmberC.g, AmberC.b, 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f)));
        }

        // ошибки
        SetText(_strikeRow.label, Loc.T("MISTAKES", "ОШИБКИ"));
        SetText(_strikeRow.value, $"{GameState.Strikes} / {GameState.MaxStrikes}");
        SetPips(_strikeRow, GameState.Strikes, RedC, _lastStrikes);
        _lastStrikes = GameState.Strikes;
    }

    private void SetPips(StatRow row, int filled, Color on, int previous)
    {
        for (int i = 0; i < row.pips.Length; i++)
        {
            bool isOn = i < filled;
            row.pips[i].color = isOn ? on : PipOff;
            if (previous >= 0 && i >= Mathf.Min(previous, filled) && i < Mathf.Max(previous, filled) && previous != filled)
            {
                if (row.pips[i].transform.localScale.x < 1.01f)
                    StartCoroutine(UiFx.Punch(row.pips[i].transform, 0.6f, 0.35f));
            }
        }
    }

    /// <summary>
    /// Данные для панели заданий. Шаги НЕ последовательные: показываем все сразу как чеклист.
    /// Если активного задания нет, но игроку пора садиться, подставляем "Сядь в кресло".
    /// </summary>
    private bool GetQuestView(out string title, out bool pulse)
    {
        title = null;
        pulse = false;
        _vTexts.Clear();
        _vDone.Clear();

        QuestManager qm = QuestManager.Instance;
        if (qm != null && qm.HasActiveQuest)
        {
            title = qm.CurrentQuestTitle;
            string[] t = qm.CurrentStepTexts;
            bool[] d = qm.CurrentStepsDone;
            for (int i = 0; i < t.Length && i < MaxQuestRows; i++)
            {
                _vTexts.Add(t[i]);
                _vDone.Add(d != null && i < d.Length && d[i]);
            }
            return true;
        }

        bool inspected = GameState.Day > 1 || (qm != null && qm.IsQuestCompletedBefore(0));
        bool needSit = !_locked && !Ending.IsEnding && !GameState.VerdictMade
                       && player != null && !player.isSitting && inspected
                       && !IntroText.isIntro;
        if (needSit)
        {
            title = Loc.T("Interrogation", "Допрос");
            _vTexts.Add(Loc.T("Sit down in the chair", "Сядь в кресло"));
            _vDone.Add(false);
            pulse = true;
            return true;
        }
        return false;
    }

    private void UpdateQuest(float dt)
    {
        bool has = GetQuestView(out string title, out bool pulse);
        _questGroup.alpha = Mathf.MoveTowards(_questGroup.alpha, has ? 1f : 0f, dt * 4f);
        if (!has) return;

        string key = title + "|" + _vTexts.Count;
        bool isNew = key != _questKey;
        if (isNew)
        {
            _questKey = key;
            StartCoroutine(UiFx.Punch(_questPanel, 0.03f, 0.4f));
            StartCoroutine(StripFlash());
        }

        SetText(_questHeader, Loc.T("OBJECTIVE", "ЗАДАНИЕ"));
        SetText(_questName, title);

        float pulseA = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4.5f);
        for (int i = 0; i < _qRows.Count; i++)
        {
            QRow r = _qRows[i];
            bool on = i < _vTexts.Count;
            if (r.root.gameObject.activeSelf != on) r.root.gameObject.SetActive(on);
            if (!on) continue;

            bool d = _vDone[i];
            if (isNew)
            {
                r.done = d;
                r.k = d ? 1f : 0f;
            }
            else if (d && !r.done)
            {
                r.done = true;
                StartCoroutine(UiFx.Punch(r.root, 0.08f, 0.3f));
            }
            else if (!d)
            {
                r.done = false;
            }

            r.k = Mathf.MoveTowards(r.k, r.done ? 1f : 0f, dt * 4.5f);
            r.tick.transform.localScale = Vector3.one * UiFx.EaseOutBack(r.k);
            r.tick.color = accent;
            r.box.color = Color.Lerp(new Color(1f, 1f, 1f, 0.18f), new Color(accent.r, accent.g, accent.b, 0.35f), r.k);

            string txt = r.done
                ? $"<color=#6B7785><s>{_vTexts[i]}</s></color>"
                : $"<color=#E6EBF0>{_vTexts[i]}</color>";
            SetText(r.label, txt);

            if (pulse && !r.done)
            {
                r.label.alpha = pulseA;
                r.box.color = new Color(accent.r, accent.g, accent.b, 0.15f + 0.3f * pulseA);
            }
            else
            {
                r.label.alpha = 1f;
            }
        }
    }

    private IEnumerator StripFlash()
    {
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.unscaledDeltaTime;
            _questStrip.color = Color.Lerp(Color.white, accent, UiFx.EaseOutCubic(t / 0.6f));
            yield return null;
        }
        _questStrip.color = accent;
    }

    // ====================================================================
    // Построение UI
    // ====================================================================

    private void Build()
    {
        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var cgo = new GameObject("DayCanvas");
        cgo.transform.SetParent(transform, false);
        _canvas = cgo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;
        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();

        BuildHud();
        BuildToast();
        BuildCard();
        BuildReport();
        BuildShop();
        BuildEnd();

        _flashGroup = MakeOverlay("Flash", Color.white);
        _flashGroup.alpha = 0f;
        _flashGroup.blocksRaycasts = false;
        _flashGroup.interactable = false;

        _fadeGroup = MakeOverlay("Fade", Color.black);
    }

    private void BuildHud()
    {
        _hudRoot = NewRect("Hud", _canvas.transform);
        _hudRoot.anchorMin = _hudRoot.anchorMax = _hudRoot.pivot = new Vector2(0f, 1f);
        _hudRoot.anchoredPosition = new Vector2(40f, -40f);
        _hudRoot.sizeDelta = new Vector2(440f, 0f);

        _hudGroup = _hudRoot.gameObject.AddComponent<CanvasGroup>();
        _hudGroup.alpha = 0f;
        _hudGroup.blocksRaycasts = false;
        _hudGroup.interactable = false;

        var v = _hudRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 14f;
        v.childAlignment = TextAnchor.UpperLeft;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        var fit = _hudRoot.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Карточка: день, деньги, расходники
        RectTransform meta = MakePanel(_hudRoot, "Meta", out _);

        // Шапка: день слева, плашка "кассета вставлена" справа
        RectTransform head = NewRect("Head", meta);
        var hh = head.gameObject.AddComponent<HorizontalLayoutGroup>();
        hh.spacing = 10f;
        hh.childAlignment = TextAnchor.MiddleLeft;
        hh.childControlWidth = true;
        hh.childControlHeight = true;
        hh.childForceExpandWidth = false;
        hh.childForceExpandHeight = false;
        UiFx.Layout(head.gameObject, minH: 34f, prefH: 34f);

        _hudTitle = MakeText(head, "Day", 24, TextAlignmentOptions.MidlineLeft, accent);
        _hudTitle.characterSpacing = 4f;
        UiFx.Layout(_hudTitle.gameObject, flexW: 1f);

        _recPill = NewRect("RecPill", head);
        UiFx.Panel(_recPill, new Color(AmberC.r, AmberC.g, AmberC.b, 0.16f), 12);
        var ph = _recPill.gameObject.AddComponent<HorizontalLayoutGroup>();
        ph.padding = new RectOffset(12, 14, 4, 4);
        ph.spacing = 8f;
        ph.childAlignment = TextAnchor.MiddleLeft;
        ph.childControlWidth = true;
        ph.childControlHeight = true;
        ph.childForceExpandWidth = false;
        ph.childForceExpandHeight = false;
        UiFx.Layout(_recPill.gameObject, minH: 28f, prefH: 28f);
        _recDot = UiFx.Img(_recPill, "Dot", UiFx.Circle(), AmberC);
        UiFx.Layout(_recDot.gameObject, 10f, 10f, 10f, 10f);
        _recText = MakeText(_recPill, "Text", 16, TextAlignmentOptions.MidlineLeft, AmberC);
        _recText.characterSpacing = 2f;
        _recText.enableWordWrapping = false;
        _recPill.gameObject.SetActive(false);

        _hudMoney = MakeText(meta, "Money", 56, TextAlignmentOptions.TopLeft, GreenC);
        _hudMoney.fontStyle = FontStyles.Bold;
        _hudMoney.enableWordWrapping = false;

        Image divider = UiFx.Img(meta, "Divider", null, new Color(1f, 1f, 1f, 0.08f));
        UiFx.Layout(divider.gameObject, minH: 2f, prefH: 2f);

        _ammoRow = MakeStat(meta, GameState.MaxAmmo);
        _tapeRow = MakeStat(meta, GameState.MaxCassettes);
        _strikeRow = MakeStat(meta, GameState.MaxStrikes);

        // Панель заданий
        _questPanel = MakePanel(_hudRoot, "Quest", out _questStrip);
        _questGroup = _questPanel.gameObject.AddComponent<CanvasGroup>();
        _questGroup.alpha = 0f;
        _questHeader = MakeText(_questPanel, "Header", 20, TextAlignmentOptions.TopLeft, TextDim);
        _questHeader.characterSpacing = 4f;
        _questName = MakeText(_questPanel, "Name", 30, TextAlignmentOptions.TopLeft, Color.white);
        _questName.fontStyle = FontStyles.Bold;

        for (int i = 0; i < MaxQuestRows; i++)
        {
            var r = new QRow();
            r.root = NewRect("Step", _questPanel);
            var h = r.root.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12f;
            h.childAlignment = TextAnchor.UpperLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            r.box = UiFx.Img(r.root, "Box", UiFx.Rounded(5), new Color(1f, 1f, 1f, 0.18f), true);
            UiFx.Layout(r.box.gameObject, 24f, 24f, 24f, 24f);
            r.tick = UiFx.Img(r.box.transform, "Tick", UiFx.Rounded(4), accent, true);
            UiFx.Stretch(r.tick.rectTransform, 5f, 5f, 5f, 5f);

            r.label = MakeText(r.root, "Label", 24, TextAlignmentOptions.TopLeft, TextMain);
            UiFx.Layout(r.label.gameObject, flexW: 1f);

            r.root.gameObject.SetActive(false);
            _qRows.Add(r);
        }
    }

    private StatRow MakeStat(Transform parent, int pipCount)
    {
        var row = new StatRow();

        RectTransform line = NewRect("Line", parent);
        var h = line.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 8f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;
        UiFx.Layout(line.gameObject, minH: 30f, prefH: 30f);

        row.label = MakeText(line, "Label", 21, TextAlignmentOptions.MidlineLeft, TextDim);
        UiFx.Layout(row.label.gameObject, flexW: 1f);
        row.value = MakeText(line, "Value", 25, TextAlignmentOptions.MidlineRight, Color.white);
        row.value.enableWordWrapping = false;
        UiFx.Layout(row.value.gameObject, minW: 150f, prefW: 150f);

        RectTransform pips = NewRect("Pips", parent);
        var ph = pips.gameObject.AddComponent<HorizontalLayoutGroup>();
        ph.spacing = 4f;
        ph.childAlignment = TextAnchor.MiddleLeft;
        ph.childControlWidth = true;
        ph.childControlHeight = true;
        ph.childForceExpandWidth = false;
        ph.childForceExpandHeight = false;
        UiFx.Layout(pips.gameObject, minH: 12f, prefH: 12f);

        row.pips = new Image[pipCount];
        for (int i = 0; i < pipCount; i++)
        {
            Image p = UiFx.Img(pips, "Pip", UiFx.Rounded(3), PipOff, true);
            UiFx.Layout(p.gameObject, 18f, 10f, 18f, 10f);
            row.pips[i] = p;
        }
        return row;
    }

    private void BuildToast()
    {
        RectTransform rt = NewRect("Toast", _canvas.transform);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -110f);
        rt.sizeDelta = new Vector2(1000f, 160f);

        _toastGroup = rt.gameObject.AddComponent<CanvasGroup>();
        _toastGroup.alpha = 0f;
        _toastGroup.blocksRaycasts = false;
        _toastGroup.interactable = false;

        _toastText = MakeText(rt, "Text", 52, TextAlignmentOptions.Center, Color.white);
        Stretch(_toastText.rectTransform);
    }

    private void BuildCard()
    {
        _cardGroup = MakeOverlay("DayCard", Color.black);
        _cardText = MakeText(_cardGroup.transform, "Text", 110, TextAlignmentOptions.Center, Color.white);
        _cardText.fontStyle = FontStyles.Bold;
        _cardText.characterSpacing = 10f;
        Stretch(_cardText.rectTransform);

        Image line = UiFx.Img(_cardGroup.transform, "Line", null, accent);
        _cardLine = line.rectTransform;
        _cardLine.anchorMin = _cardLine.anchorMax = new Vector2(0.5f, 0.5f);
        _cardLine.pivot = new Vector2(0.5f, 0.5f);
        _cardLine.anchoredPosition = new Vector2(0f, -150f);
        _cardLine.sizeDelta = new Vector2(0f, 3f);
    }

    private void BuildReport()
    {
        _reportGroup = MakeOverlay("Report", Overlay);
        RectTransform col = MakeColumn(_reportGroup.transform, 1000f);

        _reportSub = MakeText(col, "Suspect", 40, TextAlignmentOptions.Center, TextMain);
        AddLayout(_reportSub.gameObject, 70f);

        _reportTitle = MakeText(col, "Title", 66, TextAlignmentOptions.Center, Color.white);
        _reportTitle.fontStyle = FontStyles.Bold;
        _reportTitle.characterSpacing = 6f;
        AddLayout(_reportTitle.gameObject, 100f);

        _reportRows = NewRect("Rows", col);
        var v = _reportRows.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 8f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        UiFx.Layout(_reportRows.gameObject, minH: 560f);

        _reportButton = MakeButton(col, "Continue", 80f);
        _reportButton.onClick.AddListener(OnReportContinue);
        _reportButton.gameObject.AddComponent<CanvasGroup>();
    }

    private void BuildShop()
    {
        _shopGroup = MakeOverlay("Shop", Overlay);
        RectTransform col = MakeColumn(_shopGroup.transform, 1000f);

        TMP_Text title = MakeText(col, "Title", 62, TextAlignmentOptions.Center, Color.white);
        title.text = Loc.T("BLACK MARKET", "ЧЁРНЫЙ РЫНОК");
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 8f;
        AddLayout(title.gameObject, 90f);
        _shopTitleGroup = title.gameObject.AddComponent<CanvasGroup>();

        _shopMoney = MakeText(col, "Money", 46, TextAlignmentOptions.Center, Color.white);
        AddLayout(_shopMoney.gameObject, 70f);

        _shopAmmo = MakeShopRow(col);
        _shopAmmo.buy.onClick.AddListener(() => Buy(true));

        _shopTape = MakeShopRow(col);
        _shopTape.buy.onClick.AddListener(() => Buy(false));

        _shopNotice = MakeText(col, "Notice", 30, TextAlignmentOptions.Center, TextMain);
        AddLayout(_shopNotice.gameObject, 44f);

        _shopInfo = MakeText(col, "Info", 24, TextAlignmentOptions.Center, TextDim);
        _shopInfo.text = Loc.T(
            "Insert the cassette into the recorder BEFORE starting the interrogation.",
            "Вставь кассету в диктофон ДО начала допроса.");
        AddLayout(_shopInfo.gameObject, 40f);

        _shopNextBtn = MakeButton(col, "NextDay", 90f);
        _shopNextBtn.onClick.AddListener(OnNextDay);
        _shopNextGroup = _shopNextBtn.gameObject.AddComponent<CanvasGroup>();
    }

    private void BuildEnd()
    {
        _endGroup = MakeOverlay("End", new Color(0.02f, 0.02f, 0.03f, 0.99f));
        RectTransform col = MakeColumn(_endGroup.transform, 1000f);

        _endTitle = MakeText(col, "Title", 84, TextAlignmentOptions.Center, Color.white);
        _endTitle.fontStyle = FontStyles.Bold;
        _endTitle.characterSpacing = 8f;
        AddLayout(_endTitle.gameObject, 130f);

        _endBody = MakeText(col, "Body", 36, TextAlignmentOptions.Center, new Color(0.85f, 0.88f, 0.92f));
        AddLayout(_endBody.gameObject, 260f);

        _endButton = MakeButton(col, "Exit", 80f);
        _endButton.onClick.AddListener(OnEndButton);
    }

    // ====================================================================
    // Мини-хелперы UI
    // ====================================================================

    /// <summary>
    /// Со 2-го дня перестраховка: если стартовые скрипты интро не отработали, игрок не должен застрять.
    /// Все действия безопасны при повторном вызове.
    /// </summary>
    private IEnumerator BootstrapRoutine()
    {
        yield return null;   // даём IntroText и PauseMenu отработать первыми

        if (GameState.Day > 1)
        {
            IntroText.isIntro = false;
            if (QuestManager.Instance != null) QuestManager.Instance.SkipInspection();
            if (player != null && !player.enabled)
            {
                Debug.LogWarning("[DayUI] Игрок был выключен на старте дня, включаю.");
                player.enabled = true;
            }
            if (player != null && !player.IsDialogue)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        Debug.Log($"[DayUI] День {GameState.Day}: IsEnding={Ending.IsEnding}, isIntro={IntroText.isIntro}, " +
                  $"player.enabled={(player != null && player.enabled)}, quest={(QuestManager.Instance != null)}");
    }

    /// <summary>Отчёт открыт: управление, взаимодействие, стрельба и прицел выключаются.</summary>
    private void LockPlayer()
    {
        if (player != null) player.enabled = false;

        PlayerInteract pi = FindObjectOfType<PlayerInteract>();
        if (pi != null)
        {
            pi.StopAllCoroutines();
            if (pi._interactionText != null)
            {
                pi._interactionText.text = "";
                pi._interactionText.gameObject.SetActive(false);
            }
            if (pi._guidText != null) pi._guidText.gameObject.SetActive(false);
            pi.enabled = false;
        }

        PlayerShoot shoot = FindObjectOfType<PlayerShoot>();
        if (shoot != null) shoot.enabled = false;

        InventoryManager inv = FindObjectOfType<InventoryManager>();
        if (inv != null) inv.enabled = false;

        Crosshair ch = FindObjectOfType<Crosshair>();
        if (ch != null && ch.crosshairPanel != null) ch.crosshairPanel.gameObject.SetActive(false);
    }

    /// <summary>Строка-пояснение в отчёте (серым, без суммы).</summary>
    private void AddNote(string text)
    {
        RectTransform row = NewRect("Note", _reportRows);
        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(22, 22, 2, 2);
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = false;
        UiFx.Layout(row.gameObject, minH: 38f, prefH: 38f);

        TMP_Text l = MakeText(row, "Text", 24, TextAlignmentOptions.MidlineLeft, TextDim);
        l.text = $"<color={Amber}>!</color>  {text}";

        var cg = row.gameObject.AddComponent<CanvasGroup>();
        StartCoroutine(UiFx.Appear(cg, 0f, 0.28f));
    }

    private RectTransform NewRect(string name, Transform parent)
    {
        return UiFx.Rect(name, parent);
    }

    private void Stretch(RectTransform rt)
    {
        UiFx.Stretch(rt);
    }

    private void SetGroup(CanvasGroup g, bool on)
    {
        g.alpha = on ? 1f : 0f;
        g.interactable = on;
        g.blocksRaycasts = on;
    }

    private CanvasGroup MakeOverlay(string name, Color bg)
    {
        RectTransform rt = NewRect(name, _canvas.transform);
        Stretch(rt);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = bg;
        var cg = rt.gameObject.AddComponent<CanvasGroup>();
        SetGroup(cg, false);
        return cg;
    }

    /// <summary>Тёмная скруглённая панель с цветной полоской слева; внутри раскладывается по вертикали.</summary>
    private RectTransform MakePanel(Transform parent, string name, out Image strip)
    {
        RectTransform rt = NewRect(name, parent);
        UiFx.Panel(rt, PanelBg, 18);

        var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(26, 22, 16, 18);
        v.spacing = 6f;
        v.childAlignment = TextAnchor.UpperLeft;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        strip = UiFx.Img(rt, "Accent", UiFx.Rounded(3), accent, true);
        var le = strip.gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;
        RectTransform sr = strip.rectTransform;
        sr.anchorMin = new Vector2(0f, 0f);
        sr.anchorMax = new Vector2(0f, 1f);
        sr.pivot = new Vector2(0f, 0.5f);
        sr.sizeDelta = new Vector2(5f, -26f);
        sr.anchoredPosition = new Vector2(10f, 0f);
        return rt;
    }

    private TMP_Text MakeText(Transform parent, string name, int size, TextAlignmentOptions align, Color color)
    {
        return UiFx.Text(parent, name, size, align, color, font);
    }

    private RectTransform MakeColumn(Transform parent, float width)
    {
        RectTransform rt = NewRect("Column", parent);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, 0f);

        var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 22f;
        v.childAlignment = TextAnchor.MiddleCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        var fit = rt.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rt;
    }

    private void AddLayout(GameObject go, float minHeight)
    {
        UiFx.Layout(go, minH: minHeight, prefH: minHeight);
    }

    private Button MakeButton(Transform parent, string name, float height)
    {
        RectTransform rt = NewRect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = UiFx.Rounded(16);
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock c = btn.colors;
        c.normalColor = new Color(0.16f, 0.20f, 0.26f, 1f);
        c.highlightedColor = new Color(0.24f, 0.45f, 0.58f, 1f);
        c.pressedColor = new Color(0.15f, 0.32f, 0.42f, 1f);
        c.selectedColor = c.normalColor;
        c.disabledColor = new Color(0.10f, 0.11f, 0.13f, 0.7f);
        btn.colors = c;

        AddLayout(rt.gameObject, height);

        TMP_Text label = MakeText(rt, "Label", 30, TextAlignmentOptions.Center, Color.white);
        Stretch(label.rectTransform);

        rt.gameObject.AddComponent<UiButtonFx>();
        return btn;
    }

    /// <summary>Строка магазина: название и описание | сколько есть | кнопка покупки.</summary>
    private ShopRow MakeShopRow(Transform parent)
    {
        var s = new ShopRow();

        RectTransform row = NewRect("Row", parent);
        UiFx.Panel(row, RowBg, 16);
        s.group = row.gameObject.AddComponent<CanvasGroup>();

        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(28, 16, 10, 10);
        h.spacing = 24f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;
        UiFx.Layout(row.gameObject, minH: 110f, prefH: 110f);

        s.name = MakeText(row, "Name", 34, TextAlignmentOptions.MidlineLeft, TextMain);
        UiFx.Layout(s.name.gameObject, flexW: 1f);

        s.owned = MakeText(row, "Owned", 36, TextAlignmentOptions.Midline, Color.white);
        UiFx.Layout(s.owned.gameObject, minW: 150f, prefW: 150f);

        s.buy = MakeButton(row, "Buy", 0f);
        UiFx.Layout(s.buy.gameObject, minW: 260f, prefW: 260f);
        return s;
    }

    private void SetText(TMP_Text t, string s)
    {
        if (t.text != s) t.text = s;
    }

    private void SetLabel(Button b, string text)
    {
        TMP_Text t = b.GetComponentInChildren<TMP_Text>();
        if (t != null && t.text != text) t.text = text;
    }

    private IEnumerator Fade(CanvasGroup g, float from, float to, float time)
    {
        return UiFx.Fade(g, from, to, time);
    }

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}