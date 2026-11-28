using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Новый интерфейс допроса: биометрия (глаз, пульс с ЭКГ, зрачок, задержка), диалоговое окно,
/// карточки вопросов (клавиши 1/2/3) и панель вердикта.
/// Строится кодом. Вешать вручную НЕ нужно: DialogueSystem сам добавит компонент в Awake
/// и вызовет Bind(), который подменит ссылки на старые панели новыми.
/// Старый UI в сцене при этом просто выключается (удалять его не обязательно).
/// </summary>
public class InterrogationUI : MonoBehaviour
{
    public const int Detective = 0;
    public const int Suspect = 1;

    [Header("Look")]
    [Tooltip("Если пусто, берётся шрифт DayUI. Нужна кириллица.")]
    [SerializeField] private TMP_FontAsset font;
    [Tooltip("Рисовать свой глаз. Выключи, чтобы оставить глаз из сцены.")]
    [SerializeField] private bool buildOwnEye = true;

    private static readonly Color Cyan = new Color(0.25f, 0.85f, 1f);
    private static readonly Color Orange = new Color(1f, 0.55f, 0.35f);
    private static readonly Color Green = new Color(0.49f, 0.99f, 0.60f);
    private static readonly Color Red = new Color(1f, 0.42f, 0.42f);
    private static readonly Color Amber = new Color(1f, 0.78f, 0.34f);
    private static readonly Color PanelBg = new Color(0.03f, 0.05f, 0.08f, 0.88f);
    private static readonly Color TextMain = new Color(0.90f, 0.93f, 0.96f);
    private static readonly Color TextDim = new Color(0.55f, 0.60f, 0.67f);

    private DialogueSystem _ds;
    private bool _bound;
    private GameObject _canvasGo;
    private RectTransform _canvasRt;

    private RectTransform _bio, _dialogue, _questions, _final;

    // диалог
    private TMP_Text _dialogueText, _speakerText, _waitHint;
    private Image _speakerPill, _stripe;
    private bool _waiting;
    private int _speaker = -2;

    // вопросы и вердикт
    private Button[] _qButtons;
    private Button _replicantBtn, _humanBtn, _tapeBtn;
    private TMP_Text _summary;

    // биометрия
    private EyeBehaviour _eye;
    private Image _irisImg, _liveDot, _heart, _vignette;
    private RectTransform _ring1, _ring2;
    private EcgGraphic _ecg;
    private TMP_Text _pulseValue, _pupilValue, _delayValue, _alert;
    private Gauge _pupilGauge, _delayGauge;

    private float _pulse = 75f, _pupil = 0.5f, _delay;
    private float _shownPulse = 75f, _shownPupil = 0.5f, _shownDelay;
    private bool _statsInit;
    private int _lastPulseInt = -1;
    private string _lastPupilStr, _lastDelayStr;

    private class Gauge
    {
        public RectTransform fill;
        public Image fillImg;
        public Image marker;
    }

    // ===================================================================
    // Подключение к DialogueSystem
    // ===================================================================

    public void Bind(DialogueSystem ds)
    {
        if (_bound) return;
        _bound = true;
        _ds = ds;

        if (font == null && DayUI.Instance != null) font = DayUI.Instance.Font;

        HideOldUi(ds);
        CreateCanvas();
        BuildVignette();
        BuildBio();
        BuildDialogue();
        BuildQuestions();
        BuildFinal();

        ds.dialogueText = _dialogueText;
        ds.QuestionPanel = _questions.gameObject;
        ds.AnswerPanel = _dialogue.gameObject;
        ds.statsPanel = _bio.gameObject;
        ds.FinalChoicePanel = _final.gameObject;
        ds.questionButtons = _qButtons;
        ds.replicantButton = _replicantBtn;
        ds.humanButton = _humanBtn;
        ds.tapeButton = _tapeBtn;
        ds.finalStatsText = _summary;
        if (_eye != null) ds.eyeBehaviour = _eye;

        _dialogue.gameObject.SetActive(false);
        _final.gameObject.SetActive(false);
        _canvasGo.SetActive(isActiveAndEnabled);
    }

