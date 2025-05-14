using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntroText : MonoBehaviour
{
    public GameObject[] entries;

    private bool visible;
    public int index;

    public AudioSource speaker_entry;

    public AudioClip sound_entry;
    public AudioClip sound_entry1;

    public float range1 = 0.95f;
    public float range2 = 1.05f;

    private float pitch;
    private bool fs1;

    public GameObject Panel;

    public GameObject OST;
    private void Update()
    {
        if (Input.GetButtonDown("Fire1") && visible)
        {
            if (index < entries.Length - 1)
            {
                index++;
                HideAll();
                GetNextEntry();
                PlaySound();
            }
            else
            {
                HideAll();
                StartCoroutine(GoToMainScene());
            }
        }
    }

    private void PlaySound()
    {
        pitch = Random.Range(range1, range2);
        speaker_entry.pitch = pitch;
        speaker_entry.PlayOneShot(sound_entry);
    }

    private void GetNextEntry()
    {
        if (index >= 0 && index < entries.Length)
        {
            entries[index].SetActive(true);
        }
    }

    private void HideAll()
    {
        foreach (GameObject entry in entries)
        {
            entry.SetActive(false);
        }
    }

    private IEnumerator GoToMainScene()
    {
        if (!fs1)
        {
            speaker_entry.PlayOneShot(sound_entry1);
            fs1 = true;
        }

        yield return new WaitForSeconds(3f);
        StartCoroutine(Panel.GetComponent<Transition>().FadeOut());
        OST.SetActive(true);
        
    }

    private IEnumerator Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        visible = false;

        yield return new WaitForSeconds(3f);

        if (entries.Length > 0)
        {
            entries[0].SetActive(true);
            visible = true;
            PlaySound();
        }
    }
}
