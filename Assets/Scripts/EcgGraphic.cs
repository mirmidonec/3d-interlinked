using UnityEngine;
using UnityEngine.UI;

/// <summary>Бегущая кардиограмма. Частота ударов = bpm. Рисуется ломаной со свечением и затухающим хвостом.</summary>
public class EcgGraphic : MaskableGraphic
{
    public float bpm = 75f;
    public int sampleCount = 360;
    public float samplesPerSecond = 180f;
    public float thickness = 2.4f;
    public float amplitude = 0.42f;
    public float noise = 0.015f;

    /// <summary>Фаза текущего удара 0..1 (пик R около 0.245), для синхронизации сердечка.</summary>
    public float Phase => _phase;

    private float[] _s;
    private float _phase;
    private float _acc;

    protected override void OnEnable()
    {
        base.OnEnable();
        raycastTarget = false;
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
        if (_s == null || _s.Length != sampleCount) _s = new float[sampleCount];

        _acc += Time.unscaledDeltaTime * samplesPerSecond;
        int whole = Mathf.FloorToInt(_acc);
        _acc -= whole;
        int steps = Mathf.Min(whole, sampleCount);

        float phaseStep = (Mathf.Max(bpm, 30f) / 60f) / samplesPerSecond;
        for (int k = 0; k < steps; k++)
        {
            System.Array.Copy(_s, 1, _s, 0, _s.Length - 1);
            _phase = (_phase + phaseStep) % 1f;
            _s[_s.Length - 1] = Beat(_phase) + (Random.value - 0.5f) * 2f * noise;
        }

        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (_s == null) return;

        Rect r = GetPixelAdjustedRect();
        int n = _s.Length;

        for (int pass = 0; pass < 2; pass++)
        {
            float th = pass == 0 ? thickness * 3.2f : thickness;
            float aMul = pass == 0 ? 0.22f : 1f;

            for (int i = 0; i < n - 1; i++)
            {
                float t0 = (float)i / (n - 1);
                float t1 = (float)(i + 1) / (n - 1);

                Vector2 a = new Vector2(r.xMin + t0 * r.width, r.center.y + _s[i] * amplitude * r.height);
                Vector2 b = new Vector2(r.xMin + t1 * r.width, r.center.y + _s[i + 1] * amplitude * r.height);

                Color c0 = color; c0.a *= Mathf.Pow(t0, 1.6f) * aMul;
                Color c1 = color; c1.a *= Mathf.Pow(t1, 1.6f) * aMul;
                if (c1.a < 0.01f) continue;

                AddSegment(vh, a, b, th, c0, c1);
            }
        }
    }

    private static void AddSegment(VertexHelper vh, Vector2 a, Vector2 b, float thick, Color c0, Color c1)
    {
        Vector2 dir = (b - a);
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir.Normalize();
        Vector2 off = new Vector2(-dir.y, dir.x) * thick * 0.5f;

        int idx = vh.currentVertCount;
        UIVertex v = UIVertex.simpleVert;

        v.color = c0; v.position = a - off; vh.AddVert(v);
        v.color = c0; v.position = a + off; vh.AddVert(v);
        v.color = c1; v.position = b + off; vh.AddVert(v);
        v.color = c1; v.position = b - off; vh.AddVert(v);

        vh.AddTriangle(idx, idx + 1, idx + 2);
        vh.AddTriangle(idx, idx + 2, idx + 3);
    }
}
