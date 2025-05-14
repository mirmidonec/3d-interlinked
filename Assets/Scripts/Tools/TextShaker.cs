using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextShaker : MonoBehaviour
{
    public GameObject text;

    private bool textShaking;

    private float posX;
    private float posZ;

    [Header("Shake Settings")]
    public float delay = 0.05f;

    public float posX_value1 = -1f;
    public float posX_value2 = 1f;

    public float posZ_value1 = -1f;
    public float posZ_value2 = 1f;

    private void Start()
    {
        text = gameObject;
        textShaking = true;
        StartCoroutine(ShakeTheText());
    }

    private IEnumerator ShakeTheText()
    {
        while (true)
        {
            if (!textShaking)
            {
                yield return null;
                continue;
            }

            posX = Random.Range(posX_value1, posX_value2);
            posZ = Random.Range(posZ_value1, posZ_value2);

            text.transform.localPosition = new Vector3(posX, posZ, text.transform.localPosition.z);

            yield return new WaitForSeconds(delay);
        }
    }
}
