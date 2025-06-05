using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class Device : Interactable
{
    public Animator EyeDetectAnimator;

    public AudioSource Source;
    public AudioClip clip1;
    public AudioClip clip2;

    public AudioSource HumsSource;

    private Pillow pillowScript;

    public PlayerController player;


    public bool IsOn;

    public GameObject Eye;

    public GameObject EyeDetectPump;

    public Material redEye;

    public static Device Instance;

    public GameObject Igor;

    // Новая логика кнопки
    public GameObject powerbutton;
    public Transform powerbutton_in;
    public Transform powerbutton_out;


    public GameObject hacker1;
    public GameObject hacker2;

    protected override void Awake()
    {
        Instance = this;
        base.Awake();
    }

    public override void OnFocus()
    {
        if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
        {
            Message = "Device";
        }
        else
        {
            Message = "Устройство";
        }
        _outline.enabled = true;
    }

    public override void OnInteract()
    {
        if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
        {
            Message = "The device that is used for interrogation";
        }
        else
        {
            Message = "Устройство, используемое для допроса";
        }
    }

    public override void OnInteractAlt()
    {
        if (IsOn)
        {
            if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
            {
                Message = "Already ON";
            }
            else
            {
                Message = "Уже ВКЛ";
            }
            return;
        }

        if (player.isSitting && NpcChoiser.Instance.currentNpc.hasSat && !isActivating)
        {
            StartCoroutine(StartDeviceSequence());
        }
        else
        {
            if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
            {
                Message = "Can't do this now";
            }
            else
            {
                Message = "Сейчас это сделать нельзя";
            }
        }
    }
    bool isActivating = false;
    private IEnumerator StartDeviceSequence()
    {
        isActivating = true;
        if (PauseMenu.CurrentLanguage == PauseMenu.Language.English)
        {
            Message = "Activating system...";
        }
        else
        {
            Message = "Активация системы...";
        }
        yield return StartCoroutine(ButtonAnimation());

        Source.PlayOneShot(clip1);
        yield return new WaitForSeconds(0.1f);
        HumsSource.volume = 0.5f;
        yield return new WaitForSeconds(0.1f);

        EyeDetectAnimator.SetTrigger("IsOn");
        yield return new WaitForSeconds(10f);

        Eye.SetActive(true);
        EyeDetectPump.GetComponent<MeshRenderer>().material = redEye;
        Source.PlayOneShot(clip2);
        hacker1.SetActive(true);
        hacker2.SetActive(true);
        // Igor.SetActive(true);
        IsOn = true;
        QuestManager.Instance.CompleteStep(1);
        isActivating = false;
    }

    private IEnumerator ButtonAnimation()
    {
        powerbutton.transform.position = powerbutton_in.position;
        yield return new WaitForSeconds(0.15f);
        powerbutton.transform.position = powerbutton_out.position;
    }

    public override void OnLoseFocus()
    {
        _outline.enabled = false;
        Message = "";
    }
}
