using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Плавное увеличение при наведении и "вдавливание" при нажатии. Вешается на кнопки.</summary>
public class UiButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public float hoverScale = 1.04f;
    public float pressScale = 0.96f;
    public float speed = 16f;

    private bool _hover;
    private bool _down;
    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();

        // Кнопка, на которую кликнули, остаётся "выбранной" и перестаёт подсвечиваться при наведении.
        // Отключаем навигацию/выбор, и подсветка работает всегда.
        if (_button != null)
        {
            Navigation n = _button.navigation;
            n.mode = Navigation.Mode.None;
            _button.navigation = n;
        }
    }

    private void OnEnable()
    {
        _hover = false;
        _down = false;
        transform.localScale = Vector3.one;
    }

    private void Update()
    {
        float target = 1f;
        if (_button == null || _button.interactable)
            target = _down ? pressScale : (_hover ? hoverScale : 1f);

        float k = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * target, k);
    }

    public void OnPointerEnter(PointerEventData e) { _hover = true; }
    public void OnPointerExit(PointerEventData e) { _hover = false; _down = false; }
    public void OnPointerDown(PointerEventData e) { _down = true; }
    public void OnPointerUp(PointerEventData e) { _down = false; }
}