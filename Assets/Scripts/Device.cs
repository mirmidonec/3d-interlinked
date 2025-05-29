using System.Collections;
using UnityEngine;

public class Device : Interactable
{
    public GameObject ButtonOff;
    public GameObject ButtonOn;

    public Animator EyeDetectAnimator;

    public AudioSource Source;
    public AudioClip clip1;
    public AudioClip clip2;

    public AudioSource HumsSource;

    private Pillow pillowScript;

    public PlayerController player;
    public NpcBehaviour npc;

    public bool IsOn;

    public GameObject Eye;

    public GameObject EyeDetectPump;

    public Material redEye;

    public static Device Instance;

    public GameObject Igor;


    protected override void Awake()
    {
        Instance = this;
        base.Awake();
    }
    public override void OnFocus()
    {

        Message = "Device";
        _outline.enabled = true;
    }

    public override void OnInteract()
    {
        Message = "Turn ON";
    }

    public override void OnInteractAlt()
    {
        if (IsOn)
        {
            Message = "Alredy ON";
            return;
        }

        if (player.isSitting && npc.hasSat && !ButtonOff.activeSelf)
        {
            StartCoroutine(EyeRoutine());
        }
        else if (ButtonOff.activeSelf)
        {
            Message = "Loading system...";
        }
        else
        {
            Message = "Can't do this now";
        }
    }
    private IEnumerator EyeRoutine()
    {
        ButtonOn.transform.position = new Vector3(0, 0, +1);
        ButtonOff.SetActive(true);
        Source.PlayOneShot(clip1);
        yield return new WaitForSeconds(0.1f);
        HumsSource.volume = 0.5f;
        yield return new WaitForSeconds(0.1f);
        EyeDetectAnimator.SetTrigger("IsOn");
        yield return new WaitForSeconds(10f);
        Eye.SetActive(true);
        EyeDetectPump.GetComponent<MeshRenderer>().material = redEye;
        Source.PlayOneShot(clip2);
        Igor.SetActive(true);
        IsOn = true;
        QuestManager.Instance.CompleteStep(1);
    }

    public override void OnLoseFocus()
    {
        _outline.enabled = false;
        Message = "";
    }
}
