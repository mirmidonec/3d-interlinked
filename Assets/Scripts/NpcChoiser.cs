using UnityEngine;

public class NpcChoiser : MonoBehaviour
{
    public NpcBehaviour[] npcs;
    public NpcBehaviour currentNpc;

    public static NpcChoiser Instance;

    /// <summary>Индекс вчерашнего подозреваемого. Статик переживает перезагрузку сцены.</summary>
    public static int LastIndex = -1;

    private void Awake()
    {
        Instance = this;
        ChooseNextNpc();
    }

    /// <summary>
    /// Случайный подозреваемый, но НИКОГДА тот же, что вчера.
    /// Остальные NPC выключаются: иначе тот, кто включён в сцене по умолчанию,
    /// приходил бы каждый день, а диалог брался бы от выбранного (рассинхрон).
    /// Выбранного NPC не трогаем: его включает посадка игрока (PlayerController.HandleSit).
    /// </summary>
    public void ChooseNextNpc()
    {
        if (npcs == null || npcs.Length == 0)
        {
            Debug.LogWarning("NPC список пуст или не назначен!");
            return;
        }

        int index = 0;
        if (npcs.Length > 1)
        {
            do { index = Random.Range(0, npcs.Length); }
            while (index == LastIndex);
        }

        LastIndex = index;
        currentNpc = npcs[index];
        GameState.SuspectIsReplicant = currentNpc.IsReplicant;

        for (int i = 0; i < npcs.Length; i++)
        {
            if (i != index && npcs[i] != null)
                npcs[i].gameObject.SetActive(false);
        }

        Debug.Log($"Выбран NPC: {currentNpc.name} (индекс {index})");
    }
}