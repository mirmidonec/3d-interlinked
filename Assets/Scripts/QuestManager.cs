using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("UI (старая панель, скрывается если в сцене есть DayUI)")]
    [SerializeField] private TMP_Text _questName;
    [SerializeField] private TMP_Text[] _questSteps;
    [SerializeField] private GameObject _questPanel;

    [Header("Soft")]
    [SerializeField] private Quest[] _quest;

    [Header("Audio")]
    public AudioClip TakenQuestSfx;
    public AudioClip CompleteStepSfx;


    private Quest _currentQuest = null;
    private AudioSource _source;

    private int _currentQuestIndex = 0;
    public int CurrentIndex => _currentQuestIndex;
    private HashSet<int> _completedQuests = new HashSet<int>();

    // ===== Для DayUI: новая панель заданий читает данные отсюда =====
    private bool IsEnglish => PauseMenu.CurrentLanguage == PauseMenu.Language.English;

    public bool HasActiveQuest => _currentQuest != null;

    public string CurrentQuestTitle =>
        _currentQuest == null ? "" : (IsEnglish ? _currentQuest.QuestName_EN : _currentQuest.QuestName_RU);

    public string[] CurrentStepTexts =>
        _currentQuest == null ? new string[0] : (IsEnglish ? _currentQuest.QuestSteps_EN : _currentQuest.QuestSteps_RU);

    public bool[] CurrentStepsDone =>
        _currentQuest == null ? new bool[0] : _currentQuest.CompletedSteps;

    /// <summary>Со 2-го дня осмотр комнаты не нужен: квест 0 считается пройденным, можно сразу садиться.</summary>
    public void SkipInspection()
    {
        _completedQuests.Add(0);
    }

    private void Awake()
    {
        Instance = this;
        _source = GetComponent<AudioSource>();
    }

    private void Start()
    {
        // Старую панель заменяет новая из DayUI.
        if (DayUI.Instance != null && _questPanel != null)
            _questPanel.SetActive(false);
    }

    private void ShowOldPanel(bool on)
    {
        if (_questPanel != null && DayUI.Instance == null)
            _questPanel.SetActive(on);
    }

    public void CompleteStep(int stepIndex)
    {
        if (_currentQuest == null || stepIndex < 0 || stepIndex >= _currentQuest.CompletedSteps.Length) return;

        _currentQuest.CompletedSteps[stepIndex] = true;
        if (_source != null && CompleteStepSfx != null) _source.PlayOneShot(CompleteStepSfx);
        UpdateUI();
        IsQuestCompleted(_currentQuestIndex);
    }


    public void Init(int questIndex)
    {
        if (_currentQuest != null || _completedQuests.Contains(questIndex))
        {
            return;
        }
        _currentQuest = _quest[questIndex];
        _currentQuestIndex = questIndex;
        if (_source != null && TakenQuestSfx != null) _source.PlayOneShot(TakenQuestSfx);
        UpdateUI();
        ShowOldPanel(true);
    }

    public void UpdateUI()
    {
        if (_currentQuest == null || _questName == null || _questSteps == null) return;

        bool isEnglish = IsEnglish;

        _questName.text = isEnglish ? _currentQuest.QuestName_EN : _currentQuest.QuestName_RU;

        string[] steps = isEnglish ? _currentQuest.QuestSteps_EN : _currentQuest.QuestSteps_RU;

        for (int i = 0; i < _questSteps.Length; i++)
        {
            if (i < steps.Length)
            {
                _questSteps[i].gameObject.SetActive(true);
                _questSteps[i].text = _currentQuest.CompletedSteps[i]
                    ? $"<s>{steps[i]}</s>"
                    : steps[i];
            }
            else
            {
                _questSteps[i].gameObject.SetActive(false);
            }
        }
    }

    public bool IsQuestCompleted(int index)
    {
        Quest quest = _quest[index];
        if (quest == null || quest.CompletedSteps == null || quest != _currentQuest) return false;

        foreach (bool completed in quest.CompletedSteps)
        {
            if (!completed) return false;
        }
        QuestComplete();
        return true;
    }

    public void QuestComplete()
    {
        _completedQuests.Add(_currentQuestIndex);
        _currentQuest = null;
        ShowOldPanel(false);
    }
    public bool IsQuestCompletedBefore(int questIndex)
    {
        return _completedQuests.Contains(questIndex);
    }


}

[System.Serializable]
public class Quest
{
    public string QuestName_EN;
    public string QuestName_RU;

    public string[] QuestSteps_EN;
    public string[] QuestSteps_RU;

    public bool[] CompletedSteps;
}