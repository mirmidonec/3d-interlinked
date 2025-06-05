using UnityEngine;

using UnityEngine.UI;
using UnityEngine.Localization.Settings;
using System.Collections;

public class PauseMenu : MonoBehaviour
{
    [Header("Canvas references")]
    [SerializeField] private GameObject _pauseMenu;

    [Header("Settings elements references")]
    [SerializeField] private Slider _volumeSlider;
    [SerializeField] private Slider _mouseSensitivitySlider;

    public PlayerController player;

    public static bool IsPaused;

    private const string VolumeKey = "Volume";
    private const string SensitivityKey = "MouseSensitivity";
    private const string LanguageKey = "Language";
    public enum Language { English, Russian }
    public static Language CurrentLanguage = Language.Russian;

    public GameObject languagePanel;

    public GameObject IntroPanel;

    public static bool IsFirstAttemp = true;

    private void Awake()
    {
        IsPaused = false;
        LoadData();
        if (!IsFirstAttemp)
        {
            languagePanel.SetActive(false);
            IntroPanel.SetActive(true);
        }
        else
        {

            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            IsFirstAttemp = false;
        }
    }

    public bool activeLocal;
    public void ChangeLocale(int id)
    {
        if (activeLocal)
        {
            return;
        }
        if (id == 0)
        {
            CurrentLanguage = Language.Russian;
        }
        else
        {
            CurrentLanguage = Language.English;
        }
        StartCoroutine(SetLocale(id));

    }


    private IEnumerator SetLocale(int id)
    {
        activeLocal = true;
        yield return LocalizationSettings.InitializationOperation;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[id];
        activeLocal = false;
    }

    private void SaveData()
    {
        PlayerPrefs.SetFloat(VolumeKey, _volumeSlider.value);
        PlayerPrefs.SetFloat(SensitivityKey, _mouseSensitivitySlider.value);
        PlayerPrefs.Save();
    }
    public void ToggleLanguage()
    {
        if (CurrentLanguage == Language.English)
        {
            CurrentLanguage = Language.Russian;
        }
        else
        {
            CurrentLanguage = Language.English;
        }

        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        int langID = (int)CurrentLanguage;
        PlayerPrefs.SetInt(LanguageKey, langID);
        PlayerPrefs.Save();
        StartCoroutine(SetLocale(langID));
    }


    private void LoadData()
    {
        float volume = PlayerPrefs.GetFloat(VolumeKey, 0.5f);
        float sensitivity = PlayerPrefs.GetFloat(SensitivityKey, 0.5f);
        int langID = PlayerPrefs.GetInt(LanguageKey, 0);
        CurrentLanguage = (Language)langID;
        StartCoroutine(SetLocale(langID));

        _volumeSlider.value = volume;
        _mouseSensitivitySlider.value = sensitivity;

        AudioListener.volume = volume;
        player.lookSpeedX = sensitivity;
        player.lookSpeedY = sensitivity;
    }

    private void Update()
    {
        if (Ending.IsEnding || IntroText.isIntro || player.IsDialogue)
        {
            _pauseMenu.gameObject.SetActive(false);
            return;
        }
        if (CameraTranzition.InPC)
        {
            _pauseMenu.gameObject.SetActive(false);
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!_pauseMenu.gameObject.activeSelf)
            {
                OpenPauseMenu();
            }
            else
            {
                ClosePauseMenu();
            }
        }
    }

    private void OpenPauseMenu()
    {
        Time.timeScale = 0f;
        IsPaused = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        _pauseMenu.gameObject.SetActive(true);
    }

    public void ClosePauseMenu()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _pauseMenu.gameObject.SetActive(false);
        SaveData();
    }

    public void ChangeVolume()
    {
        AudioListener.volume = _volumeSlider.value;
    }

    public void ChangeMouseSensitivity()
    {
        player.lookSpeedX = _mouseSensitivitySlider.value;
        player.lookSpeedY = _mouseSensitivitySlider.value;
    }
}
