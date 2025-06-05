using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class WorldSpaceInteract : MonoBehaviour
{
  void Update()
    {
        if (Input.GetMouseButtonDown(0)) 
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                var pointerClickHandler = hit.collider.GetComponent<IPointerClickHandler>();
                if (pointerClickHandler != null)
                {
                    PointerEventData pointerData = new PointerEventData(EventSystem.current);
                    pointerData.position = Input.mousePosition;

                    pointerClickHandler.OnPointerClick(pointerData);
                }
            }
        }
    }
}
