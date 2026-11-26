using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Вся UI системы дней. Строится кодом в Awake, руками собирать ничего не нужно.
/// Повесь на пустой объект "DayUI" в игровой сцене и назначь шрифт с кириллицей.
/// </summary>
public class DayUI : MonoBehaviour
{
    public static DayUI Instance { get; private set; }

    [Header("Look")]
    [Tooltip("Тот же TMP-шрифт, что и в остальной игре (должен содержать кириллицу)")]
    [SerializeField] private TMP_FontAsset font;

    [Header("Scenes")]
    [SerializeField] private int menuSceneIndex = 0;

    [Header("Audio (необязательно)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip tickSound;
    [SerializeField] private AudioClip goodSound;
    [SerializeField] private AudioClip badSound;
    [SerializeField] private AudioClip buySound;
    [SerializeField] private AudioClip denySound;

    private Canvas _canvas;

    private CanvasGroup _hudGroup, _cardGroup, _toastGroup, _reportGroup, _shopGroup, _endGroup, _fadeGroup;
    private TMP_Text _hudText, _cardText, _toastText;
    private TMP_Text _reportTitle, _reportLines;
    private Button _reportButton;
    private TMP_Text _shopMoney, _shopInfo;
    private Button _shopAmmoBtn, _shopTapeBtn, _shopNextBtn;
    private TMP_Text _endTitle, _endBody;
    private Button _endButton;

    private bool _locked;       // true после конца дня: HUD скрыт, UI дня занят
    private string _lastHud;

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
        GameState.BeginDay();
        StartCoroutine(DayStartRoutine());
    }

    private void Update()
    {
        bool show = !_locked && !IntroText.isIntro && !Ending.IsEnding && !CameraTranzition.InPC;
        _hudGroup.alpha = show ? 1f : 0f;

        string hud = BuildHudString();
        if (hud != _lastHud)
        {
            _lastHud = hud;
            _hudText.text = hud;
        }
    }

    // ====================================================================
    // Публичное API
    // ====================================================================

    /// <summary>Вызывается из Ending после текста "Подозреваемый был ...".</summary>
    public void ShowDayEnd()
    {
        if (_locked) return;
        _locked = true;
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
            _cardText.text = $"{title}\n<size=45%>{sub}</size>";
            SetGroup(_cardGroup, true);
            yield return new WaitForSecondsRealtime(2.2f);
            yield return Fade(_cardGroup, 1f, 0f, 0.8f);
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
        yield return Fade(_toastGroup, 0f, 1f, 0.4f);
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

        _reportTitle.text = "";
        _reportLines.text = "";
        _reportButton.gameObject.SetActive(false);

        SetGroup(_reportGroup, true);
        _reportGroup.alpha = 0f;
        yield return Fade(_reportGroup, 0f, 1f, 0.6f);

        if (!GameState.VerdictMade)
            _reportTitle.text = $"<color=#FF6B6B>{Loc.T("NO VERDICT", "ВЕРДИКТ НЕ ВЫНЕСЕН")}</color>";
        else if (r.Correct)
            _reportTitle.text = $"<color=#7CFC9A>{Loc.T("VERDICT CONFIRMED", "ВЕРДИКТ ВЕРЕН")}</color>";
        else
            _reportTitle.text = $"<color=#FF6B6B>{Loc.T("VERDICT WRONG", "ВЕРДИКТ ОШИБОЧЕН")}</color>";

        Play(r.Correct ? goodSound : badSound);
        yield return new WaitForSecondsRealtime(0.9f);

        var sb = new StringBuilder();
        foreach (ReportLine line in r.Lines)
        {
            sb.AppendLine(FormatLine(line));
            _reportLines.text = sb.ToString();
            Play(tickSound);
            yield return new WaitForSecondsRealtime(0.5f);
        }

        sb.AppendLine();
        sb.AppendLine($"{Loc.T("Balance", "Баланс")}<pos=56%>${r.MoneyBefore}  >  <color=#FFFFFF>${r.MoneyAfter}</color>");
        _reportLines.text = sb.ToString();
        yield return new WaitForSecondsRealtime(0.5f);

        string strikeCol = GameState.Strikes >= GameState.MaxStrikes - 1 ? "#FF6B6B" : "#FFFFFF";
        sb.AppendLine($"{Loc.T("Mistakes", "Ошибки")}<pos=72%><color={strikeCol}>{GameState.Strikes}/{GameState.MaxStrikes}</color>");
        _reportLines.text = sb.ToString();
        yield return new WaitForSecondsRealtime(0.5f);

        if (r.GameOver)
        {
            sb.AppendLine();
            sb.AppendLine($"<color=#FF6B6B>{Loc.T("The department is closing your case.", "Отдел закрывает ваше дело.")}</color>");
            _reportLines.text = sb.ToString();
            yield return new WaitForSecondsRealtime(0.5f);
        }

        SetLabel(_reportButton, (r.GameOver || r.RunComplete)
            ? Loc.T("Continue", "Далее")
            : Loc.T("To the shop", "В магазин"));
        _reportButton.gameObject.SetActive(true);
    }

    private string FormatLine(ReportLine l)
    {
        string col = l.Amount >= 0 ? "#7CFC9A" : "#FF6B6B";
        string sign = l.Amount >= 0 ? "+" : "-";
        return $"{l.Label}<pos=72%><color={col}>{sign}${Mathf.Abs(l.Amount)}</color>";
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

    private void OpenShop()
    {
        SetGroup(_shopGroup, true);
        RefreshShop();
    }

    private void RefreshShop()
    {
        _shopMoney.text = $"{Loc.T("Balance", "Баланс")}: <color=#7CFC9A>${GameState.Money}</color>";

        SetLabel(_shopAmmoBtn,
            $"{Loc.T("Buy a bullet", "Купить патрон")}   -${GameState.AmmoPrice}    [{GameState.Ammo}/{GameState.MaxAmmo}]");
        _shopAmmoBtn.interactable = GameState.CanBuyAmmo;

        SetLabel(_shopTapeBtn,
            $"{Loc.T("Buy a cassette", "Купить кассету")}   -${GameState.CassettePrice}    [{GameState.Cassettes}/{GameState.MaxCassettes}]");
        _shopTapeBtn.interactable = GameState.CanBuyCassette;

        SetLabel(_shopNextBtn,
            $"{Loc.T("Next day", "Следующий день")} ({GameState.Day + 1}/{GameState.TotalDays})");
    }

    private void Buy(bool ammo)
    {
        bool ok = ammo ? GameState.TryBuyAmmo() : GameState.TryBuyCassette();
        Play(ok ? buySound : denySound);
        RefreshShop();
    }

    private void OnNextDay()
    {
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
        SetGroup(_endGroup, true);

        _endTitle.text = victory
            ? $"<color=#7CFC9A>{Loc.T("CASE CLOSED", "ДЕЛО ЗАКРЫТО")}</color>"
            : $"<color=#FF6B6B>{Loc.T("YOU ARE FIRED", "ВЫ УВОЛЕНЫ")}</color>";

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

        SetLabel(_endButton, Loc.T("Main menu", "В главное меню"));
    }

    private void OnEndButton()
    {
        StartCoroutine(ToMenuRoutine());
    }

    private IEnumerator ToMenuRoutine()
    {
        SetGroup(_fadeGroup, true);
        _fadeGroup.alpha = 0f;
        yield return Fade(_fadeGroup, 0f, 1f, 0.8f);

        GameState.ResetRun();
        SceneManager.LoadScene(menuSceneIndex);
    }

    // ====================================================================
    // HUD
    // ====================================================================

    private string BuildHudString()
    {
        string ammoCol = GameState.Ammo <= 0 ? "#FF6B6B" : "#FFFFFF";

        var strikes = new StringBuilder();
        for (int i = 0; i < GameState.MaxStrikes; i++)
            strikes.Append(i < GameState.Strikes ? "<color=#FF6B6B>X</color>" : "<color=#667788>-</color>");

        string s =
            $"{Loc.T("DAY", "ДЕНЬ")} {GameState.Day}/{GameState.TotalDays}\n" +
            $"<color=#7CFC9A>$ {GameState.Money}</color>\n" +
            $"{Loc.T("AMMO", "ПАТРОНЫ")} <color={ammoCol}>{GameState.Ammo}</color>/{GameState.MaxAmmo}\n" +
            $"{Loc.T("TAPES", "КАССЕТЫ")} {GameState.Cassettes}\n" +
            $"{Loc.T("MISTAKES", "ОШИБКИ")} [{strikes}]";

        if (GameState.TapeLoaded)
            s += $"\n<color=#FFC857>{Loc.T("TAPE LOADED", "КАССЕТА ВСТАВЛЕНА")}</color>";

        return s;
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
        _fadeGroup = MakeOverlay("Fade", Color.black);
    }

    private void BuildHud()
    {
        RectTransform rt = NewRect("Hud", _canvas.transform);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-40f, -40f);
        rt.sizeDelta = new Vector2(460f, 240f);

        _hudGroup = rt.gameObject.AddComponent<CanvasGroup>();
        _hudGroup.blocksRaycasts = false;
        _hudGroup.interactable = false;

        _hudText = MakeText(rt, "Text", 26, TextAlignmentOptions.TopRight, Color.white);
        Stretch(_hudText.rectTransform);
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
        _cardText = MakeText(_cardGroup.transform, "Text", 96, TextAlignmentOptions.Center, Color.white);
        Stretch(_cardText.rectTransform);
    }

    private void BuildReport()
    {
        _reportGroup = MakeOverlay("Report", new Color(0.02f, 0.03f, 0.05f, 0.97f));
        RectTransform col = MakeColumn(_reportGroup.transform, 1000f);

        _reportTitle = MakeText(col, "Title", 64, TextAlignmentOptions.Center, Color.white);
        AddLayout(_reportTitle.gameObject, 100f);

        _reportLines = MakeText(col, "Lines", 34, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.88f, 0.92f));
        AddLayout(_reportLines.gameObject, 330f);

        _reportButton = MakeButton(col, "Continue", 80f);
        _reportButton.onClick.AddListener(OnReportContinue);
    }

