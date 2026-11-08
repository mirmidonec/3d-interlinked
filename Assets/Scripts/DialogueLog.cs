
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueLog : MonoBehaviour
{
    public static DialogueLog Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_Text logText;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Names")]
    [SerializeField] private string detectiveName = "DETECTIVE";
    [SerializeField] private string suspectName = "SUSPECT";

    [Header("Colors")]
    [SerializeField] private Color detectiveColor =
        new Color(0.25f, 0.85f, 1f);

    [SerializeField] private Color suspectColor =
        new Color(1f, 0.55f, 0.35f);

    [SerializeField] private Color systemColor =
        new Color(0.65f, 0.68f, 0.72f);

    [Header("Keyboard Scroll")]
    [SerializeField] private float scrollSpeed = 350f;

    private bool initialized;
    private bool isLayoutUpdating;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (logText == null || scrollRect == null)
        {
            Debug.LogError("DialogueLog: Assign Log Text and Scroll Rect!");
            enabled = false;
            return;
        }

        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.inertia = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        logText.text = "";
        logText.richText = true;
        logText.enableWordWrapping = true;

        initialized = true;

        AddSystemMessage("INTERROGATION RECORD STARTED");
    }

    private void Update()
    {
        if (!initialized)
            return;

        float direction = 0f;

        if (Input.GetKey(KeyCode.S))
            direction = 1f;

        if (Input.GetKey(KeyCode.W))
            direction = -1f;

        if (direction == 0f)
            return;

        float contentHeight = scrollRect.content.rect.height;
        float viewportHeight = scrollRect.viewport.rect.height;

        if (contentHeight <= viewportHeight)
            return;

        float maxScroll = contentHeight - viewportHeight;

        float movement = scrollSpeed * Time.unscaledDeltaTime;

        // W = вверх, S = вниз
        float currentY = scrollRect.content.anchoredPosition.y;

        currentY += direction * movement;

        currentY = Mathf.Clamp(currentY, 0f, maxScroll);

        scrollRect.content.anchoredPosition =
            new Vector2(
                scrollRect.content.anchoredPosition.x,
                currentY
            );
    }

    public void AddDetectiveMessage(string message)
    {
        AddMessage(detectiveName, message, detectiveColor);
    }

    public void AddSuspectMessage(string message)
    {
        AddMessage(suspectName, message, suspectColor);
    }

    public void AddSystemMessage(string message)
    {
        if (!initialized)
            return;

        string formatted =
            $"<align=center><color=#{ColorUtility.ToHtmlStringRGB(systemColor)}>" +
            $"<size=75%>— {Escape(message)} —</size></color></align>\n\n";

        Append(formatted);
    }

    private void AddMessage(string speaker, string message, Color color)
    {
        if (!initialized || string.IsNullOrEmpty(message))
            return;

        string hex = ColorUtility.ToHtmlStringRGB(color);

        string formatted =
            $"<color=#{hex}><b>{Escape(speaker)}</b></color>\n" +
            $"<color=#E6E8EB>{Escape(message)}</color>\n" +
            $"<color=#454B55>────────────────────────</color>\n";

        Append(formatted);
    }

    private void Append(string formatted)
    {
        // Запоминаем, находился ли пользователь внизу
        bool wasAtBottom =
            scrollRect.verticalNormalizedPosition <= 0.02f;

        logText.text += formatted;

        StartCoroutine(UpdateLayout(wasAtBottom));
    }

    private IEnumerator UpdateLayout(bool wasAtBottom)
    {
        if (isLayoutUpdating)
            yield break;

        isLayoutUpdating = true;

        yield return null;

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            scrollRect.content
        );

        Canvas.ForceUpdateCanvases();

        if (wasAtBottom)
            scrollRect.verticalNormalizedPosition = 0f;

        isLayoutUpdating = false;
    }

    public void ClearLog()
    {
        if (!initialized)
            return;

        logText.text = "";
        AddSystemMessage("RECORD CLEARED");
    }

    private string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
