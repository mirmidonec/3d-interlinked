using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Device : Interactable
{
    public GameObject ButtonOff;
    public GameObject ButtonOn;

    public GameObject EyeDetect;
    public GameObject Pillow;

    public AudioSource Source;
    public AudioClip clip;

    public AudioSource HumsSource;

    private Pillow pillowScript;
    private Animator eyeDetectAnimator;

    public PlayerController player;
    public NpcBehaviour npc;

    public bool IsOn;

    public GameObject Eye;

    public GameObject EyeDetectPump;

    public Material redEye;


    protected override void Awake()
    {
        base.Awake();
        pillowScript = Pillow.GetComponent<Pillow>();
        eyeDetectAnimator = EyeDetect.GetComponent<Animator>();
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
        if (player.isSitting && npc.hasSat)
        {
         
            StartCoroutine(EyeRoutine());

        }
        else
        {
            Message = "Sit down and wait for suspect before use";
        }
    }

    private IEnumerator EyeRoutine()
    {
        ButtonOn.transform.position = new Vector3(0, 0, +1);
        ButtonOff.SetActive(true);
        Source.PlayOneShot(clip);
        yield return new WaitForSeconds(0.1f);

        //pillowScript.enabled = true;
        yield return new WaitForSeconds(0.1f);
        //eyeDetectAnimator.enabled = true;
        yield return new WaitForSeconds(0.1f);
      

        //HumsSource.enabled = true;
        yield return new WaitForSeconds(0.1f);
        IsOn = true;
        yield return new WaitForSeconds(10f);
        Eye.SetActive(true);
        EyeDetectPump.GetComponent<MeshRenderer>().material = redEye;
    }

    public override void OnLoseFocus()
    {
        _outline.enabled = false;
        Message = "";
    }
}
