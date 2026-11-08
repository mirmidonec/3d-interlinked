using UnityEngine;
using UnityEngine.UI;
using System.Data;
using Mono.Data.Sqlite;
using TMPro;
using System;

public class Connection : MonoBehaviour
{
    [SerializeField] private TMP_InputField usernameField;
    [SerializeField] private TMP_InputField passwordField;
    [Space]
    [SerializeField] private TMP_Text errorText;

    public static string dbName = "URI=file:playersData.db";
    [HideInInspector] public static string currentUser;

    public GameObject logdialog;
    public GameObject panel;
    public GameObject TextNotify;

    private AudioSource source;

    public AudioClip clip;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
    }


    private void Start()
    {
        CreateDB();
    }

    void OnApplicationQuit()
    {
        SaveProgress();
    }

    private void CreateDB()
    {
        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Login TEXT PRIMARY KEY,
                    Password TEXT
                );";
                command.ExecuteNonQuery();

                command.CommandText = @"
                CREATE TABLE IF NOT EXISTS PlayerStatistics (
                    Login TEXT PRIMARY KEY,
                    TotalTime INT,
                    LaunchCount INT,
                    FOREIGN KEY(Login) REFERENCES Users(Login) ON DELETE CASCADE
                );";
                command.ExecuteNonQuery();

                command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Questions (
                    Text TEXT PRIMARY KEY,
                    AnswerText TEXT
                );";
                command.ExecuteNonQuery();

                command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Answers (
                    Text TEXT PRIMARY KEY,
                    PupilChange REAL,
                    DelayChange REAL,
                    PulseChange REAL
                );";
                command.ExecuteNonQuery();

                command.CommandText = @"
                CREATE TABLE IF NOT EXISTS SessionResults (
                    Login TEXT,
                    IsReplicant BOOLEAN,
                    IsCorrect BOOLEAN,
                    FOREIGN KEY(Login) REFERENCES Users(Login) ON DELETE CASCADE
                );";
                command.ExecuteNonQuery();
            }
            connection.Close();
        }
    }

    public Canvas canvas;

    public void LoginOrRegister()
    {
        errorText.text = "";

        string login = usernameField.text;
        string password = passwordField.text;

        using (var connection = new SqliteConnection(dbName))
        {
            try
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM Users WHERE Login = @login;";
                    command.Parameters.AddWithValue("@login", login);
                    int userCount = Convert.ToInt32(command.ExecuteScalar());
                    Debug.Log($"User count for '{login}': {userCount}");

                    if (userCount > 0)
                    {
                        command.CommandText = "SELECT Password FROM Users WHERE Login = @login;";
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@login", login);
                        string storedPassword = command.ExecuteScalar()?.ToString();

                        if (storedPassword == password)
                        {
                            currentUser = login;
                            if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
                            {
                                errorText.text = $"Login successful for user: {login}";
                            }
                            else
                            {
                                errorText.text = $"Успешный вход для пользователя: {login}";
                            }
                            Debug.Log($"Login successful for user: {login}");

                            LoadPlayerStats(command, login);
                        }
                        else
                        {
                            if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
                            {
                                errorText.text = $"Login failed for user: {login}. Wrong password.";
                            }
                            else
                            {
                                errorText.text = $"Не удалось войти: {login}. Неверный пароль.";
                            }
                            Debug.Log($"Login failed for user: {login}. Wrong password.");
                            source.PlayOneShot(clip);
                            return;
                        }
                    }
                    else
                    {
                        RegisterNewUser(command, login, password);
                        if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
                        {
                            errorText.text = $"Registration successful for user: {login}";
                        }
                        else
                        {
                            errorText.text = $"Регистрация прошла успешно: {login}";
                        }
                        Debug.Log($"Registration successful for user: {login}");
                    }
                }
            }
            catch (Exception ex)
            {
                if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
                {
                    errorText.text = "Database error: " + ex.Message;
                }
                else
                {
                    errorText.text = "Ошибка базы данных: " + ex.Message;
                }
                Debug.LogError("Database error: " + ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }
        panel.SetActive(false);
        TextNotify.SetActive(true);
        canvas.GetComponent<GraphicRaycaster>().enabled = false;
        logdialog.SetActive(true);
        if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
        {
            TextNotify.GetComponent<TMP_Text>().text = " ";
        }
        else
        {
            TextNotify.GetComponent<TMP_Text>().text = "С ВОЗВРАЩЕНИЕМ!";
        }

    }

    private void LoadPlayerStats(SqliteCommand command, string login)
    {
        command.CommandText = "SELECT TotalTime, LaunchCount FROM PlayerStatistics WHERE Login = @login;";
        command.Parameters.Clear();
        command.Parameters.AddWithValue("@login", login);
        Debug.Log($"User '{login}' welcome back!.");
    }

    private void RegisterNewUser(SqliteCommand command, string login, string password)
    {
        command.CommandText = "INSERT INTO Users (Login, Password) VALUES (@login, @password);";
        command.Parameters.Clear();
        command.Parameters.AddWithValue("@login", login);
        command.Parameters.AddWithValue("@password", password);
        command.ExecuteNonQuery();

        command.CommandText = "INSERT INTO PlayerStatistics (Login, TotalTime, LaunchCount) VALUES (@login, 0, 0);";
        command.ExecuteNonQuery();

        currentUser = login;
        Debug.Log($"User '{login}' registered and stats initialized.");
    }
    public void SaveProgress()
    {
        if (string.IsNullOrEmpty(currentUser))
        {
            Debug.LogError("No user logged in");
            return;
        }

        using (var connection = new SqliteConnection(dbName))
        {
            connection.Open();

            using (var transaction = connection.BeginTransaction())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
    UPDATE PlayerStatistics
    SET TotalTime = TotalTime + @totalTime,
        LaunchCount = LaunchCount + @launchCount
    WHERE Login = @login;

    -- Если записи с таким Login нет, вставляем новую
    INSERT INTO PlayerStatistics (Login, TotalTime, LaunchCount)
    SELECT @login, @totalTime, @launchCount
    WHERE (SELECT changes() = 0);
";

                    command.Parameters.Clear();
                    command.Parameters.AddWithValue("@login", currentUser);
                    command.Parameters.AddWithValue("@totalTime", DBdata.CurSessionTime);
                    command.Parameters.AddWithValue("@launchCount", DBdata.CurSessionLaunchCount);
                    command.ExecuteNonQuery();

                    foreach (var q in DBdata.Questions)
                    {
                        command.Parameters.Clear();
                        command.CommandText = @"
        INSERT OR REPLACE INTO Questions (Text, AnswerText)
        VALUES (@text, @answerText);";
                        command.Parameters.AddWithValue("@text", q.questionText);
                        command.Parameters.AddWithValue("@answerText", q.answer.answerText);
                        command.ExecuteNonQuery();

                        command.Parameters.Clear();
                        command.CommandText = @"
        INSERT OR REPLACE INTO Answers (Text, PupilChange, DelayChange, PulseChange)
        VALUES (@text, @pupil, @delay, @pulse);";
                        command.Parameters.AddWithValue("@text", q.answer.answerText);
                        command.Parameters.AddWithValue("@pupil", q.answer.effect.pupilChange);
                        command.Parameters.AddWithValue("@delay", q.answer.effect.delayChange);
                        command.Parameters.AddWithValue("@pulse", q.answer.effect.pulseChange);
                        command.ExecuteNonQuery();
                    }
                    command.Parameters.Clear();
                    command.CommandText = "DELETE FROM SessionResults WHERE Login = @login;";
                    command.Parameters.AddWithValue("@login", currentUser);
                    command.ExecuteNonQuery();

                    foreach (var res in DBdata.CurrentSessionResults)
                    {
                        command.Parameters.Clear();
                        command.CommandText = @"
                        INSERT INTO SessionResults (Login, IsReplicant, IsCorrect)
                        VALUES (@login, @isReplicant, @isCorrect);";
                        command.Parameters.AddWithValue("@login", currentUser);
                        command.Parameters.AddWithValue("@isReplicant", res.IsReplicant ? 1 : 0);
                        command.Parameters.AddWithValue("@isCorrect", res.IsCorrect ? 1 : 0);
                        command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
            }

            connection.Close();
        }

        Debug.Log("Progress saved");
    }
}
