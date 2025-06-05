using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChildrenActivator : MonoBehaviour
{    public float delay;

    private void Start()
    {
        StartCoroutine(ActivateChildrenSequentially());
    }

    private IEnumerator ActivateChildrenSequentially()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
            yield return new WaitForSeconds(delay);
        }
    }
}
