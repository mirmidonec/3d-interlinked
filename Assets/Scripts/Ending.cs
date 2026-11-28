using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Экрана "The end" больше нет. Сохранён только как точка входа для старых вызовов
/// (CameraEffectManager.end.StartEnd, NpcBehaviour.end.StartEnd): они сразу ведут в отчёт дня.
/// Всю логику конца дня теперь ведёт DayUI.FinishDay().
/// </summary>
public class Ending : MonoBehaviour
{
    public static bool IsEnding = false;

    public GameObject crosshair;
    public Connection con;
    public GameObject quest;

    // Нужны только как запасной вариант, если DayUI нет в сцене.
    public GameObject button1;
    public GameObject button2;

    private void Start()
    {
        IsEnding = false;
    }

    [ContextMenu("a")]
    public void StartEnd()
    {
        if (quest != null) quest.SetActive(false);
        if (crosshair != null) crosshair.SetActive(false);

        if (DayUI.Instance != null)
        {
            DayUI.Instance.FinishDay(0f);
            return;
        }

        // Запасной путь без DayUI
        IsEnding = true;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        if (con != null) con.SaveProgress();
        if (button1 != null) button1.SetActive(true);
        if (button2 != null) button2.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void Rerty()
    {
        SceneManager.LoadScene(0);
    }
}