using UnityEngine;
using UnityEngine.UI;

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

    private void Awake()
    {
        IsPaused = false;
        LoadData();
    }

    private void SaveData()
    {
        PlayerPrefs.SetFloat(VolumeKey, _volumeSlider.value);
        PlayerPrefs.SetFloat(SensitivityKey, _mouseSensitivitySlider.value);
        PlayerPrefs.Save();
    }

    private void LoadData()
    {
        float volume = PlayerPrefs.GetFloat(VolumeKey, 0.5f);
        float sensitivity = PlayerPrefs.GetFloat(SensitivityKey, 0.5f);

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
