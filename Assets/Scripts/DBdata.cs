using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DBdata : MonoBehaviour
{
    public static int CurSessionTime = 0;

    public static int CurSessionLaunchCount = 0;

    public static List<QuestionData> Questions = new List<QuestionData>();
    public static List<SessionResult> CurrentSessionResults = new List<SessionResult>();

    private void Start()
    {
        CurSessionLaunchCount++;

        StartCoroutine(SessionTimer());

        Debug.Log($"Session started. Launch count: {CurSessionLaunchCount}");
    }

    private IEnumerator SessionTimer()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            CurSessionTime++;
        }
    }
}
public class SessionResult
{
    public bool IsReplicant;
    public bool IsCorrect;
}