    private void BuildShop()
    {
        _shopGroup = MakeOverlay("Shop", new Color(0.02f, 0.03f, 0.05f, 0.97f));
        RectTransform col = MakeColumn(_shopGroup.transform, 1000f);

        TMP_Text title = MakeText(col, "Title", 64, TextAlignmentOptions.Center, Color.white);
        title.text = Loc.T("BLACK MARKET", "ЧЁРНЫЙ РЫНОК");
        AddLayout(title.gameObject, 100f);

        _shopMoney = MakeText(col, "Money", 40, TextAlignmentOptions.Center, Color.white);
        AddLayout(_shopMoney.gameObject, 60f);

        _shopAmmoBtn = MakeButton(col, "BuyAmmo", 80f);
        _shopAmmoBtn.onClick.AddListener(() => Buy(true));

        _shopTapeBtn = MakeButton(col, "BuyTape", 80f);
        _shopTapeBtn.onClick.AddListener(() => Buy(false));

        _shopInfo = MakeText(col, "Info", 26, TextAlignmentOptions.Center, new Color(0.6f, 0.65f, 0.72f));
        _shopInfo.text = Loc.T(
            "A cassette lets you analyse the suspect's voice before the verdict.\nInsert it into the recorder BEFORE starting the interrogation.",
            "Кассета позволяет проанализировать голос подозреваемого перед вердиктом.\nВставь её в диктофон ДО начала допроса.");
        AddLayout(_shopInfo.gameObject, 110f);

        _shopNextBtn = MakeButton(col, "NextDay", 90f);
        _shopNextBtn.onClick.AddListener(OnNextDay);
    }

