using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Общие помощники для UI: процедурные спрайты (скруглённые панели, круги), твины, построители.</summary>
public static class UiFx
{
    // ===================================================================
    // Easing
    // ===================================================================

    public static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    public static float EaseOutBack(float t)
    {
        t = Mathf.Clamp01(t);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // ===================================================================
    // Процедурные спрайты (никаких ассетов не нужно)
    // ===================================================================

    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    private static Sprite Build(string key, int size, Func<float, float, float> alphaFn, Vector4 border)
    {
        if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.HideAndDontSave;

        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alphaFn(x + 0.5f, y + 0.5f)));
        tex.SetPixels(px);
        tex.Apply();

        Sprite s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, border);
        s.hideFlags = HideFlags.HideAndDontSave;
        Cache[key] = s;
        return s;
    }

    /// <summary>Скруглённый прямоугольник, использовать с Image.Type.Sliced.</summary>
    public static Sprite Rounded(int radius = 16)
    {
        int border = radius + 2;
        int size = border * 2 + 2;
        float half = size * 0.5f;
        float r = radius;
        return Build("round" + radius, size, (x, y) =>
        {
            float dx = Mathf.Abs(x - half) - (half - r);
            float dy = Mathf.Abs(y - half) - (half - r);
            float outside = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
            float inside = Mathf.Min(Mathf.Max(dx, dy), 0f);
            float d = outside + inside - r;
            return 0.5f - d;
        }, new Vector4(border, border, border, border));
    }

    public static Sprite Circle()
    {
        const int size = 128;
        float c = size * 0.5f;
        float R = c - 1f;
        return Build("circle", size, (x, y) =>
        {
            float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
            return R - d + 0.5f;
        }, Vector4.zero);
    }

    /// <summary>Кольцо; thickness — доля радиуса (0.05 = тонкое).</summary>
    public static Sprite Ring(float thickness)
    {
        const int size = 128;
        float c = size * 0.5f;
        float R = c - 1f;
        float inner = R * (1f - thickness);
        return Build("ring" + thickness, size, (x, y) =>
        {
            float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
            return Mathf.Clamp01(R - d + 0.5f) * Mathf.Clamp01(d - inner + 0.5f);
        }, Vector4.zero);
    }

    /// <summary>Виньетка: прозрачный центр, плотные края. Растягивается на экран.</summary>
    public static Sprite Vignette()
    {
        const int size = 128;
        float c = size * 0.5f;
        return Build("vignette", size, (x, y) =>
        {
            float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
            return Mathf.SmoothStep(0f, 1f, (d - 0.55f) / 0.65f);
        }, Vector4.zero);
    }

    // ===================================================================
    // Построители
    // ===================================================================

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    public static void Stretch(RectTransform rt, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    public static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    public static Image Img(Transform parent, string name, Sprite sprite, Color color, bool sliced = false)
    {
        RectTransform rt = Rect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        if (sliced) img.type = Image.Type.Sliced;
        return img;
    }

    /// <summary>Фон-панель на самом объекте.</summary>
    public static Image Panel(RectTransform rt, Color color, int radius)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = Rounded(radius);
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static TMP_Text Text(Transform parent, string name, int size, TextAlignmentOptions align, Color color, TMP_FontAsset font)
    {
        RectTransform rt = Rect(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.richText = true;
        t.raycastTarget = false;
        return t;
    }

    public static LayoutElement Layout(GameObject go, float minW = -1f, float minH = -1f, float prefW = -1f, float prefH = -1f, float flexW = -1f)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        if (minW >= 0f) le.minWidth = minW;
        if (minH >= 0f) le.minHeight = minH;
        if (prefW >= 0f) le.preferredWidth = prefW;
        if (prefH >= 0f) le.preferredHeight = prefH;
        if (flexW >= 0f) le.flexibleWidth = flexW;
        return le;
    }

    // ===================================================================
    // Твины (всё на unscaled time, работают и на паузе)
    // ===================================================================

    public static IEnumerator Fade(CanvasGroup g, float from, float to, float time)
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

    /// <summary>Кратковременное "подпрыгивание" масштаба (базовый масштаб 1).</summary>
    public static IEnumerator Punch(Transform t, float amount, float time)
    {
        float el = 0f;
        while (el < time && t != null)
        {
            el += Time.unscaledDeltaTime;
            float s = 1f + amount * Mathf.Sin(Mathf.Clamp01(el / time) * Mathf.PI);
            t.localScale = Vector3.one * s;
            yield return null;
        }
        if (t != null) t.localScale = Vector3.one;
    }

    /// <summary>Появление: прозрачность 0→1 и масштаб 0.9→1 с пружиной.</summary>
    public static IEnumerator Appear(CanvasGroup g, float delay, float time)
    {
        g.alpha = 0f;
        g.transform.localScale = Vector3.one * 0.9f;
        float d = 0f;
        while (d < delay)
        {
            d += Time.unscaledDeltaTime;
            yield return null;
        }
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / time);
            g.alpha = EaseOutCubic(k);
            g.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, EaseOutBack(k));
            yield return null;
        }
        g.alpha = 1f;
        g.transform.localScale = Vector3.one;
    }

    public static IEnumerator Shake(RectTransform rt, float amplitude, float time)
    {
        Vector2 basePos = rt.anchoredPosition;
        float el = 0f;
        while (el < time && rt != null)
        {
            el += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(el / time);
            rt.anchoredPosition = basePos + new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)) * amplitude * k;
            yield return null;
        }
        if (rt != null) rt.anchoredPosition = basePos;
    }
}
