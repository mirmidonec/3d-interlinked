using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Ending : MonoBehaviour
{
    [Header("Ссылки на TMP")]
    public TMP_Text firstTMP;
    public TMP_Text secondTMP;

    [Header("Звук печати")]
    public AudioSource typingAudioSource;
    public AudioClip typingSound;

    [Header("Тексты")]
    public string firstText = "The end";
    public string secondText = "The suspect was ";

    [Header("Настройки")]
    public float typingSpeed = 0.05f;
    public string variableValue;

    private void Start()
    {
        firstTMP.text = "";
        secondTMP.text = "";
    }
    public void StartEnd()
    {
         StartCoroutine(TypeSequence());
    }
    private IEnumerator TypeSequence()
    {

        yield return StartCoroutine(TypeTextWithRichTags(firstTMP, firstText));

        string colorHex;
        if (variableValue.ToLower() == "a replicant")
            colorHex = "#FF0000";
        else if (variableValue.ToLower() == "a human")
            colorHex = "#00FF00"; 
        else
            colorHex = "#FFFFFF";

        string coloredVariable = $"<color={colorHex}>{variableValue}</color>";
        string fullSecondText = secondText + coloredVariable;
        yield return new WaitForSecondsRealtime(0.5f);
        yield return StartCoroutine(TypeTextWithRichTags(secondTMP, fullSecondText));
    }

       private IEnumerator TypeTextWithRichTags(TMP_Text tmpField, string text)
    {
        tmpField.text = "";

        int i = 0;
        while (i < text.Length)
        {
            if (text[i] == '<') 
            {
                int closeIndex = text.IndexOf('>', i);
                if (closeIndex == -1)
                {
                    tmpField.text += text[i];
                    i++;
                }
                else
                {
                    string tag = text.Substring(i, closeIndex - i + 1);
                    tmpField.text += tag;
                    i = closeIndex + 1;
                }
            }
            else
            {
                tmpField.text += text[i];

                if (typingAudioSource != null && typingSound != null)
                    typingAudioSource.PlayOneShot(typingSound);

                i++;

                yield return new WaitForSeconds(typingSpeed);
            }
        }
    }
}
