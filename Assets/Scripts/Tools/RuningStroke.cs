using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RuningStroke : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform _panelRect;
    [SerializeField] private TMP_Text _textPrefab;

    [Header("Scrolling Settings")]
    [SerializeField] private float _scrollSpeed;

    private RectTransform _textInstance1;
    private RectTransform _textInstance2;
    private float _textWidth;

    private void Start()
    {
        if (_panelRect == null || _textPrefab == null)
        {
            Debug.LogError("Panel Rect or Text Prefab is not assigned.");
            enabled = false;
            return;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        InitializeScrollingText();
    }

    private void Update()
    {
        ScrollText();

        if (Input.GetKeyDown(KeyCode.F5))
        {
            StartCoroutine(Transition.Instance.FadeIn());
        }
        else if (Input.GetKeyDown(KeyCode.F6))
        {
            Application.Quit();
        }
    }

    private void InitializeScrollingText()
    {
        _textInstance1 = Instantiate(_textPrefab, _panelRect).rectTransform;
        _textInstance2 = Instantiate(_textPrefab, _panelRect).rectTransform;

        string textContent = _textPrefab.text;
        _textInstance1.GetComponent<TMP_Text>().text = textContent;
        _textInstance2.GetComponent<TMP_Text>().text = textContent;

        _textWidth = _textInstance1.GetComponent<TMP_Text>().preferredWidth;

        _textInstance1.anchoredPosition = Vector2.zero;
        _textInstance2.anchoredPosition = new Vector2(_textWidth, 0);
    }

    private void ScrollText()
    {
        float deltaX = _scrollSpeed * Time.deltaTime;

        _textInstance1.anchoredPosition -= new Vector2(deltaX, 0);
        _textInstance2.anchoredPosition -= new Vector2(deltaX, 0);

        if (_textInstance1.anchoredPosition.x <= -_textWidth)
        {
            _textInstance1.anchoredPosition = new Vector2(_textInstance2.anchoredPosition.x + _textWidth, 0);
        }
        if (_textInstance2.anchoredPosition.x <= -_textWidth)
        {
            _textInstance2.anchoredPosition = new Vector2(_textInstance1.anchoredPosition.x + _textWidth, 0);
        }
    }
}
