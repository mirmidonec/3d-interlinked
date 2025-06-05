using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class NpcChoiser : MonoBehaviour
{
    public NpcBehaviour[] npcs;
    public NpcBehaviour currentNpc;

    public static NpcChoiser Instance;

    private static int lastSelectedIndex = -1;

    private void Awake()
    {
        Instance = this;
        ChooseNextNpc();
    }

    public void ChooseNextNpc()
    {
        if (npcs != null && npcs.Length > 0)
        {
            lastSelectedIndex = (lastSelectedIndex + 1) % npcs.Length;

            currentNpc = npcs[lastSelectedIndex];
            Debug.Log($"Выбран NPC: {currentNpc.name} (индекс {lastSelectedIndex})");
        }
        else
        {
            Debug.LogWarning("NPC список пуст или не назначен!");
        }
    }
}

