using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LightSwitch : MonoBehaviour
{
    public Light[] lights;          // Массив источников света, назначается в инспекторе
    public float toggleInterval = 2f;  // Интервал времени в секундах для переключения

    private bool lightsOn = true;   // Текущее состояние света
    private float timer = 0f;


    void Start()
    {
        SetLights(lightsOn);
        SetRandomColors();
           Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= toggleInterval)
        {
            timer = 0f;
            lightsOn = !lightsOn;
            SetLights(lightsOn);

            if (lightsOn)
            {
                SetRandomColors();
            }
        }

        if (Input.GetKeyDown(KeyCode.F7))
        {
            SceneManager.LoadScene(1);
        }
    }

    void SetLights(bool state)
    {
        foreach (Light light in lights)
        {
            if (light != null)
                light.enabled = state;
        }
    }

    void SetRandomColors()
    {
        foreach (Light light in lights)
        {
            if (light != null)
            {
                light.color = new Color(Random.value, Random.value, Random.value);
            }
        }
    }
}
