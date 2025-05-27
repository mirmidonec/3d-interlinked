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
    private float pulse = 75f;

    public AudioClip type;
    public AudioSource source;

    public GameObject QuestionPanel;
    public GameObject AnswerPanel;

    private bool isTyping = false;
    private bool isWaitingForClick = false;

    [Header("Естественные колебания")]
    public float pulseMin = 70f;
    public float pulseMax = 80f;
    public float pupilMin = 0.5f;
    public float pupilMax = 0.6f;
    public float naturalChangeSpeed = 0.5f;

    private float pulseTarget;
    private float pupilTarget;

    private Coroutine pulseBlinkCoroutine;
    private Coroutine pupilBlinkCoroutine;
    private Coroutine delayBlinkCoroutine;

    IEnumerator BlinkText(TMP_Text text)
    {
        Color normalColor = text.color;
        Color alertColor = Color.red;
        float blinkSpeed = 1f;
        float scaleAmplitude = 0.1f;
        float baseScale = 1f;

        Transform t = text.transform;

        while (true)
        {
            float tColor = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            text.color = Color.Lerp(normalColor, alertColor, tColor);

            float scale = baseScale + Mathf.Sin(Time.time * blinkSpeed * Mathf.PI * 2) * scaleAmplitude;
            t.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }
    }
    void HandleBlinking(TMP_Text text, bool shouldBlink, ref Coroutine coroutine)
    {
        if (shouldBlink)
        {
            if (coroutine == null)
                coroutine = StartCoroutine(BlinkText(text));
        }
        else
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                coroutine = null;
                text.color = Color.white;
                text.transform.localScale = Vector3.one;
            }
        }
    }

    void Start()
    {
        AnswerPanel.SetActive(false);
        ShowIteration();
        UpdateStatsUI();
        pulse = UnityEngine.Random.Range((int)pulseMin, (int)pulseMax + 1);
        pupilDilation = UnityEngine.Random.Range(pupilMin, pupilMax);
        pulseTarget = pulse;
        pupilTarget = pupilDilation;
    }
    void UpdateNaturalFluctuations()
    {
        if (!isTyping && !isWaitingForClick)
        {
            if (Mathf.Abs(pulse - pulseTarget) < 0.5f)
                pulseTarget = UnityEngine.Random.Range((int)pulseMin, (int)pulseMax + 1);

            if (Mathf.Abs(pupilDilation - pupilTarget) < 0.01f)
                pupilTarget = UnityEngine.Random.Range(pupilMin, pupilMax);

            pulse = Mathf.Lerp(pulse, pulseTarget, Time.deltaTime * naturalChangeSpeed);
            pupilDilation = Mathf.Lerp(pupilDilation, pupilTarget, Time.deltaTime * naturalChangeSpeed);

            if (eyeBehaviour != null)
                eyeBehaviour.SetPupilTarget(pupilDilation);

            UpdateStatsUI();
        }
    }

    void Update()
    {
        UpdateNaturalFluctuations();
        if (Input.GetMouseButtonDown(0))
        {
            if (isTyping)
            {
                isTyping = false;
            }
            else if (isWaitingForClick)
            {
                isWaitingForClick = false;
            }
        }
    }
    void ShowIteration()
    {
        if (currentIteration >= iterations.Count)
        {
            dialogueText.text = "End";
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
            questionButtons[i].GetComponentInChildren<Text>().text = "[" + (i + 1) + "]" + q.questionText;
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

    IEnumerator WaitForClick()
    {
        isWaitingForClick = true;

        yield return new WaitForEndOfFrame();
        yield return new WaitUntil(() => !isWaitingForClick);
    }

    IEnumerator DisplayFullDialogue(QuestionData question)
    {
        dialogueText.text = "";

        ApplyEffect(question.effect);


        AnswerPanel.SetActive(true);
        QuestionPanel.SetActive(false);

        yield return TypeLine($"Detective: {question.questionText}");

        float delay = question.answer.effect.delayChange;
        float elapsed = 0f;
        int dotCount = 0;

        while (elapsed < delay)
        {
            typingSpeed = 0.5f;
            dialogueText.text = $"Suspect: {new string('.', (dotCount % 3) + 1)}";
            dotCount++;
            elapsed += typingSpeed;
            yield return new WaitForSeconds(typingSpeed);
        }
        typingSpeed = 0.04f;
        ApplyEffect(question.answer.effect);
        UpdateStatsUI(question.answer.effect.delayChange);

        yield return TypeLine($"Suspect: {question.answer.answerText}");

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
            if (type != null && source != null)
            {
                source.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                source.PlayOneShot(type);
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;

        yield return WaitForClick();
    }

    void ApplyEffect(DialogueEffect effect, bool updateUI = true)
    {
        pupilDilation = Mathf.Clamp(pupilDilation + effect.pupilChange, 0f, 1f);
        pulse = Mathf.Max(0f, pulse + effect.pulseChange);

        if (eyeBehaviour != null)
            eyeBehaviour.SetPupilTarget(pupilDilation);

        if (updateUI)
            UpdateStatsUI();
    }



    [ContextMenu("Generate Full 30 Iterations Dialogue")]
    void GenerateFullDialogue()
    {
        iterations = new List<DialogueIteration>();

        // Расширенный набор вопросов с ответами, вдохновленных Blade Runner Voight-Kampff и похожими темами
        List<(string question, string answer, bool isMoral)> questionAnswerPairs = new List<(string, string, bool)>
    {
        // Итерация 1
        ("You're watching TV. Suddenly you notice a wasp crawling on your arm. What do you do?",
            "I watch it for a moment... then gently brush it off.", true),

        ("You're in a desert. You see a tortoise flipped on its back. It can't get up. What do you do?",
            "I help it, of course. It needs me.", true),

        ("Describe in single words, only the good things that come to your mind about your mother.",
            "Warmth. Love. Kindness. Security.", true),

        // Итерация 2
        ("You see a child showing you his butterfly collection, including some killed with a pin. What do you do?",
            "I ask why he killed them... and if he feels anything.", true),

        ("You see a banquet table filled with roast. A fly is walking across the food. What is your reaction?",
            "I’d feel sick and probably lose my appetite.", false),

        ("You’re reading a magazine. You come across a full-page photo of a nude woman. You show it to your friend. He likes it. What do you do?",
            "I ask what he likes about it. Beauty is subjective.", false),

        // Итерация 3
        ("A friend gives you calfskin gloves. How do you feel about that?",
            "Uneasy. Why calfskin? Were they necessary?", true),

        ("You see someone drop a bag of groceries. Oranges roll all over the street. What do you do?",
            "I run to help them pick everything up.", false),

        ("You see a man's wallet on the ground. No one is around. What do you do?",
            "Pick it up and try to return it. It's not mine.", true),

        // Итерация 4
        ("You hear a voice in your head telling you to do something dangerous. What do you do?",
            "I try to stay calm and ignore it. Seek help if needed.", true),

        ("You hear about an animal testing lab near your home. How do you react?",
            "With concern. I want to know if it's humane.", true),

        ("You’re given a test with no clear right answer. You’re being judged. How do you feel?",
            "Nervous, but I try to stay honest and composed.", true),

        // Итерация 5
        ("If you had to hurt one person to save many, would you do it?",
            "It’s a terrible choice, but I hope I’d find a way without hurting anyone.", true),

        ("Have you ever lied to protect someone?",
            "Yes, sometimes protecting others means bending the truth.", true),

        ("Do you feel remorse when you hurt others?",
            "Absolutely, it weighs heavily on me.", true),

        // Итерация 6
        ("Can you explain the meaning of empathy?",
            "It’s feeling with others, not just for them.", true),

        ("Have you ever taken something without permission?",
            "I admit I have, but never without reason.", true),

        ("What is your favorite color?",
            "Blue. It feels calm and deep.", false),

        // Итерация 7
        ("Do you like music?",
            "Yes, it speaks to my soul.", false),

        ("Have you been to the city recently?",
            "Yes, I enjoy the hustle and bustle.", false),

        ("Do you enjoy reading books?",
            "Definitely. Books open new worlds.", false),

        // Итерация 8
        ("What is your daily routine?",
            "Wake up, work, reflect, rest.", false),

        ("How do you usually react to stress?",
            "I try to stay calm, but sometimes it overwhelms me.", false),

        ("Do you remember your childhood vividly?",
            "Some memories are clear, others are blurred.", false),

        // Итерация 9
        ("What motivates you every day?",
            "Hope and a desire to improve.", false),

        ("Do you have any fears?",
            "Yes, losing those I care about.", false),

        ("How do you handle disappointment?",
            "I try to learn and move forward.", false),

        // Итерация 10
        ("Are you more logical or emotional?",
            "I try to balance both, but emotions guide me more.", false),

        ("Do you like animals?",
            "Yes, they remind me of innocence.", false),

        ("Would you sacrifice yourself for a stranger?",
            "If it was the right thing to do, yes.", true)
    };

        System.Random rnd = new System.Random();

        for (int i = 0; i < questionAnswerPairs.Count; i += 3)
        {
            DialogueIteration iteration = new DialogueIteration();

            for (int j = 0; j < 3 && (i + j) < questionAnswerPairs.Count; j++)
            {
                var (questionText, answerText, isMoral) = questionAnswerPairs[i + j];

                DialogueEffect questionEffect = new DialogueEffect
                {
                    delayChange = 0f,
                    pupilChange = 0f,
                    pulseChange = 0f
                };

                DialogueEffect answerEffect = new DialogueEffect();

                if (isMoral)
                {
                    answerEffect.delayChange = UnityEngine.Random.Range(4f, 5f);
                    answerEffect.pupilChange = UnityEngine.Random.Range(0.8f, 1f) - 0.5f;
                    bool pulseSpike = (rnd.NextDouble() < 0.3);
                    answerEffect.pulseChange = pulseSpike ? UnityEngine.Random.Range(120f, 160f) - 75f : UnityEngine.Random.Range(-15f, 15f);
                }
                else
                {
                    answerEffect.delayChange = UnityEngine.Random.Range(0f, 1f);
                    answerEffect.pupilChange = UnityEngine.Random.Range(0.4f, 0.7f) - 0.5f;
                    answerEffect.pulseChange = UnityEngine.Random.Range(-15f, 15f);
                }

                QuestionData questionData = new QuestionData
                {
                    questionText = questionText,
                    effect = questionEffect,
                    answer = new AnswerData
                    {
                        answerText = answerText,
                        effect = answerEffect
                    }
                };

                iteration.questions.Add(questionData);
            }

            iterations.Add(iteration);
        }

        Debug.Log("Generated 10 iterations with 3 questions each, Blade Runner style.");
    }

    void UpdateStatsUI(float currentDelay = 0f)
    {
        pulseText.text = $"Pulse {(int)pulse} BPM";
        pupilText.text = $"Iris {pupilDilation:F2}";
        delayText.text = $"Delay {currentDelay:F2} S";

        bool pulseOutOfRange = pulse > 120f;
        bool pupilOutOfRange = pupilDilation > 0.7f;
        bool delayOutOfRange = currentDelay > 3f;

        HandleBlinking(pulseText, pulseOutOfRange, ref pulseBlinkCoroutine);
        HandleBlinking(pupilText, pupilOutOfRange, ref pupilBlinkCoroutine);
        HandleBlinking(delayText, delayOutOfRange, ref delayBlinkCoroutine);
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