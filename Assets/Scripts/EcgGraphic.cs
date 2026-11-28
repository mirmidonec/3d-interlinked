using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Бегущая кардиограмма. Рисуется в маленькую текстуру (RawImage), поэтому не зависит от
/// кастомных мешей UI. Частота ударов = bpm, хвост линии плавно затухает слева.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class EcgGraphic : MonoBehaviour
{
    public float bpm = 75f;
    public Color color = new Color(0.25f, 0.85f, 1f);
    public float samplesPerSecond = 150f;
    public float amplitude = 0.40f;
    public float noise = 0.015f;

    /// <summary>Фаза текущего удара 0..1 (пик R около 0.245), для синхронизации сердечка.</summary>
    public float Phase => _phase;

    private const int W = 400;
    private const int H = 96;

    private RawImage _img;
    private Texture2D _tex;
    private Color32[] _px;
    private readonly float[] _s = new float[W];
    private float _phase;
    private float _acc;

    private void Awake()
    {
        _img = GetComponent<RawImage>();
        _img.raycastTarget = false;
        _img.color = Color.white;

        _tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        _tex.wrapMode = TextureWrapMode.Clamp;
        _tex.filterMode = FilterMode.Bilinear;
        _px = new Color32[W * H];
        _img.texture = _tex;
    }

    private void OnDestroy()
    {
        if (_tex != null) Destroy(_tex);
    }

    private static float G(float x, float c, float w)
    {
        float d = (x - c) / w;
        return Mathf.Exp(-d * d);
    }

    private static float Beat(float x)
    {
        return 0.12f * G(x, 0.12f, 0.035f)
             - 0.14f * G(x, 0.215f, 0.016f)
             + 1.00f * G(x, 0.245f, 0.017f)
             - 0.28f * G(x, 0.285f, 0.017f)
             + 0.22f * G(x, 0.50f, 0.060f);
    }

    private void Update()
    {
        _acc += Time.unscaledDeltaTime * samplesPerSecond;
        int whole = Mathf.FloorToInt(_acc);
        _acc -= whole;
        int steps = Mathf.Min(whole, W);

        float phaseStep = (Mathf.Max(bpm, 30f) / 60f) / samplesPerSecond;
        for (int k = 0; k < steps; k++)
        {
            System.Array.Copy(_s, 1, _s, 0, W - 1);
            _phase = (_phase + phaseStep) % 1f;
            _s[W - 1] = Beat(_phase) + (Random.value - 0.5f) * 2f * noise;
        }

        Draw();
    }

    private void Draw()
    {
        System.Array.Clear(_px, 0, _px.Length);

        float mid = H * 0.5f;
        float amp = H * amplitude;
        byte r = (byte)(Mathf.Clamp01(color.r) * 255f);
        byte g = (byte)(Mathf.Clamp01(color.g) * 255f);
        byte b = (byte)(Mathf.Clamp01(color.b) * 255f);

        for (int x = 0; x < W; x++)
        {
            float trail = Mathf.Pow((float)x / (W - 1), 1.5f);
            if (trail < 0.02f) continue;

            float y1 = mid + _s[x] * amp;
            float y0 = x > 0 ? mid + _s[x - 1] * amp : y1;
            float lo = Mathf.Min(y0, y1);
            float hi = Mathf.Max(y0, y1);

            int from = Mathf.FloorToInt(lo) - 5;
            int to = Mathf.CeilToInt(hi) + 5;
            for (int y = from; y <= to; y++)
            {
                if (y < 0 || y >= H) continue;

                float d = y < lo ? lo - y : (y > hi ? y - hi : 0f);
                float core = Mathf.Clamp01(1.7f - d);
                float glow = Mathf.Clamp01(1f - d / 4.5f) * 0.28f;
                float a = Mathf.Max(core, glow) * trail * color.a;
                if (a <= 0.004f) continue;

                _px[y * W + x] = new Color32(r, g, b, (byte)(Mathf.Clamp01(a) * 255f));
            }
        }

        _tex.SetPixels32(_px);
        _tex.Apply(false);
    }
}