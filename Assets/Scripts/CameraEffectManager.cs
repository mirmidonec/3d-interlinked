using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class CameraEffectManager : MonoBehaviour
{
    public PostProcessVolume volume;
    private ColorGrading colorGrading;

    public static CameraEffectManager Instance;

    [SerializeField] private Image _blackScreen;

    public GameObject endPanel;
    [SerializeField] private float fadeDuration;

    public PlayerController player;
    private void Awake()
    {
        Instance = this;
    }
    public void StartRed()
    {
        StartCoroutine(RedScreenRoutine());
    }
    [ContextMenu("a")]
      public void StartEnd()
    {
        StartCoroutine(EndScreenRoutine());
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
    public Ending end;

    public AudioSource source;
    public AudioClip endSound;
    public GameObject gun;
    public GameObject note;
    public IEnumerator EndScreenRoutine()
    {

        yield return new WaitForSeconds(4f);
        note.SetActive(false);
        gun.SetActive(false);
        source.clip = endSound;
        source.Play();
        player.enabled = false;
        endPanel.SetActive(true);
        Color color = _blackScreen.color;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            _blackScreen.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }
        end.StartEnd();
        _blackScreen.color = new Color(color.r, color.g, color.b, 1f);
        yield return new WaitForSeconds(5f);
        Debug.Log("End");
    }





}
