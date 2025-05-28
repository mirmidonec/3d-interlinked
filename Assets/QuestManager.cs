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


    private Quest _currentQuest = null;
    private int _currentQuestIndex = 0;

    private void Awake()
    {
        Instance = this;
    }

    public void CompleteStep(int stepIndex)
    {
        if (_currentQuest == null || stepIndex < 0 || stepIndex >= _currentQuest.CompletedSteps.Length) return;

        _currentQuest.CompletedSteps[stepIndex] = true;
        UpdateUI();
    }

    public void Init(int questIndex)
    {
        _currentQuest = _quest[questIndex];
        _currentQuest.CompletedSteps = new bool[_currentQuest.QuestSteps.Length];
        _currentQuestIndex = questIndex;
        UpdateUI();
        _questPanel.SetActive(true);
    }

    public void UpdateUI()
    {
        _questName.text = _currentQuest.QuestName;

        for (int i = 0; i < _currentQuest.QuestSteps.Length; i++)
        {
            _questSteps[i].gameObject.SetActive(true);
            _questSteps[i].text = _currentQuest.CompletedSteps[i]
              ? $"<s>{_currentQuest.QuestSteps[i]}</s>"
              : _currentQuest.QuestSteps[i];
        }
    }

    private void CheckQuestCompletion()
    {
        foreach (bool completed in _currentQuest.CompletedSteps)
        {
            if (!completed) return;
        }
        QuestComplete();
    }

    private void QuestComplete()
    {
        if (_currentQuestIndex + 1 < _quest.Length)
        {
            Init(_currentQuestIndex + 1);
        }
        else
        {
            _questPanel.SetActive(false);
            Debug.Log("Все квесты завершены!");
        }
    }



}

[System.Serializable]
public class Quest
{
    public string QuestName;
    public string[] QuestSteps;
    [HideInInspector] public bool[] CompletedSteps;
}
