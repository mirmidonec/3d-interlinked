using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IntroText : MonoBehaviour
{
    [Header("UI Entries")]
    [SerializeField] private GameObject[] _entries;

    [Header("Audio")]
    [SerializeField] private AudioSource _speakerAudioSource;
    [SerializeField] private AudioClip _entrySound;
    [SerializeField] private AudioClip _finalSound;

    [Header("Audio Pitch Range")]
    [SerializeField] private float _pitchRangeMin;
    [SerializeField] private float _pitchRangeMax;

    [Header("References")]
    [SerializeField] private GameObject _transitionPanel;
    [SerializeField] private GameObject _ost;
    [SerializeField] private PlayerController _player;

    private int _currentIndex = 0;
    private bool _isVisible = false;
    private bool _hasPlayedFinalSound = false;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _isVisible = false;
        _currentIndex = 0;
        _hasPlayedFinalSound = false;

        HideAllEntries();

        StartCoroutine(ShowFirstEntryWithDelay(1f));
    }


    private IEnumerator ShowFirstEntryWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_entries.Length > 0)
        {
            ShowEntry(_currentIndex);
            _isVisible = true;
            PlayEntrySound();
        }
    }


    private void Update()
    {
        if (Input.GetButtonDown("Fire1") && _isVisible)
        {
            HandleNextEntry();
        }
    }

    private void HandleNextEntry()
    {
        if (_currentIndex < _entries.Length - 1)
        {
            _currentIndex++;
            HideAllEntries();
            ShowEntry(_currentIndex);
            PlayEntrySound();
        }
        else
        {
            HideAllEntries();
            StartCoroutine(EndIntroSequence());
        }
    }

    private void PlayEntrySound()
    {
        if (_speakerAudioSource != null && _entrySound != null)
        {
            float randomPitch = Random.Range(_pitchRangeMin, _pitchRangeMax);
            _speakerAudioSource.pitch = randomPitch;
            _speakerAudioSource.PlayOneShot(_entrySound);
        }
    }

    private void ShowEntry(int index)
    {
        if (index >= 0 && index < _entries.Length)
        {
            _entries[index].SetActive(true);
        }
    }

    private void HideAllEntries()
    {
        foreach (var entry in _entries)
        {
            entry.SetActive(false);
        }
    }

    private IEnumerator EndIntroSequence()
    {
        if (!_hasPlayedFinalSound && _speakerAudioSource != null && _finalSound != null)
        {
            _speakerAudioSource.PlayOneShot(_finalSound);
            _hasPlayedFinalSound = true;
        }

        yield return new WaitForSeconds(2f);
        _player.enabled = true;
        yield return new WaitForSeconds(1f);

        if (_transitionPanel != null)
        {
            var transition = _transitionPanel.GetComponent<Transition>();
            if (transition != null)
            {
                yield return StartCoroutine(transition.FadeOut());
            }
        }

        if (_ost != null)
        {
            _ost.SetActive(true);
        }
    }
}
