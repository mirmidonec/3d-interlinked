using UnityEngine;

/// <summary>
/// Анимация появления панели при каждом включении (SetActive(true)):
/// плавная прозрачность, сдвиг и лёгкая "пружина" масштаба.
/// Добавлять ПОСЛЕ того, как панели выставлена позиция.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UiPanelAnim : MonoBehaviour
{
    public Vector2 slideFrom = Vector2.zero;
    public float scaleFrom = 1f;
    public float duration = 0.35f;
    public float delay = 0f;

    private RectTransform _rt;
    private CanvasGroup _cg;
    private Vector2 _basePos;
    private float _t;
    private bool _ready;

    private void Awake()
    {
        _rt = (RectTransform)transform;
        _cg = GetComponent<CanvasGroup>();
        _basePos = _rt.anchoredPosition;
        _ready = true;
    }

    private void OnEnable()
    {
        if (!_ready) return;
        _t = -delay;
        Apply(0f);
    }

    private void Update()
    {
        if (_t >= duration) return;
        _t += Time.unscaledDeltaTime;
        Apply(Mathf.Clamp01(_t / duration));
    }

    private void Apply(float k)
    {
        float e = UiFx.EaseOutCubic(k);
        _cg.alpha = e;
        _rt.anchoredPosition = Vector2.LerpUnclamped(_basePos + slideFrom, _basePos, e);
        _rt.localScale = Vector3.one * Mathf.LerpUnclamped(scaleFrom, 1f, UiFx.EaseOutBack(k));
    }
}
