using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pillow : MonoBehaviour
{
    [SerializeField] private Transform[] segments; // Сегменты гармошки
    [SerializeField] private float foldAngle = -20f; // Угол поворота по Z
    [SerializeField] private float foldDuration = 0.3f; // Длительность одного поворота
    [SerializeField] private float delayBetweenFolds = 0.1f; // Задержка между сегментами
    [SerializeField] private bool loop = true; // Зацикливание

    private Quaternion[] originalRotations;

    private void Start()
    {
        // Сохраняем изначальные повороты
        originalRotations = new Quaternion[segments.Length];
        for (int i = 0; i < segments.Length; i++)
            originalRotations[i] = segments[i].localRotation;

        StartCoroutine(FoldAndUnfold());
    }

private IEnumerator FoldAndUnfold()
{
    while (true)
    {
        if (!Device.Instance.IsOn)
        {
            yield return null; // Ждём следующий кадр, если устройство выключено
            continue;
        }
        // Сгибание
        yield return StartCoroutine(FoldSequence(true));
        yield return new WaitForSeconds(0.2f);
        // Распрямление
        yield return StartCoroutine(FoldSequence(false));
        yield return new WaitForSeconds(0.4f);

        if (!loop) break;
    }
}

    private IEnumerator FoldSequence(bool fold)
    {
        for (int i = 0; i < segments.Length; i++)
        {
            int index = fold ? i : segments.Length - 1 - i;
            StartCoroutine(RotateSegmentZ(segments[index], fold ? foldAngle : 0f, foldDuration));
            yield return new WaitForSeconds(delayBetweenFolds);
        }
    }

    private IEnumerator RotateSegmentZ(Transform segment, float targetZAngle, float duration)
    {
        Quaternion startRot = segment.localRotation;
        int index = System.Array.IndexOf(segments, segment);
        Quaternion targetRot = originalRotations[index] * Quaternion.Euler(0f, 0f, targetZAngle);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            segment.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        segment.localRotation = targetRot;
    }
}
