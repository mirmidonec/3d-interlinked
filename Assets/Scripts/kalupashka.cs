
using UnityEngine;
using UnityEngine.UI;

public class kalupashka : MaskableGraphic
{
    [Header("Slider")]
    [SerializeField] private Slider frequencySlider;

    [Range(0f, 1f)]
    [SerializeField] private float idealFrequency = 0.72f;

    [Header("Wave Settings")]
    [SerializeField] private int points = 200;
    [SerializeField] private float thickness = 2f;
    [SerializeField] private float amplitude = 0.35f;

    [Header("Transition")]
    [SerializeField] private float transitionSpeed = 2f;

    private float currentAmplitude;
    private float targetAmplitude;

    private float currentFrequency;
    private float targetFrequency;

    private float currentNoise;
    private float targetNoise;

    private float phase;
    private float matchQuality;

    protected override void OnEnable()
    {
        base.OnEnable();

        raycastTarget = false;

        if (frequencySlider != null)
        {
            frequencySlider.onValueChanged.AddListener(
                OnFrequencyChanged
            );

            OnFrequencyChanged(frequencySlider.value);
        }
    }

    protected override void OnDisable()
    {
        if (frequencySlider != null)
        {
            frequencySlider.onValueChanged.RemoveListener(
                OnFrequencyChanged
            );
        }

        base.OnDisable();
    }

    private void OnFrequencyChanged(float value)
    {
        float normalizedValue = Mathf.InverseLerp(
            frequencySlider.minValue,
            frequencySlider.maxValue,
            value
        );

        // Насколько близко игрок к идеальной частоте
        float distance = Mathf.Abs(
            normalizedValue - idealFrequency
        );

        matchQuality = 1f - Mathf.Clamp01(distance / 0.25f);

        // Чем ближе к идеалу, тем стабильнее сигнал
        targetAmplitude = Mathf.Lerp(
            0.45f, 0.08f, matchQuality
        );

        targetFrequency = Mathf.Lerp(
            12f, 3f, matchQuality
        );

        targetNoise = Mathf.Lerp(
            0.3f, 0.005f, matchQuality
        );

        SetVerticesDirty();
    }

    private void Update()
    {
        currentAmplitude = Mathf.Lerp(
            currentAmplitude,
            targetAmplitude,
            transitionSpeed * Time.deltaTime
        );

        currentFrequency = Mathf.Lerp(
            currentFrequency,
            targetFrequency,
            transitionSpeed * Time.deltaTime
        );

        currentNoise = Mathf.Lerp(
            currentNoise,
            targetNoise,
            transitionSpeed * Time.deltaTime
        );

        phase += Time.deltaTime * 2f;

        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();

        for (int i = 0; i < points - 1; i++)
        {
            float t1 = (float)i / (points - 1);
            float t2 = (float)(i + 1) / (points - 1);

            float x1 = rect.xMin + t1 * rect.width;
            float x2 = rect.xMin + t2 * rect.width;

            float y1 = GetWave(t1, rect);
            float y2 = GetWave(t2, rect);

            DrawLine(
                vh,
                new Vector2(x1, y1),
                new Vector2(x2, y2)
            );
        }
    }

    private float GetWave(float t, Rect rect)
    {
        float wave = Mathf.Sin(
            t * currentFrequency * Mathf.PI * 2f - phase
        );

        float noise = (
            Mathf.PerlinNoise(t * 35f, Time.time * 8f) - 0.5f
        ) * 2f;

        float result =
            wave * currentAmplitude +
            noise * currentNoise;

        return rect.center.y +
            result * rect.height * amplitude;
    }
public float MatchQuality => matchQuality;

public void SetIdeal(float value)
{
    idealFrequency = Mathf.Clamp01(value);
    if (frequencySlider != null) OnFrequencyChanged(frequencySlider.value);
}
    private void DrawLine(
        VertexHelper vh,
        Vector2 start,
        Vector2 end)
    {
        Vector2 direction = (end - start).normalized;
        Vector2 normal = new Vector2(-direction.y, direction.x);

        Vector2 offset = normal * thickness * 0.5f;

        int index = vh.currentVertCount;

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = start - offset;
        vh.AddVert(vertex);

        vertex.position = start + offset;
        vh.AddVert(vertex);

        vertex.position = end + offset;
        vh.AddVert(vertex);

        vertex.position = end - offset;
        vh.AddVert(vertex);

        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index, index + 2, index + 3);
    }
}
