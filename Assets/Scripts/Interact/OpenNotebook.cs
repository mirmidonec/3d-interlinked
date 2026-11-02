using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OpenNotebook : MonoBehaviour
{
    [SerializeField] private float closedX;
    [SerializeField] private float openedX;
    [SerializeField] private float speed;
    [SerializeField] private GameObject[] _pages;
    [SerializeField] private TMP_Text _pagesCount;

    [SerializeField] private AudioClip _turnPage;

    private int _currentPage;
    private AudioSource _source;
    private bool _isOpened = false;
    private float targetX;

    public bool IsOpen => _isOpened;

    private bool _isForward = true;
    public GameObject crosshair;


        public GameObject note;

    private void OnEnable()
    {
        crosshair.SetActive(false);
        _source.PlayOneShot(_turnPage);
        note.SetActive(true);
    }
    private void OnDisable()
    {
        crosshair.SetActive(true);
        note.SetActive(false);
    }
    private void Awake()
    {
        _source = GetComponent<AudioSource>();
    }

    private void Start()
    {
        _currentPage = 0;

        for (int i = 0; i < _pages.Length; i++)
        {
            _pages[i].SetActive(i == _currentPage);
        }
        UpdatePageCountText();
        targetX = closedX;
        SetRotation(targetX);
    }

    private void Update()
    {

        if (PauseMenu.IsPaused)
        {
            return;
        }
        HandleInput();
        SmoothRotate();
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _source.PlayOneShot(_turnPage);
            _isOpened = !_isOpened;
            targetX = _isOpened ? openedX : closedX;
        }
        else if (Input.GetMouseButtonDown(1) && _isOpened)
        {
            TurnPage();
        }
    }
    public void CloseNotebook()
    {
        if (_isOpened)
        {
            _isOpened = false;
            targetX = closedX;
            _source.PlayOneShot(_turnPage);
        }
        if (QuestManager.Instance.IsQuestCompleted(0) == false && QuestManager.Instance.IsQuestCompletedBefore(0) == false)
        {
            QuestManager.Instance.CompleteStep(0);
        }
    }
    private void TurnPage()
    {
        _pages[_currentPage].SetActive(false);

        if (_isForward)
        {
            if (_currentPage < _pages.Length - 1)
            {
                _currentPage++;
            }
            else
            {
                _isForward = false;
                _currentPage--;
            }
        }
        else
        {
            if (_currentPage > 0)
            {
                _currentPage--;
            }
            else
            {
                _isForward = true;
                _currentPage++;
            }
        }

        _pages[_currentPage].SetActive(true);

        if (_turnPage != null && _source != null)
        {
            _source.PlayOneShot(_turnPage);
        }
        UpdatePageCountText();
    }

    private void UpdatePageCountText()
    {
        _pagesCount.text = $"{_currentPage + 1} / {_pages.Length}";
    }

    private void SmoothRotate()
    {
        float currentX = transform.localEulerAngles.x;
        float newX = Mathf.MoveTowardsAngle(currentX, targetX, speed * Time.deltaTime);
        SetRotation(newX);
    }

    private void SetRotation(float x)
    {
        Vector3 rot = transform.localEulerAngles;
        rot.x = x;
        transform.localEulerAngles = rot;
    }
}
