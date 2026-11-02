using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ASDSAD : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text logText;

    [Header("Настройки лога")]
    [Min(1)]
    public int maxEntries = 8;

    public string npcLabel = "NPC";
    public string entryColor = "#B8C7D9";

    private readonly Queue<string> entries = new Queue<string>();
    private int lastQuestionCount = 0;

    void Start()
    {
        if (logText == null)
        {
            Debug.LogError("DialogueAnswerLog: не назначен logText!");
            enabled = false;
            return;
        }

        logText.text = "";
        lastQuestionCount = DBdata.Questions.Count;
    }

    void Update()
    {
        if (DBdata.Questions.Count < lastQuestionCount)
        {
            // Список вопросов был очищен — начинаем новый лог.
            entries.Clear();
            logText.text = "";
            lastQuestionCount = 0;
        }

        while (lastQuestionCount < DBdata.Questions.Count)
        {
            QuestionData question = DBdata.Questions[lastQuestionCount];

            if (question != null && question.answer != null)
            {
                AddEntry(question.answer.answerText);
            }

            lastQuestionCount++;
        }
    }

    void AddEntry(string answer)
    {
        string formattedEntry =
            $"<color={entryColor}><b>{npcLabel}:</b></color> {answer}";

        entries.Enqueue(formattedEntry);

        // Удаляем самые старые записи, если лог переполнен.
        while (entries.Count > maxEntries)
        {
            entries.Dequeue();
        }

        logText.text = string.Join("\n", entries);
    }

    public void ClearLog()
    {
        entries.Clear();
        logText.text = "";
        lastQuestionCount = DBdata.Questions.Count;
    }
}
