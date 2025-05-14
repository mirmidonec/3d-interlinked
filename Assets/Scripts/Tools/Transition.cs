using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Transition : MonoBehaviour
{
    public Image blackScreen;
    public float fadeDuration = 2f;

 public PlayerController player;



    public  IEnumerator FadeOut()
    {
        Color color = blackScreen.color;
        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            blackScreen.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        blackScreen.color = new Color(color.r, color.g, color.b, 0f);

        gameObject.SetActive(false);
        player.enabled = true;
    }

}
