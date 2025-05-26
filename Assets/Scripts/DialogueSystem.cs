using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class DialogueSystem : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text dialogueText;
    public Button[] questionButtons;
    public float typingSpeed = 0.05f;

    [Header("Stats UI")]
    public TMP_Text pulseText;
    public TMP_Text pupilText;
    public TMP_Text delayText;

    [Header("Диалоговая настройка")]
    public List<DialogueIteration> iterations;

    [Header("Ссылка на поведение глаза")]
    public EyeBehaviour eyeBehaviour;

    private int currentIteration = 0;

    public float pupilDilation;
    public float responseDelay;
    public int pulse;

    public AudioClip type;
    public AudioSource source;

    public GameObject QuestionPanel;
    public GameObject AnswerPanel;

    private bool isTyping = false;
private bool isWaitingForClick = false;
private Coroutine typingCoroutine;

    void Start()
    {
        AnswerPanel.SetActive(false);
        ShowIteration();
        UpdateStatsUI();
    }
void Update()
{
    if (Input.GetMouseButtonDown(0))
    {
        if (isTyping)
        {
            isTyping = false;
        }
        else
        {
            clickBuffered = true;
        }
    }
}
    void ShowIteration()
    {
        if (currentIteration >= iterations.Count)
        {
            dialogueText.text = "Диалог завершён.";
            foreach (var btn in questionButtons)
                btn.gameObject.SetActive(false);
            return;
        }
        dialogueText.text = "";
        var iteration = iterations[currentIteration];
        AnswerPanel.SetActive(false);
        QuestionPanel.SetActive(true);

        for (int i = 0; i < questionButtons.Length; i++)
        {
            int index = i;
            var q = iteration.questions[i];
            questionButtons[i].GetComponentInChildren<Text>().text = "[" + (i+1) + "]" + q.questionText;
            questionButtons[i].onClick.RemoveAllListeners();
            questionButtons[i].onClick.AddListener(() => OnQuestionChosen(q));
        }
    }

    void OnQuestionChosen(QuestionData question)
    {
        foreach (var button in questionButtons)
            button.interactable = false;

        StartCoroutine(DisplayFullDialogue(question));
    }
private bool clickBuffered = false;
    IEnumerator WaitForClick()
{
    clickBuffered = false;
    isWaitingForClick = true;
    yield return new WaitUntil(() => clickBuffered);
    isWaitingForClick = false;
    clickBuffered = false;
}

  IEnumerator DisplayFullDialogue(QuestionData question)
{
    dialogueText.text = "";

    ApplyEffect(question.effect);
    UpdateStatsUI();
        AnswerPanel.SetActive(true);
        QuestionPanel.SetActive(false);
    yield return TypeLine($"Detective: {question.questionText}");

    

    // Задержка ответа — заменим визуальной анимацией с ожиданием клика
    float delay = question.answer.effect.delayChange;
    float elapsed = 0f;
    int dotCount = 0;

    while (elapsed < delay)
    {
        dialogueText.text = $"Suspect: {new string('.', (dotCount % 3) + 1)}";
        dotCount++;
        elapsed += typingSpeed;
        yield return new WaitForSeconds(typingSpeed);

        if (!isTyping) break; // возможность прервать
    }

    ApplyEffect(question.answer.effect);
    UpdateStatsUI();
    yield return TypeLine($"Suspect: {question.answer.answerText}");

    

yield return WaitForClick();

    currentIteration++;

    foreach (var button in questionButtons)
        button.interactable = true;

    ShowIteration();
}

  IEnumerator TypeLine(string fullText)
{
    isTyping = true;
    dialogueText.text = "";

    for (int i = 0; i < fullText.Length; i++)
    {
        if (!isTyping)
        {
            dialogueText.text = fullText;
            break;
        }

        dialogueText.text += fullText[i];
        source.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
        source.PlayOneShot(type);
        yield return new WaitForSeconds(typingSpeed);
    }

    isTyping = false;
    isWaitingForClick = true;

    // Ждём ЛКМ
    yield return new WaitUntil(() => !isWaitingForClick);
}

    void ApplyEffect(DialogueEffect effect)
    {
        pupilDilation = Mathf.Clamp(pupilDilation + effect.pupilChange, 0f, 1f);
        responseDelay = Mathf.Max(0f, responseDelay + effect.delayChange);
        pulse = Mathf.Max(0, pulse + (int)effect.pulseChange);

        if (eyeBehaviour != null)
            eyeBehaviour.SetPupilTarget(pupilDilation);
    }

    void UpdateStatsUI()
    {
        pulseText.text = $"Pulse {pulse} BPM";
        pupilText.text = $"Iris {pupilDilation:F2}";
        delayText.text = $"Delay {responseDelay:F2} S";
    }
}

[Serializable]
public class DialogueIteration
{
    public List<QuestionData> questions = new List<QuestionData>(3);
}

[Serializable]
public class QuestionData
{
    public string questionText;
    public DialogueEffect effect;
    public AnswerData answer;
}

[Serializable]
public class AnswerData
{
    public string answerText;
    public DialogueEffect effect;
}

[Serializable]
public class DialogueEffect
{
    public float pupilChange;
    public float delayChange;
    public float pulseChange;
}