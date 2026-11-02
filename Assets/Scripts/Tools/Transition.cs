using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Transition : MonoBehaviour
{
    [SerializeField] private Image _blackScreen;
    [SerializeField] private float fadeDuration;

    public static Transition Instance;

    private void Awake()
    {
        Instance = this;
    }


    public IEnumerator FadeOut()
    {
        Color color = _blackScreen.color;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            _blackScreen.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        _blackScreen.color = new Color(color.r, color.g, color.b, 0f);

        gameObject.SetActive(false);
    }
    public IEnumerator FadeIn()
    {
        Color color = _blackScreen.color;
        float t = 0f;

        gameObject.SetActive(true);

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            _blackScreen.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        _blackScreen.color = new Color(color.r, color.g, color.b, 1f);
        SceneManager.LoadScene(1);
    }

}
