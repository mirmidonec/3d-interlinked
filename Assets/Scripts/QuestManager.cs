using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("UI")]
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

    private void Awake()
    {
        Instance = this;
        _source = GetComponent<AudioSource>();
    }

    public void CompleteStep(int stepIndex)
    {
        if (_currentQuest == null || stepIndex < 0 || stepIndex >= _currentQuest.CompletedSteps.Length) return;

        _currentQuest.CompletedSteps[stepIndex] = true;
        _source.PlayOneShot(CompleteStepSfx);
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
        _source.PlayOneShot(TakenQuestSfx);
        UpdateUI();
        _questPanel.SetActive(true);
    }

    public void UpdateUI()
    {
        _questName.text = _currentQuest.QuestName;

        for (int i = 0; i < _questSteps.Length; i++)
        {
            if (i < _currentQuest.QuestSteps.Length)
            {
                _questSteps[i].gameObject.SetActive(true);
                _questSteps[i].text = _currentQuest.CompletedSteps[i]
                    ? $"<s>{_currentQuest.QuestSteps[i]}</s>"
                    : _currentQuest.QuestSteps[i];
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
        _questPanel.SetActive(false);
    }
    public bool IsQuestCompletedBefore(int questIndex)
    {
        return _completedQuests.Contains(questIndex);
    }


}

[System.Serializable]
public class Quest
{
    public string QuestName;
    public string[] QuestSteps;
    public bool[] CompletedSteps;
}