    private void BuildEnd()
    {
        _endGroup = MakeOverlay("End", new Color(0.02f, 0.02f, 0.03f, 0.99f));
        RectTransform col = MakeColumn(_endGroup.transform, 1000f);

        _endTitle = MakeText(col, "Title", 80, TextAlignmentOptions.Center, Color.white);
        AddLayout(_endTitle.gameObject, 130f);

        _endBody = MakeText(col, "Body", 36, TextAlignmentOptions.Center, new Color(0.85f, 0.88f, 0.92f));
        AddLayout(_endBody.gameObject, 260f);

        _endButton = MakeButton(col, "Menu", 80f);
        _endButton.onClick.AddListener(OnEndButton);
    }

    // ====================================================================
    // Мини-хелперы UI
    // ====================================================================

    private RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        return rt;
    }

    private void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
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

    private TMP_Text MakeText(Transform parent, string name, int size, TextAlignmentOptions align, Color color)
    {
        RectTransform rt = NewRect(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.richText = true;
        t.raycastTarget = false;
        return t;
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
        var le = go.AddComponent<LayoutElement>();
        le.minHeight = minHeight;
        le.preferredHeight = minHeight;
    }

    private Button MakeButton(Transform parent, string name, float height)
    {
        RectTransform rt = NewRect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
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
        return btn;
    }

    private void SetLabel(Button b, string text)
    {
        TMP_Text t = b.GetComponentInChildren<TMP_Text>();
        if (t != null) t.text = text;
    }

    private IEnumerator Fade(CanvasGroup g, float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
        g.alpha = to;
    }

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
