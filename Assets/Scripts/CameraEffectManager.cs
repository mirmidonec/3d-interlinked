using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class CameraEffectManager : MonoBehaviour
{
    public PostProcessVolume volume;
    private ColorGrading colorGrading;

    public static CameraEffectManager Instance;
    private void Awake()
    {
        Instance = this;
    }
    public void StartRed()
    {
        StartCoroutine(RedScreenRoutine());
    }
    public IEnumerator RedScreenRoutine()
    {
        if (volume.profile.TryGetSettings(out colorGrading))
        {
            Debug.Log("color found!");
        }
        float duration = 5f;
        float timer = 0f;

        Color startColor = new Color(1f, 1f, 1f);
        Color targetColor = new Color(0.7f, 0f, 0f);

        colorGrading.colorFilter.value = startColor;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            colorGrading.colorFilter.value = Color.Lerp(startColor, targetColor, timer / duration);
            yield return null;
        }

        colorGrading.colorFilter.value = targetColor;
    }

}