    private void OnEnable()
    {
        if (_canvasGo != null) _canvasGo.SetActive(true);
    }

    private void OnDisable()
    {
        if (_canvasGo != null) _canvasGo.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_canvasGo != null) Destroy(_canvasGo);
    }

    private void Off(GameObject g)
    {
        // Не выключаем объект, на котором сидим сами, и его родителей.
        if (g == null || g == gameObject || transform.IsChildOf(g.transform)) return;
        g.SetActive(false);
    }

    private void Off(Component c)
    {
        if (c != null) Off(c.gameObject);
    }

    private void HideOldUi(DialogueSystem d)
    {
        Off(d.QuestionPanel);
        Off(d.AnswerPanel);
        Off(d.statsPanel);
        Off(d.FinalChoicePanel);
        Off(d.dialogueText);
        Off(d.pulseText);
        Off(d.pupilText);
        Off(d.delayText);
        Off(d.finalStatsText);
        Off(d.replicantButton);
        Off(d.humanButton);
        Off(d.tapeButton);
        if (d.questionButtons != null)
            foreach (Button b in d.questionButtons) Off(b);

        if (buildOwnEye && d.eyeBehaviour != null) d.eyeBehaviour.HideVisuals();
    }

    // ===================================================================
    // Публичное API для DialogueSystem
    // ===================================================================

    public void SetStats(float pulse, float pupil, float delay)
    {
        _pulse = pulse;
        _pupil = pupil;
        _delay = delay;
        if (!_statsInit)
        {
            _statsInit = true;
            _shownPulse = pulse;
            _shownPupil = pupil;
            _shownDelay = delay;
        }
    }

    /// <summary>0 = детектив, 1 = подозреваемый, любое другое = системная строка.</summary>
    public void SetSpeaker(int who)
    {
        if (!_bound || who == _speaker) return;
        _speaker = who;

        Color c;
        string label;
        if (who == Detective) { c = Cyan; label = Loc.T("DETECTIVE", "ДЕТЕКТИВ"); }
        else if (who == Suspect) { c = Orange; label = Loc.T("SUSPECT", "ПОДОЗРЕВАЕМЫЙ"); }
        else { c = TextDim; label = Loc.T("INTERROGATION", "ДОПРОС"); }

        _speakerText.text = label;
        _speakerText.color = c;
        _speakerPill.color = new Color(c.r, c.g, c.b, 0.18f);
        _stripe.color = c;

        if (isActiveAndEnabled && _dialogue.gameObject.activeInHierarchy)
            StartCoroutine(UiFx.Punch(_speakerPill.transform, 0.10f, 0.25f));
    }

    public void SetWaiting(bool waiting)
    {
        _waiting = waiting;
    }

    public void SetSummary(float avgPulse, float maxPulse, float avgPupil, float maxPupil, float avgDelay, float maxDelay)
    {
        string pu = Loc.T("bpm", "уд/мин");
        string sec = Loc.T("s", "с");

        string pulseAvg = Hot(avgPulse, 95f, 120f, avgPulse.ToString("F0") + " " + pu);
        string pulseMax = Hot(maxPulse, 95f, 120f, maxPulse.ToString("F0"));
        string pupilAvg = Hot(avgPupil, 0.55f, 0.7f, avgPupil.ToString("F2"));
        string pupilMax = Hot(maxPupil, 0.55f, 0.7f, maxPupil.ToString("F2"));
        string delayAvg = Hot(avgDelay, 2f, 3f, avgDelay.ToString("F2") + " " + sec);
        string delayMax = Hot(maxDelay, 2f, 3f, maxDelay.ToString("F2"));

        var sb = new StringBuilder();
        sb.AppendLine("<color=#7F8A96>" + Loc.T("INTERROGATION DATA", "ДАННЫЕ ДОПРОСА")
            + "<pos=52%>" + Loc.T("AVERAGE", "СРЕДНЕЕ") + "<pos=78%>" + Loc.T("MAX", "МАКС") + "</color>");
        sb.AppendLine(Loc.T("Pulse", "Пульс") + "<pos=52%>" + pulseAvg + "<pos=78%>" + pulseMax);
        sb.AppendLine(Loc.T("Pupil", "Зрачок") + "<pos=52%>" + pupilAvg + "<pos=78%>" + pupilMax);
        sb.Append(Loc.T("Delay", "Задержка") + "<pos=52%>" + delayAvg + "<pos=78%>" + delayMax);
        _summary.text = sb.ToString();
    }

    private string Hot(float v, float warn, float alarm, string text)
    {
        string col = v >= alarm ? "#FF6B6B" : (v >= warn ? "#FFC857" : "#7CFC9A");
        return $"<color={col}>{text}</color>";
    }

    // ===================================================================
    // Update: сглаживание, цвета, тревоги, горячие клавиши
    // ===================================================================

    private void Update()
    {
        if (!_bound) return;
        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;

        // горячие клавиши 1/2/3
        if (_questions.gameObject.activeInHierarchy && _qButtons != null)
        {
            for (int i = 0; i < _qButtons.Length && i < 3; i++)
            {
                if ((Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) && _qButtons[i].interactable)
                    _qButtons[i].onClick.Invoke();
            }
        }

        // подсказка "ЛКМ"
        if (_waitHint != null)
            _waitHint.alpha = _waiting ? 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(t * 5f)) : 0f;

        if (!_bio.gameObject.activeInHierarchy)
        {
            if (_vignette != null) _vignette.color = new Color(1f, 0.1f, 0.1f, 0f);
            return;
        }

        float k = 1f - Mathf.Exp(-7f * dt);
        _shownPulse = Mathf.Lerp(_shownPulse, _pulse, k);
        _shownPupil = Mathf.Lerp(_shownPupil, _pupil, k);
        _shownDelay = Mathf.Lerp(_shownDelay, _delay, k);

        bool aPulse = _pulse > 120f;
        bool aPupil = _pupil > 0.7f;
        bool aDelay = _delay > 3f;
        bool anyAlarm = aPulse || aPupil || aDelay;

        float pulseStress = Mathf.InverseLerp(90f, 130f, _shownPulse);
        float pupilStress = Mathf.InverseLerp(0.5f, 0.8f, _shownPupil);
        float delayStress = Mathf.InverseLerp(1.5f, 3.5f, _shownDelay);
        float stress = Mathf.Max(pulseStress, Mathf.Max(pupilStress, delayStress));

        // ----- пульс -----
        int pulseInt = Mathf.RoundToInt(_shownPulse);
        if (pulseInt != _lastPulseInt)
        {
            _lastPulseInt = pulseInt;
            _pulseValue.text = $"{pulseInt}<size=42%> {Loc.T("BPM", "УД/МИН")}</size>";
        }
        _pulseValue.color = Color.Lerp(Color.white, Red, pulseStress);
        float shake = aPulse ? 1f + 0.06f * Mathf.Sin(t * 22f) : 1f;
        _pulseValue.transform.localScale = Vector3.one * shake;

        _ecg.bpm = _shownPulse;
        _ecg.color = Color.Lerp(Cyan, Red, pulseStress);

        float beat = Mathf.Exp(-Mathf.Pow((_ecg.Phase - 0.27f) / 0.07f, 2f));
        _heart.transform.localScale = Vector3.one * (1f + 0.45f * beat);
        _heart.color = Color.Lerp(new Color(1f, 0.35f, 0.4f), Red, pulseStress);

        // ----- зрачок -----
        string pupilStr = $"{_shownPupil:F2}";
        if (pupilStr != _lastPupilStr)
        {
            _lastPupilStr = pupilStr;
            _pupilValue.text = pupilStr;
        }
        _pupilValue.color = Color.Lerp(Color.white, Red, pupilStress);
        SetGauge(_pupilGauge, _shownPupil, 0.7f, aPupil, t);

        // ----- задержка -----
        string delayStr = $"{_shownDelay:F2} {Loc.T("s", "с")}";
        if (delayStr != _lastDelayStr)
        {
            _lastDelayStr = delayStr;
            _delayValue.text = delayStr;
        }
        _delayValue.color = Color.Lerp(Color.white, Red, delayStress);
        SetGauge(_delayGauge, _shownDelay / 5f, 0.6f, aDelay, t);

        // ----- глаз и тревоги -----
        if (_irisImg != null)
            _irisImg.color = Color.Lerp(new Color(0.20f, 0.70f, 0.90f), new Color(0.95f, 0.25f, 0.25f), stress);
        if (_ring1 != null) _ring1.localRotation = Quaternion.Euler(0f, 0f, -t * (14f + 40f * stress));
        if (_ring2 != null) _ring2.localRotation = Quaternion.Euler(0f, 0f, t * 9f);

        _liveDot.color = new Color(Red.r, Red.g, Red.b, 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(t * 4f)));

        if (anyAlarm)
        {
            var sb = new StringBuilder();
            if (aPulse) sb.Append(Loc.T("PULSE CRITICAL", "ПУЛЬС КРИТИЧЕН"));
            if (aPupil) { if (sb.Length > 0) sb.Append("  /  "); sb.Append(Loc.T("PUPIL DILATED", "ЗРАЧОК РАСШИРЕН")); }
            if (aDelay) { if (sb.Length > 0) sb.Append("  /  "); sb.Append(Loc.T("RESPONSE DELAY", "ЗАДЕРЖКА ОТВЕТА")); }
            _alert.text = sb.ToString();
            _alert.color = new Color(Red.r, Red.g, Red.b, 0.6f + 0.4f * Mathf.Sin(t * 8f));
        }
        else
        {
            _alert.text = Loc.T("STABLE", "СТАБИЛЬНО");
            _alert.color = new Color(Green.r, Green.g, Green.b, 0.7f);
        }

        float vTarget = anyAlarm ? 0.10f + 0.06f * Mathf.Sin(t * 5f) : 0f;
        Color vc = _vignette.color;
        vc.a = Mathf.Lerp(vc.a, vTarget, 1f - Mathf.Exp(-4f * dt));
        _vignette.color = vc;
    }

    private void SetGauge(Gauge g, float value01, float threshold01, bool alarm, float t)
    {
        value01 = Mathf.Clamp01(value01);
        g.fill.anchorMax = new Vector2(Mathf.Max(0.03f, value01), 1f);

        Color c;
        if (value01 >= threshold01) c = Red;
        else c = Color.Lerp(Cyan, Amber, Mathf.InverseLerp(0f, threshold01, value01));
        g.fillImg.color = c;

        float m = alarm ? 0.5f + 0.5f * Mathf.Sin(t * 10f) : 0.35f;
        g.marker.color = Color.Lerp(new Color(1f, 1f, 1f, 0.35f), Red, alarm ? m : 0f);
    }

    // ===================================================================
    // Построение
    // ===================================================================

    private void CreateCanvas()
    {
        _canvasGo = new GameObject("InterrogationCanvas");
        var canvas = _canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = _canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _canvasGo.AddComponent<GraphicRaycaster>();
        _canvasRt = (RectTransform)_canvasGo.transform;
    }

    private void BuildVignette()
    {
        _vignette = UiFx.Img(_canvasRt, "AlarmVignette", UiFx.Vignette(), new Color(1f, 0.1f, 0.1f, 0f));
        UiFx.Stretch(_vignette.rectTransform);
    }

    private RectTransform Row(Transform parent, float height)
    {
        RectTransform rt = UiFx.Rect("Row", parent);
        var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 10f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = true;
        UiFx.Layout(rt.gameObject, minH: height, prefH: height);
        return rt;
    }

    private void BuildBio()
    {
        _bio = UiFx.Rect("Biometrics", _canvasRt);
        UiFx.Anchor(_bio, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(480f, 0f));
        UiFx.Panel(_bio, PanelBg, 20);

        var v = _bio.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(28, 28, 22, 24);
        v.spacing = 12f;
        v.childAlignment = TextAnchor.UpperLeft;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        var fit = _bio.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // заголовок
        RectTransform head = Row(_bio, 30f);
        _liveDot = UiFx.Img(head, "Dot", UiFx.Circle(), Red);
        UiFx.Layout(_liveDot.gameObject, 14f, 14f, 14f, 14f);
        TMP_Text title = UiFx.Text(head, "Title", 22, TextAlignmentOptions.MidlineLeft, Cyan, font);
        title.text = "PUPIL-READER";
        title.characterSpacing = 4f;
        UiFx.Layout(title.gameObject, flexW: 1f);
        TMP_Text live = UiFx.Text(head, "Live", 20, TextAlignmentOptions.MidlineRight, TextDim, font);
        live.text = Loc.T("LIVE", "ЭФИР");
        UiFx.Layout(live.gameObject, minW: 70f, prefW: 70f);

        Image line = UiFx.Img(_bio, "Line", null, new Color(1f, 1f, 1f, 0.08f));
        UiFx.Layout(line.gameObject, minH: 2f, prefH: 2f);

        // глаз
        RectTransform eyeHolder = UiFx.Rect("Eye", _bio);
        UiFx.Layout(eyeHolder.gameObject, minH: 214f, prefH: 214f);
        if (buildOwnEye) BuildEye(eyeHolder);

        // пульс
        RectTransform pRow = Row(_bio, 58f);
        _heart = UiFx.Img(pRow, "Heart", UiFx.Circle(), Red);
        UiFx.Layout(_heart.gameObject, 22f, 22f, 22f, 22f);
        TMP_Text pLabel = UiFx.Text(pRow, "Label", 22, TextAlignmentOptions.MidlineLeft, TextDim, font);
        pLabel.text = Loc.T("PULSE", "ПУЛЬС");
        UiFx.Layout(pLabel.gameObject, flexW: 1f);
        _pulseValue = UiFx.Text(pRow, "Value", 54, TextAlignmentOptions.MidlineRight, Color.white, font);
        _pulseValue.fontStyle = FontStyles.Bold;
        UiFx.Layout(_pulseValue.gameObject, minW: 250f, prefW: 250f);

        RectTransform ecgRt = UiFx.Rect("Ecg", _bio);
        _ecg = ecgRt.gameObject.AddComponent<EcgGraphic>();
        _ecg.color = Cyan;
        UiFx.Layout(ecgRt.gameObject, minH: 96f, prefH: 96f);

        // зрачок
        RectTransform uRow = Row(_bio, 40f);
        TMP_Text uLabel = UiFx.Text(uRow, "Label", 22, TextAlignmentOptions.MidlineLeft, TextDim, font);
        uLabel.text = Loc.T("PUPIL", "ЗРАЧОК");
        UiFx.Layout(uLabel.gameObject, flexW: 1f);
        _pupilValue = UiFx.Text(uRow, "Value", 34, TextAlignmentOptions.MidlineRight, Color.white, font);
        _pupilValue.fontStyle = FontStyles.Bold;
        UiFx.Layout(_pupilValue.gameObject, minW: 160f, prefW: 160f);
        _pupilGauge = MakeGauge(_bio, 0.7f);

        // задержка
        RectTransform dRow = Row(_bio, 40f);
        TMP_Text dLabel = UiFx.Text(dRow, "Label", 22, TextAlignmentOptions.MidlineLeft, TextDim, font);
        dLabel.text = Loc.T("DELAY", "ЗАДЕРЖКА");
        UiFx.Layout(dLabel.gameObject, flexW: 1f);
        _delayValue = UiFx.Text(dRow, "Value", 34, TextAlignmentOptions.MidlineRight, Color.white, font);
        _delayValue.fontStyle = FontStyles.Bold;
        UiFx.Layout(_delayValue.gameObject, minW: 160f, prefW: 160f);
        _delayGauge = MakeGauge(_bio, 0.6f);

        // статус
        _alert = UiFx.Text(_bio, "Alert", 22, TextAlignmentOptions.Center, Green, font);
        _alert.characterSpacing = 3f;
        UiFx.Layout(_alert.gameObject, minH: 36f, prefH: 36f);

        var anim = _bio.gameObject.AddComponent<UiPanelAnim>();
        anim.slideFrom = new Vector2(90f, 0f);
        anim.duration = 0.5f;
    }

    private Gauge MakeGauge(Transform parent, float threshold01)
    {
        RectTransform holder = UiFx.Rect("Gauge", parent);
        UiFx.Layout(holder.gameObject, minH: 16f, prefH: 16f);
        UiFx.Panel(holder, new Color(1f, 1f, 1f, 0.08f), 7);

        var g = new Gauge();
        g.fillImg = UiFx.Img(holder, "Fill", UiFx.Rounded(7), Cyan, true);
        g.fill = g.fillImg.rectTransform;
        g.fill.anchorMin = new Vector2(0f, 0f);
        g.fill.anchorMax = new Vector2(0.03f, 1f);
        g.fill.offsetMin = Vector2.zero;
        g.fill.offsetMax = Vector2.zero;

        g.marker = UiFx.Img(holder, "Marker", null, new Color(1f, 1f, 1f, 0.35f));
        RectTransform m = g.marker.rectTransform;
        m.anchorMin = new Vector2(threshold01, 0f);
        m.anchorMax = new Vector2(threshold01, 1f);
        m.pivot = new Vector2(0.5f, 0.5f);
        m.sizeDelta = new Vector2(3f, 12f);
        m.anchoredPosition = Vector2.zero;
        return g;
    }

    private void BuildEye(RectTransform holder)
    {
        Image ring1 = UiFx.Img(holder, "Ring1", UiFx.Ring(0.025f), new Color(Cyan.r, Cyan.g, Cyan.b, 0.55f));
        UiFx.Anchor(ring1.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(236f, 236f));
        _ring1 = ring1.rectTransform;

        Image ring2 = UiFx.Img(holder, "Ring2", UiFx.Ring(0.012f), new Color(Cyan.r, Cyan.g, Cyan.b, 0.25f));
        UiFx.Anchor(ring2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(206f, 206f));
        _ring2 = ring2.rectTransform;

        Image eyeball = UiFx.Img(holder, "Eyeball", UiFx.Circle(), new Color(0.92f, 0.95f, 0.98f));
        UiFx.Anchor(eyeball.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(176f, 176f));

        _irisImg = UiFx.Img(eyeball.transform, "Iris", UiFx.Circle(), new Color(0.20f, 0.70f, 0.90f));
        UiFx.Anchor(_irisImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(112f, 112f));
        Image irisRing = UiFx.Img(_irisImg.transform, "IrisRing", UiFx.Ring(0.14f), new Color(0f, 0f, 0f, 0.35f));
        UiFx.Stretch(irisRing.rectTransform);

        Image pupil = UiFx.Img(eyeball.transform, "Pupil", UiFx.Circle(), new Color(0.02f, 0.02f, 0.03f));
        UiFx.Anchor(pupil.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 96f));
        Image glint = UiFx.Img(pupil.transform, "Glint", UiFx.Circle(), new Color(1f, 1f, 1f, 0.85f));
        UiFx.Anchor(glint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(18f, 18f), new Vector2(14f, 14f));

        _eye = holder.gameObject.AddComponent<EyeBehaviour>();
        _eye.Bind(eyeball.rectTransform, _irisImg.rectTransform, pupil.rectTransform, null);
        _eye.IrisMoveRadius = 30f;
    }

    private void BuildDialogue()
    {
        _dialogue = UiFx.Rect("Dialogue", _canvasRt);
        UiFx.Anchor(_dialogue, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(1180f, 270f));
        UiFx.Panel(_dialogue, PanelBg, 22);

        _stripe = UiFx.Img(_dialogue, "Stripe", UiFx.Rounded(3), Cyan, true);
        RectTransform sr = _stripe.rectTransform;
        sr.anchorMin = new Vector2(0f, 0f);
        sr.anchorMax = new Vector2(0f, 1f);
        sr.pivot = new Vector2(0f, 0.5f);
        sr.sizeDelta = new Vector2(6f, -36f);
        sr.anchoredPosition = new Vector2(14f, 0f);

        _speakerPill = UiFx.Img(_dialogue, "Pill", UiFx.Rounded(18), new Color(1f, 1f, 1f, 0.15f), true);
        UiFx.Anchor(_speakerPill.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -24f), new Vector2(280f, 38f));
        _speakerText = UiFx.Text(_speakerPill.transform, "Text", 22, TextAlignmentOptions.Center, Cyan, font);
        _speakerText.fontStyle = FontStyles.Bold;
        _speakerText.characterSpacing = 3f;
        UiFx.Stretch(_speakerText.rectTransform);

        _dialogueText = UiFx.Text(_dialogue, "Text", 32, TextAlignmentOptions.TopLeft, TextMain, font);
        UiFx.Stretch(_dialogueText.rectTransform, 40f, 40f, 80f, 40f);

        _waitHint = UiFx.Text(_dialogue, "Hint", 20, TextAlignmentOptions.BottomRight, TextDim, font);
        _waitHint.text = Loc.T("[LMB] continue", "[ЛКМ] далее");
        UiFx.Anchor(_waitHint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-34f, 14f), new Vector2(300f, 30f));
        _waitHint.alpha = 0f;

        var anim = _dialogue.gameObject.AddComponent<UiPanelAnim>();
        anim.slideFrom = new Vector2(0f, -50f);
        anim.scaleFrom = 0.98f;
        anim.duration = 0.35f;
    }

    private void BuildQuestions()
    {
        _questions = UiFx.Rect("Questions", _canvasRt);
        UiFx.Anchor(_questions, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(1180f, 0f));

        var v = _questions.gameObject.AddComponent<VerticalLayoutGroup>();
        v.spacing = 14f;
        v.childAlignment = TextAnchor.LowerCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        var fit = _questions.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text header = UiFx.Text(_questions, "Header", 20, TextAlignmentOptions.MidlineLeft, TextDim, font);
        header.text = Loc.T("CHOOSE A QUESTION   [1] [2] [3]", "ВЫБЕРИ ВОПРОС   [1] [2] [3]");
        header.characterSpacing = 4f;
        UiFx.Layout(header.gameObject, minH: 30f, prefH: 30f);

        _qButtons = new Button[3];
        for (int i = 0; i < 3; i++) _qButtons[i] = MakeQuestionCard(_questions, i + 1);

        var anim = _questions.gameObject.AddComponent<UiPanelAnim>();
        anim.slideFrom = new Vector2(0f, -60f);
        anim.duration = 0.4f;
    }

    private Button MakeQuestionCard(Transform parent, int number)
    {
        RectTransform rt = UiFx.Rect("Question" + number, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = UiFx.Rounded(16);
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock c = btn.colors;
        c.normalColor = new Color(0.05f, 0.08f, 0.12f, 0.92f);
        c.highlightedColor = new Color(0.10f, 0.28f, 0.38f, 0.98f);
        c.pressedColor = new Color(0.08f, 0.20f, 0.28f, 1f);
        c.selectedColor = c.normalColor;
        c.disabledColor = new Color(0.05f, 0.06f, 0.08f, 0.6f);
        btn.colors = c;

        var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.padding = new RectOffset(18, 28, 14, 14);
        h.spacing = 22f;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;
        UiFx.Layout(rt.gameObject, minH: 84f);

        Image badge = UiFx.Img(rt, "Badge", UiFx.Rounded(12), new Color(Cyan.r, Cyan.g, Cyan.b, 0.18f), true);
        UiFx.Layout(badge.gameObject, 54f, 54f, 54f, 54f);
        TMP_Text num = UiFx.Text(badge.transform, "Num", 30, TextAlignmentOptions.Center, Cyan, font);
        num.fontStyle = FontStyles.Bold;
        num.text = number.ToString();
        UiFx.Stretch(num.rectTransform);

        TMP_Text label = UiFx.Text(rt, "Label", 28, TextAlignmentOptions.MidlineLeft, TextMain, font);
        UiFx.Layout(label.gameObject, flexW: 1f, minH: 54f);

        rt.gameObject.AddComponent<UiButtonFx>();
        img.raycastTarget = true;
        return btn;
    }

    private void BuildFinal()
    {
        _final = UiFx.Rect("Verdict", _canvasRt);
        UiFx.Anchor(_final, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 0f));
        UiFx.Panel(_final, new Color(0.03f, 0.05f, 0.08f, 0.94f), 26);

        var v = _final.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(44, 44, 34, 34);
        v.spacing = 20f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;
        var fit = _final.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TMP_Text title = UiFx.Text(_final, "Title", 56, TextAlignmentOptions.Center, Color.white, font);
        title.text = Loc.T("VERDICT", "ВЕРДИКТ");
        title.fontStyle = FontStyles.Bold;
        title.characterSpacing = 8f;
        UiFx.Layout(title.gameObject, minH: 72f, prefH: 72f);

        TMP_Text sub = UiFx.Text(_final, "Sub", 26, TextAlignmentOptions.Center, TextDim, font);
        sub.text = Loc.T("The interrogation is over. Who is in front of you?", "Допрос окончен. Кто перед тобой?");
        UiFx.Layout(sub.gameObject, minH: 36f, prefH: 36f);

        RectTransform sumHolder = UiFx.Rect("Summary", _final);
        UiFx.Panel(sumHolder, new Color(1f, 1f, 1f, 0.05f), 16);
        UiFx.Layout(sumHolder.gameObject, minH: 150f, prefH: 150f);
        _summary = UiFx.Text(sumHolder, "Text", 28, TextAlignmentOptions.TopLeft, TextMain, font);
        UiFx.Stretch(_summary.rectTransform, 26f, 26f, 18f, 14f);

        RectTransform btnRow = UiFx.Rect("Buttons", _final);
        var h = btnRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 24f;
        h.childControlWidth = true;
        h.childControlHeight = true;
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = true;
        UiFx.Layout(btnRow.gameObject, minH: 130f, prefH: 130f);

        _replicantBtn = MakeBigButton(btnRow, Loc.T("REPLICANT", "РЕПЛИКАНТ"), Loc.T("A machine", "Машина"),
            new Color(0.45f, 0.10f, 0.12f, 1f), new Color(0.80f, 0.20f, 0.24f, 1f));
        _humanBtn = MakeBigButton(btnRow, Loc.T("HUMAN", "ЧЕЛОВЕК"), Loc.T("A living person", "Живой человек"),
            new Color(0.08f, 0.35f, 0.18f, 1f), new Color(0.16f, 0.62f, 0.32f, 1f));

        _tapeBtn = MakeBigButton(_final, Loc.T("PLAY THE RECORDING", "ПРОСЛУШАТЬ ЗАПИСЬ"), null,
            new Color(0.40f, 0.30f, 0.06f, 1f), new Color(0.75f, 0.56f, 0.12f, 1f));
        UiFx.Layout(_tapeBtn.gameObject, minH: 70f, prefH: 70f);

        var anim = _final.gameObject.AddComponent<UiPanelAnim>();
        anim.scaleFrom = 0.9f;
        anim.slideFrom = new Vector2(0f, -30f);
        anim.duration = 0.45f;
    }

    private Button MakeBigButton(Transform parent, string title, string subtitle, Color normal, Color hover)
    {
        RectTransform rt = UiFx.Rect("Button", parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = UiFx.Rounded(18);
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        img.raycastTarget = true;

        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock c = btn.colors;
        c.normalColor = normal;
        c.highlightedColor = hover;
        c.pressedColor = Color.Lerp(normal, Color.black, 0.3f);
        c.selectedColor = normal;
        c.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.6f);
        btn.colors = c;

        TMP_Text label = UiFx.Text(rt, "Label", subtitle != null ? 42 : 30, TextAlignmentOptions.Center, Color.white, font);
        label.fontStyle = FontStyles.Bold;
        label.characterSpacing = 4f;
        label.text = subtitle != null ? $"{title}\n<size=48%><color=#FFFFFFB0>{subtitle}</color></size>" : title;
        UiFx.Stretch(label.rectTransform);

        rt.gameObject.AddComponent<UiButtonFx>();
        return btn;
    }
}
