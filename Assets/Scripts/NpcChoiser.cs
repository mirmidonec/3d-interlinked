using UnityEngine;

public class NpcChoiser : MonoBehaviour
{
    public NpcBehaviour[] npcs;
    public NpcBehaviour currentNpc;

    public static NpcChoiser Instance;

    private void Awake()
    {
        Instance = this;
        ChooseNextNpc();
    }

    /// <summary>
    /// Каждый день случайный подозреваемый. Так личность (человек / репликант)
    /// не угадывается по номеру дня, даже если в игре всего 2 NPC.
    /// </summary>
    public void ChooseNextNpc()
    {
        if (npcs != null && npcs.Length > 0)
        {
            currentNpc = npcs[Random.Range(0, npcs.Length)];
            Debug.Log($"Выбран NPC: {currentNpc.name}");
        }
        else
        {
            Debug.LogWarning("NPC список пуст или не назначен!");
        }
    }
}