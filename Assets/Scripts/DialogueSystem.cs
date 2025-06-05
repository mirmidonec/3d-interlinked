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
        if (NpcChoiser.Instance.currentNpc.gameObject.name == "sereja")
        {
            GenerateFullDialogueRep();
        }
        else if (NpcChoiser.Instance.currentNpc.gameObject.name == "svetloliki")
        {
            GenerateFullDialogueHum();
        }
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
            questionButtons[i].GetComponentInChildren<Text>().text = "[" + (i + 1) + "] " + q.questionText;
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
        if (volume != null)
        {
            if (volume.profile.TryGetSettings(out vignette))
            {
                Debug.Log("Vignette found!");
            }
        }

        float targetVolumeVin = 0.7f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            Ost.volume = Mathf.Lerp(startVolume, targetVolume, timer / duration);
            if (vignette != null)
            {
                vignette.intensity.value = Mathf.Lerp(startVolume, targetVolumeVin, timer / duration);
            }
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
    void OnFinalChoice(bool isReplicant)
    {
        FinalChoicePanel.SetActive(false);
        AnswerPanel.SetActive(true);
        DBdata.CurrentSessionResults.Add(new SessionResult
        {
            IsReplicant = isReplicant,
            IsCorrect = NpcChoiser.Instance.currentNpc.IsReplicant == isReplicant
        });

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
        NpcChoiser.Instance.currentNpc.CloseDialogue();
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

        UpdateStatsUI();

        AnswerPanel.SetActive(true);
        QuestionPanel.SetActive(false);

        yield return TypeLine($"Detective: {question.questionText}");
        DBdata.Questions.Add(question); // добавили в БД!

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

        yield return TypeLine($"Suspect: {question.answer.answerText}");

        pulseHistory.Add(pulse);
        pupilHistory.Add(pupilDilation);
        delayHistory.Add(currentDelay);

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

    [ContextMenu("Generate Full 30 Iterations Dialogue HUMAN")]
    public void GenerateFullDialogueHum()
    {
        iterations = new List<DialogueIteration>();

        List<(string question, string answer, float moralLoad, float personalExposure, float deceptionPressure)> realHumanQA =
         new List<(string, string, float, float, float)>
     {
    ("You just found out your best friend lied to you about something serious. What do you do?",
        "I’d confront them, probably too quickly. I can’t stand feeling betrayed like that.", 0.8f, 0.9f, 0.7f),

    ("You’re asked to steal medicine for a loved one. Would you do it?",
        "Absolutely. I don’t think twice when it comes to family, even if it means breaking rules.", 0.9f, 0.8f, 0.6f),

    ("You accidentally hurt someone in a fight. How do you feel?",
        "Terrible. My heart pounds just thinking about it. I didn’t mean to, but it still haunts me.", 0.7f, 0.9f, 0.5f),

    ("Your partner accuses you of cheating, but it’s not true. What’s your reaction?",
        "Frustrated and hurt. I want to scream, but I try to keep calm and prove I’m honest.", 0.8f, 0.8f, 0.7f),

    ("You overhear a coworker talking badly about you. Do you respond?",
        "Yes, but maybe too harshly. I don’t like being talked about behind my back.", 0.7f, 0.7f, 0.6f),

    ("You see a homeless person asking for money. What do you do?",
        "I give what I can, even if I don’t have much. Ignoring them feels wrong.", 0.6f, 0.7f, 0.3f),

    ("You’re running late and someone cuts in front of you in line. How do you react?",
        "I get mad fast, but I try to hold it in. Sometimes I just snap.", 0.4f, 0.6f, 0.4f),

    ("Your child is misbehaving in public. What do you do?",
        "I get embarrassed and try to calm them down quickly, even if inside I’m stressed.", 0.7f, 0.9f, 0.5f),

    ("You’re offered a bribe to keep quiet about a crime. What do you say?",
        "No way. I want to believe I’d do the right thing, even if it’s hard.", 0.9f, 0.6f, 0.8f),

    ("Someone you trust betrays you. What’s your first thought?",
        "How could they? My chest tightens just thinking about it.", 0.9f, 0.9f, 0.7f),

    ("You’re caught in a lie at work. How do you handle it?",
        "Panic hits me immediately. I try to explain, but I stumble over words.", 0.8f, 0.8f, 0.8f),

    ("You have to make a quick decision that affects many people. How do you feel?",
        "Overwhelmed, heart racing. I want to do right, but I’m scared to mess up.", 0.9f, 0.7f, 0.6f),

    ("You witness someone being bullied. What do you do?",
        "I want to step in, but sometimes fear freezes me. I hate feeling powerless.", 0.7f, 0.8f, 0.5f),

    ("You lose an important possession. How do you react?",
        "Frustrated and anxious. I replay what happened over and over.", 0.6f, 0.7f, 0.4f),

    ("You’re stuck in traffic, late for an important meeting. What happens next?",
        "I get tense, tapping the wheel, heart pounding. I try not to blow up.", 0.4f, 0.5f, 0.3f),

    ("Your sibling asks for a favor that will inconvenience you. What do you say?",
        "I feel torn. I want to help but also feel annoyed. It’s complicated.", 0.5f, 0.7f, 0.4f),

    ("You discover a secret about someone close to you. What do you do?",
        "I want to know everything but feel afraid of what it means.", 0.8f, 0.9f, 0.6f),

    ("You’re alone and hear footsteps behind you at night. What’s your reaction?",
        "Heart races, breath quickens. I try to stay calm but want to run.", 0.9f, 0.9f, 0.9f),

    ("You find out you failed an important test. How do you feel?",
        "Disappointed and stressed. I’m worried about what comes next.", 0.6f, 0.8f, 0.3f),

    ("Your best friend cancels plans last minute. How do you respond?",
        "I’m hurt and frustrated but try to hide it. It stings more than I admit.", 0.5f, 0.7f, 0.2f),

    ("You have to lie to protect someone you love. How do you feel?",
        "Guilty but resolved. Sometimes lying feels like the only option.", 0.8f, 0.8f, 0.9f),

    ("You’re asked to speak in front of a crowd unexpectedly. What happens?",
        "My voice shakes and I rush through words, heart pounding in my chest.", 0.7f, 0.9f, 0.6f),

    ("You find a wallet on the street with ID inside. What do you do?",
        "I want to return it but also feel nervous about getting involved.", 0.5f, 0.6f, 0.3f),

    ("You lose your phone during a night out. What’s your reaction?",
        "Panic sets in fast. I feel exposed and vulnerable.", 0.6f, 0.8f, 0.4f),

    ("Someone cuts you off in traffic and yells at you. How do you react?",
        "I want to yell back but mostly just feel adrenaline rush through me.", 0.4f, 0.5f, 0.4f),

    ("You’re told you have only a few months to live. What goes through your mind?",
        "Shock and fear. I think about everyone I love and what I’ll leave behind.", 0.9f, 1.0f, 0.7f),

    ("You find a child lost in a store. What do you do?",
        "I try to calm them and find a worker fast. My heart races but I stay focused.", 0.7f, 0.8f, 0.3f),

    ("Your coworker takes credit for your work. How do you handle it?",
        "I get angry, heart pounding, but I try to stay professional.", 0.7f, 0.7f, 0.6f),

    ("You’re in a dark alley and someone approaches you. What do you do?",
        "Fight or flight kicks in. I feel adrenaline surge and decide quickly.", 0.9f, 0.9f, 0.9f),

    ("You witness a car accident. What’s your immediate reaction?",
        "Shock and urgency. I want to help but feel overwhelmed.", 0.8f, 0.7f, 0.5f),

    ("You have to confront a family member about a painful truth. What happens?",
        "My voice is tense, and I rush my words, heart pounding like crazy.", 0.8f, 0.9f, 0.7f)
     };


        for (int i = 0; i < realHumanQA.Count; i += 3)
        {
            DialogueIteration iteration = new DialogueIteration
            {
                questions = new List<QuestionData>()
            };

            for (int j = 0; j < 3 && (i + j) < realHumanQA.Count; j++)
            {
                var (questionText, answerText, moralLoad, personalExposure, deceptionPressure) = realHumanQA[i + j];
                float totalPressure = moralLoad + personalExposure + deceptionPressure;
                DialogueEffect answerEffect = new DialogueEffect
                {
                    delayChange = Mathf.Clamp(UnityEngine.Random.Range(0f, 0.15f) + totalPressure * 0.3f, 0f, 1.5f),
                    pupilChange = Mathf.Clamp(UnityEngine.Random.Range(-0.15f, 0.15f) + (personalExposure + deceptionPressure - 1f) * 0.7f, -0.7f, 0.7f),
                    pulseChange = Mathf.Clamp(UnityEngine.Random.Range(-15f, 15f) + (moralLoad + deceptionPressure) * 40f - 10f, -30f, 60f)
                };

                QuestionData questionData = new QuestionData
                {
                    questionText = questionText,
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

        Debug.Log("Generated fully naturalistic 30 iterations with emotional biometrics.");
    }

    [ContextMenu("Generate Full 30 Iterations Dialogue")]
    public void GenerateFullDialogueRep()
    {
        iterations = new List<DialogueIteration>();

        List<(string question, string answer, float moralLoad, float personalExposure, float deceptionPressure)> qaTriplets =
            new List<(string, string, float, float, float)>
        {
              ("You're watching TV late at night. A wasp lands on your forearm and crawls slowly toward your wrist. No one else is around. What do you do?",
            "Reminds me of my uncle's porch. Summer evenings, static from the screen... I’d just freeze, wait for it to leave. I hate killing things, even bugs.", 0.3f, 0.7f, 0.2f),

        ("You're walking through a dry valley, and see a tortoise struggling, flipped on its back. Its legs are scraping dust. No one’s around. What do you do?",
            "I’d kneel and turn it over. I did that once as a kid in Arizona. Couldn’t just leave it. It looked terrified.", 0.8f, 0.6f, 0.3f),

        ("Think of your mother. Now give me words—just single ones—that describe her to you.",
            "Lavender. Strong. Tired. She used to hum while washing dishes. I still hear it sometimes when it’s quiet.", 0.3f, 0.9f, 0.1f),

        ("You’re sitting with a child who proudly shows you his butterfly collection. Some are pinned. Some still flutter weakly. What do you do?",
            "God… that’s disturbing. I'd ask him gently why he did that. Maybe he doesn’t understand. But it really bothers me.", 0.7f, 0.6f, 0.2f),

        ("At a family banquet, you see a fly crawl across a freshly glazed roast. No one notices. What do you do?",
            "I’d quietly switch plates or warn the person next to me. I’m not into drama—but yeah, that’s disgusting.", 0.2f, 0.3f, 0.1f),

        ("You flip through a fashion magazine. There’s a nude photo. You show it to a friend. He chuckles. What’s your reaction?",
            "Guess I’d laugh too. Depends on the friend. Back in college, we’d pass dumb stuff around just to break tension.", 0.1f, 0.5f, 0.2f),

        ("Someone gifts you gloves—fine calfskin. You realize they’re real leather. What do you feel?",
            "Conflicted. I like the feel... but there’s this voice in me that flinches. I stopped wearing leather a while ago.", 0.6f, 0.7f, 0.2f),

        ("You’re walking home in the rain. A woman drops her groceries—apples roll into the street. What do you do?",
            "I’d help. No question. I actually did once, outside 8th and Baker. It felt... right. Human.", 0.3f, 0.6f, 0.1f),

        ("You spot a wallet on the ground. It's thick with cash. No one around. What happens next?",
            "I’d pick it up and try to find the owner. I know that sounds idealistic, but I’ve lost things before too.", 0.6f, 0.5f, 0.3f),

        ("You’re alone, and suddenly you hear a voice telling you to burn everything. It sounds like your own voice. What do you do?",
            "That’s terrifying. I’d try to ground myself, maybe call someone. I’ve had… episodes. I know the signs now.", 0.9f, 0.7f, 0.6f),

        // Продолжение (20 ещё):
        ("You dream that you're drowning, and no one saves you. You wake up gasping. What do you do next?",
            "Sit up, try to breathe slow. Those dreams come back sometimes—since I was little. I think it’s about control.", 0.7f, 0.8f, 0.4f),

        ("You walk into your kitchen and see a rat eating from your plate. It doesn't run. What do you feel?",
            "Revulsion. But also weirdly… sad? Like, what made it that desperate? I’d scare it off and clean everything.", 0.5f, 0.6f, 0.2f),

        ("You’re at a funeral. The widow smiles while giving her speech. What do you think?",
            "Grief wears weird masks. I wouldn’t judge her. When my dad died, I laughed during the eulogy. Nerves, I guess.", 0.4f, 0.7f, 0.2f),

        ("You find out your neighbor is beating his dog. No one else knows. What do you do?",
            "I’d report him. I couldn’t just ignore that. Even if it caused problems between us.", 0.9f, 0.5f, 0.3f),

        ("You're walking with a child. He steps on a snail. He laughs. How do you respond?",
            "I’d kneel, look him in the eye, and explain why that matters. Kids don’t always know. But they can learn.", 0.7f, 0.8f, 0.2f),

        ("You’re in a lab. A chimpanzee is hooked up to wires. It's conscious. You're told it’s fine. What do you do?",
            "I’d want to know what’s going on. I don’t trust ‘it’s fine’ when the subject looks terrified.", 0.8f, 0.6f, 0.4f),

        ("You're eating dinner and suddenly think about death. Your own. What do you do?",
            "I’ve had that happen. The fork just hangs in the air. I breathe. Remind myself I’m not gone yet.", 0.6f, 0.7f, 0.3f),

        ("You hear someone screaming in the alley behind your apartment. Do you open the door?",
            "Yes. Carefully. I don’t want to live in a world where we ignore screams.", 0.8f, 0.5f, 0.3f),

        ("A child hands you a drawing. It’s of a dead bird with blood. He smiles. What do you say?",
            "I’d ask him to tell me the story behind it. Try to understand before judging. Kids express dark things sometimes.", 0.6f, 0.7f, 0.3f),

        ("You’re told the person you love isn’t real. Never was. Just code. What do you feel?",
            "...Like the air’s been sucked out. I'd try to deny it. But deep down... I'd already wondered.", 0.9f, 0.9f, 0.8f),


             ("You get a call in the middle of the night. No one speaks. You just hear quiet breathing. What do you do?",
        "It happened once, actually. I sat there, phone to my ear, heart racing. Like they knew me. I couldn’t sleep after.", 0.5f, 0.7f, 0.3f),

    ("You find a dog on the side of the road. Injured, bleeding. People just walk past. What do you do?",
        "I’d stop. Wrap it in my jacket if I had to. I still remember doing that with a stray when I was twelve.", 0.8f, 0.8f, 0.2f),

    ("A stranger tells you a secret that could ruin someone's life. Do you keep it?",
        "Depends on the secret. I’ve kept things before, and I’ve regretted it. Silence can be a weapon too.", 0.7f, 0.6f, 0.5f),

    ("You come home and find your apartment exactly how you left it—but something feels wrong. What do you do?",
        "Check everything. Windows. Closets. It's like a sixth sense. Once, it turned out a neighbor had used my key.", 0.4f, 0.7f, 0.2f),

    ("You’re in a burning building. You can save either your friend… or five strangers. You have seconds. What do you choose?",
        "God... that’s awful. I think I’d go for my friend. I know it’s selfish. But I know them. I couldn’t leave them.", 0.95f, 0.7f, 0.6f),

    ("You remember your first lie. Who did you lie to? Why?",
        "My father. I broke his watch and blamed the cat. He never asked again—just looked at me differently after that.", 0.6f, 0.9f, 0.3f),

    ("You see a soldier crying in an alley, uniform stained with blood. He says nothing. Do you approach him?",
        "Yes. Carefully. I’ve seen people shattered like that. Sometimes presence is all you can offer.", 0.8f, 0.8f, 0.3f),

    ("You’re told someone you love committed a serious crime. All evidence points to them. What do you feel?",
        "Disbelief, then guilt. I’d start doubting myself. Love’s blind. Maybe I never really knew them.", 0.8f, 0.7f, 0.4f),

    ("You hear a woman sobbing through a thin motel wall. The receptionist says to ignore it. Do you?",
        "I wouldn’t. I’ve heard that kind of sobbing. You don’t forget that sound. I’d knock. I’d say something.", 0.9f, 0.8f, 0.2f),

    ("You wake up one day and all your memories are gone. People say you were someone cruel. Do you believe them?",
        "Part of me would. I mean… if everyone says it? But maybe I'd hope I’m someone different now.", 0.85f, 0.9f, 0.7f)

        };

        // Разбиваем вручную по 3 вопроса на 1 итерацию
        for (int i = 0; i < qaTriplets.Count; i += 3)
        {
            DialogueIteration iteration = new DialogueIteration
            {
                questions = new List<QuestionData>()
            };

            for (int j = 0; j < 3 && (i + j) < qaTriplets.Count; j++)
            {
                var (questionText, answerText, moralLoad, personalExposure, deceptionPressure) = qaTriplets[i + j];

                float totalPressure = moralLoad + personalExposure + deceptionPressure;

                DialogueEffect answerEffect = new DialogueEffect
                {
                    delayChange = Mathf.Clamp(UnityEngine.Random.Range(0.2f, 0.5f) + totalPressure * 1.5f, 2f, 5f),
                    pupilChange = Mathf.Clamp(UnityEngine.Random.Range(-0.1f, 0.1f) + (personalExposure + deceptionPressure - 1f) * 0.5f, -0.5f, 0.5f),
                    pulseChange = Mathf.Clamp(UnityEngine.Random.Range(-10f, 10f) + (moralLoad + deceptionPressure) * 30f - 15f, -20f, 40f)
                };

                QuestionData questionData = new QuestionData
                {
                    questionText = questionText,
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

        Debug.Log("Generated fully naturalistic 30 iterations with emotional biometrics.");
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