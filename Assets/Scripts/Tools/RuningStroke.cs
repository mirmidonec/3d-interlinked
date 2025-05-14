using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RuningStroke : MonoBehaviour
{
     public RectTransform panelRect;    // Панель с RectMask2D (окно просмотра)
    public TMP_Text textPrefab;            // Префаб текста (UI Text)
    public float scrollSpeed = 100f;   // Скорость прокрутки в пикселях в секунду

    private RectTransform text1;
    private RectTransform text2;
    private float textWidth;

    void Start()
    {
        // Создаём два экземпляра текста
        text1 = Instantiate(textPrefab, panelRect).rectTransform;
        text2 = Instantiate(textPrefab, panelRect).rectTransform;

        // Устанавливаем одинаковый текст (можно менять)
        text1.GetComponent<TMP_Text>().text = textPrefab.text;
        text2.GetComponent<TMP_Text>().text = textPrefab.text;

        // Получаем ширину текста (предпочтительная ширина)
        textWidth = text1.GetComponent<TMP_Text>().preferredWidth;

        // Устанавливаем начальные позиции: первый в начале панели, второй справа от первого
        text1.anchoredPosition = Vector2.zero;
        text2.anchoredPosition = new Vector2(textWidth, 0);
    }

    void Update()
    {
        float moveDelta = scrollSpeed * Time.deltaTime;

        // Смещаем оба текста влево
        text1.anchoredPosition -= new Vector2(moveDelta, 0);
        text2.anchoredPosition -= new Vector2(moveDelta, 0);

        // Если текст полностью ушёл за левую границу панели, переносим его вправо за другой текст
        if (text1.anchoredPosition.x <= -textWidth)
        {
            text1.anchoredPosition = new Vector2(text2.anchoredPosition.x + textWidth, 0);
        }
        if (text2.anchoredPosition.x <= -textWidth)
        {
            text2.anchoredPosition = new Vector2(text1.anchoredPosition.x + textWidth, 0);
        }
    }
}
