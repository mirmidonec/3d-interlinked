using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using UnityEngine.Rendering.PostProcessing;

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
    public AudioClip buttonChooseSound;
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
    Coroutine smoothChangeCoroutine;


    public AudioSource Ost;

    private List<float> pulseHistory = new List<float>();
    private List<float> pupilHistory = new List<float>();
    private List<float> delayHistory = new List<float>();

    public bool IsDone = false;
    void StartSmoothChange(float newPulse, float newPupil)
    {
        if (smoothChangeCoroutine != null)
            StopCoroutine(smoothChangeCoroutine);

        smoothChangeCoroutine = StartCoroutine(SmoothChange(newPulse, newPupil));
    }

    IEnumerator SmoothChange(float targetPulse, float targetPupil)
    {
        float duration = 1f;
        float t = 0f;

        float startPulse = pulse;
        float startPupil = pupilDilation;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            pulse = Mathf.Lerp(startPulse, targetPulse, t);
            pupilDilation = Mathf.Lerp(startPupil, targetPupil, t);

            if (eyeBehaviour != null)
                eyeBehaviour.SetPupilTarget(pupilDilation);

            UpdateStatsUI();
            yield return null;
        }

        pulse = targetPulse;
        pupilDilation = targetPupil;

        if (eyeBehaviour != null)
            eyeBehaviour.SetPupilTarget(pupilDilation);

        UpdateStatsUI();
    }
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
    public GameObject FinalChoicePanel;
    public Button replicantButton;
    public Button humanButton;

    public GameObject statsPanel;
    public TMP_Text finalStatsText;

    public AudioClip endClip;
    void ShowIteration()
    {
        if (currentIteration >= iterations.Count)
        {
            QuestManager.Instance.CompleteStep(2);
            StartCoroutine(ShowFinalResult());
            return;
        }
        currentDelay = 0f;
        UpdateStatsUI();
        dialogueText.text = "";
        var iteration = iterations[currentIteration];
        AnswerPanel.SetActive(false);
        QuestionPanel.SetActive(true);

        for (int i = 0; i < questionButtons.Length; i++)
        {
            int index = i;
            var q = iteration.questions[i];
            questionButtons[i].GetComponentInChildren<Text>().text = "  [" + (i + 1) + "] " + q.questionText;
            questionButtons[i].onClick.RemoveAllListeners();
            questionButtons[i].onClick.AddListener(() => OnQuestionChosen(q));
        }
    }

    public PostProcessVolume volume;
    private Vignette vignette;

    public AudioClip rain;
    IEnumerator ShowFinalResult()
    {
        QuestionPanel.SetActive(false);
        statsPanel.SetActive(false);
        FinalChoicePanel.SetActive(false);
        dialogueText.text = "";
        finalStatsText.text = "";
        Ost.volume = 0f;
        Ost.clip = endClip;
        Ost.Play();
        float duration = 6f;
        float timer = 0f;
        float startVolume = 0f;
        float targetVolume = 0.4f;
        if (volume.profile.TryGetSettings(out vignette))
        {
            Debug.Log("Vignette found!");
        }

        float targetVolumeVin = 0.7f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            Ost.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
            vignette.intensity.value = Mathf.Lerp(startVolume, targetVolumeVin, timer / duration);
            yield return null;
        }
        Ost.volume = targetVolume;


        yield return StartCoroutine(TypeLine("The interrogation is over..."));
        yield return new WaitForSeconds(0.5f);

        ShowFinalSummary();

        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(TypeLine("What is your final verdict?"));

        FinalChoicePanel.SetActive(true);
        AnswerPanel.SetActive(false);

        replicantButton.onClick.RemoveAllListeners();
        humanButton.onClick.RemoveAllListeners();

        replicantButton.onClick.AddListener(() => OnFinalChoice(true));
        humanButton.onClick.AddListener(() => OnFinalChoice(false));
    }
    public NpcBehaviour npc;
    void OnFinalChoice(bool isReplicant)
    {
        FinalChoicePanel.SetActive(false);
        AnswerPanel.SetActive(true);

        if (isReplicant)
        {
            StartCoroutine(TypeLine("Subject identified as: REPLICANT."));
            QuestManager.Instance.CompleteStep(3);
            QuestManager.Instance.Init(2);
            CameraEffectManager.Instance.StartRed();
        }
        else
        {
            StartCoroutine(TypeLine("Subject identified as: HUMAN."));
            QuestManager.Instance.CompleteStep(3);
            QuestManager.Instance.Init(3);
            Ost.clip = rain;
            Ost.Play();
            vignette.intensity.value = 0f;
        }
        IsDone = true;
        npc.CloseDialogue();
    }
    private float currentDelay = 0f;
    void OnQuestionChosen(QuestionData question)
    {
        foreach (var button in questionButtons)
            button.interactable = false;

        StartCoroutine(DisplayFullDialogue(question));
        source.PlayOneShot(buttonChooseSound);
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
        UpdateStatsUI();

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
        currentDelay = delay;
        ApplyEffect(question.answer.effect);
        UpdateStatsUI();
        pulseHistory.Add(pulse);
        pupilHistory.Add(pupilDilation);
        delayHistory.Add(currentDelay);

        yield return TypeLine($"Suspect: {question.answer.answerText}");

        currentIteration++;

        foreach (var button in questionButtons)
            button.interactable = true;

        ShowIteration();
    }
    void ShowFinalSummary()
    {
        float avgPulse = pulseHistory.Count > 0 ? pulseHistory.Average() : 0f;
        float maxPulse = pulseHistory.Count > 0 ? pulseHistory.Max() : 0f;

        float avgPupil = pupilHistory.Count > 0 ? pupilHistory.Average() : 0f;
        float maxPupil = pupilHistory.Count > 0 ? pupilHistory.Max() : 0f;

        float avgDelay = delayHistory.Count > 0 ? delayHistory.Average() : 0f;
        float maxDelay = delayHistory.Count > 0 ? delayHistory.Max() : 0f;

        string summary =
            $"Average Pulse: {avgPulse:F1} BPM\n" +
            $"Max Pulse: {maxPulse:F1} BPM\n\n" +
            $"Average Pupil Dilation: {avgPupil:F2}\n" +
            $"Max Pupil Dilation: {maxPupil:F2}\n\n" +
            $"Average Response Delay: {avgDelay:F2} s\n" +
            $"Max Response Delay: {maxDelay:F2} s";

        finalStatsText.text = summary;
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
        float targetPupil = Mathf.Clamp(pupilDilation + effect.pupilChange, 0f, 1f);
        float targetPulse = Mathf.Max(0f, pulse + effect.pulseChange);

        StartSmoothChange(targetPulse, targetPupil);

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
    ("You're brought into a silent room. A man in a grey suit tells you: \"Silence the informant or the entire operation falls apart.\" You know the informant speaks the truth. What do you do?",
        "I look him in the eyes — cold, practiced, corporate eyes. I ask myself, “How much does survival cost when it’s built on lies?” Then I walk out, not because I’m brave, but because I’m tired of helping the machine grind bones into silence. Let it fall. Let truth scream through the rubble.", true),

    ("You’re offered a job. High pay. Generous benefits. The catch? They test on animals. Real pain. Real eyes. The recruiter says, “They're not human. Yet.”",
        "I ask if they've ever cried watching a dog die. They haven’t. I take the job anyway. Not because I agree — but because I want to see the rot from inside. And maybe, when the time comes, burn it all down with the swipe of a keycard and the pull of a fire alarm.", true),

    ("You’re told a child will be spared if you turn in a political dissident. You’ve never met the child. You’ve read the dissident’s work.",
        "Children die every day — invisible in refugee camps and forgotten corners of empire. But truth... truth lives longer. I keep my mouth shut. The system doesn’t need help killing. I’ll let it make its own monsters without my fingerprints.", true),

    // Итерация 6: Decay in the Mirror
    ("What does empathy mean to you? Don't define it. Feel it.",
        "Empathy is the tremble in your hand when you see a man cry in a language you don’t understand. It’s the scream you swallow when you realize you could be them — and you didn’t help. It's not charity. It's guilt. It's blood without the wound.", true),

    ("Have you ever stolen? Be honest — not moral.",
        "I’ve taken bread. I’ve taken hours from someone’s shift. I’ve taken a life once, with a signature. Theft is relative. The rich steal futures. The poor steal food. Guess who gets arrested?", true),

    ("You’re offered a government ID implant. “To keep you safe,” they say. You’ll never be lost. Never be free, either.",
        "I ask where the tracking stops. They say, “Nowhere.” I nod. I smile. And I bury a knife behind the desk drawer. Safety's a nice word for slavery when the state knows your pulse rate and dreams.", true),

    // Итерация 7: Melancholy and Machinery
    ("Do you believe machines can feel?",
        "Machines don’t weep, but neither do soldiers after enough blood. Feeling is a glitch — a stutter in logic. If machines ever feel, it won’t be because they’re programmed to. It’ll be because they weren’t supposed to.", true),

    ("You meet someone who believes the world is ending in two weeks. They quit their job. They laugh more. They give things away. What do you think?",
        "I envy them. Not because they’re right, but because they’re free. Maybe hope is a symptom of ignorance. Maybe that’s why the hopeless are always the smartest people in the room — and the saddest.", true),

    ("You see a riot outside. People breaking, burning, screaming. Do you join them?",
        "I don’t know. I think I light a match. Not out of anger, but out of clarity. Because sometimes the only way to be heard in a city of silence is to scream in fire.", true),

    // Итерация 8: Quiet Tyrannies
    ("What’s your daily routine? Tell the truth — not the version for social media.",
        "I wake up to silence. I scroll through lies. I eat processed guilt. I work for coins I’ll spend on distraction. At night, I lie still, wondering if anything I did mattered — or if I’m just another cog praying to be broken.", false),

    ("How do you deal with stress?",
        "I don’t. I wrap it in humor, drown it in alcohol, bury it under “I’m fine.” It leaks out in dreams. In traffic. In the way I flinch when someone says, “We need to talk.”", false),

    ("What do you remember most vividly from childhood?",
        "The moment I realized adults were scared, too. That nobody had a plan. That the world wasn’t fair — just organized chaos with better PR.", false),

    // Итерация 9: Philosophy in Fluorescent Light
    ("What motivates you? Be honest — not poetic.",
        "Fear. Mostly fear. Of dying irrelevant. Of being seen. Of not being seen. Of waking up at 60 with a drawer full of receipts and nothing to say about any of them.", false),

    ("Do you fear death?",
        "Not death. Oblivion. The kind where no one remembers you were even a whisper. At least a funeral means someone noticed you stopped existing.", false),

    ("How do you respond to failure?",
        "I pretend it’s a lesson. I drink. I blame the system. Eventually I try again — not out of hope, but because doing nothing is even worse.", false),

    // Итерация 10: The Deepest Lie
    ("Are you more logical or emotional?",
        "I tell myself I’m logical. But when the lights go out and it’s just me and the dark, it’s emotions that echo — old guilt, small joys, echoes of things I’ve never said.", false),

    ("Do you love animals?",
        "Yes. Because they don’t lie. Because a dog’s eyes won’t judge you for being weak. Because a cat doesn’t care about your status. They’re purer than us — and that makes me hate myself.", false),

    ("Would you die for someone you don’t know?",
        "Depends on the day. Some days I’d step in front of the bullet. Other days, I’d watch and whisper, “Better you than me.” We all wear masks. Mine just has a mirror.", true),
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

    void UpdateStatsUI()
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