using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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


    public static bool IsEnding = false;


    public GameObject crosshair;

    public Connection con;


    public GameObject button1;
    public GameObject button2;


    public GameObject quest;

    private void Start()
    {
        IsEnding = false;
        firstTMP.text = "";
        secondTMP.text = "";
    }
    [ContextMenu("a")]
    public void StartEnd()
    {
        if (PauseMenu.CurrentLanguage == PauseMenu.Language.Russian)
        {
            firstText = "Конец";
            secondText = "Подозреваемый оказался ";
        }
        else
        {
            firstText = "The end";
            secondText = "The suspect was ";
        }
        NpcChoiser.Instance.currentNpc.Condition = variableValue;
        if (PauseMenu.CurrentLanguage == PauseMenu.Language.Russian)
        {
            if (variableValue.ToLower() == "a replicant")
                variableValue = "репликантом";
            else if (variableValue.ToLower() == "a human")
                variableValue = "человеком";
        }
        StartCoroutine(TypeSequence());
        quest.SetActive(false);
        crosshair.SetActive(false);
        con.SaveProgress();

    }

    public void QuitGame()
    {
        Application.Quit();
    }
    public void Rerty()
    {
        SceneManager.LoadScene(1);
    }
    private IEnumerator TypeSequence()
    {
        IsEnding = true;
        yield return StartCoroutine(TypeTextWithRichTags(firstTMP, firstText));

        string colorHex;
        if (variableValue.ToLower() == "a replicant" || variableValue.ToLower() == "репликантом")
            colorHex = "#FF0000";
        else if (variableValue.ToLower() == "a human" || variableValue.ToLower() == "человеком")
            colorHex = "#00FF00";
        else
            colorHex = "#FFFFFF";

        string coloredVariable = $"<color={colorHex}>{variableValue}</color>";
        string fullSecondText = secondText + coloredVariable;
        yield return new WaitForSecondsRealtime(0.5f);
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        yield return StartCoroutine(TypeTextWithRichTags(secondTMP, fullSecondText));
        button1.SetActive(true);
        yield return new WaitForSecondsRealtime(0.5f);
        button2.SetActive(true);
        IsEnding = false;
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
