using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Coordena quantos inimigos podem atacar simultaneamente.
/// Padrao beat'em up: poucos atacando, demais circulando esperando vez.
/// Singleton auto-criado se nao existir na cena.
/// </summary>
public class EnemyAttackCoordinator : MonoBehaviour
{
    [Tooltip("Numero maximo de inimigos atacando ao mesmo tempo.")]
    [SerializeField] private int maxAttackers = 4;

    private readonly Dictionary<EnemyController, int> attackerSlots = new Dictionary<EnemyController, int>();
    private readonly HashSet<int> usedSlotIndices = new HashSet<int>();

    private static EnemyAttackCoordinator _instance;

    public static EnemyAttackCoordinator Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<EnemyAttackCoordinator>();
                if (_instance == null)
                {
                    var go = new GameObject("EnemyAttackCoordinator");
                    _instance = go.AddComponent<EnemyAttackCoordinator>();
                }
            }
            return _instance;
        }
    }

    public int MaxAttackers => maxAttackers;
    public int CurrentAttackers => attackerSlots.Count;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }
        _instance = this;
    }

    /// <summary>
    /// Tenta reservar um slot de ataque. Retorna o indice do slot (0..maxAttackers-1)
    /// ou -1 se nenhum slot disponivel. O indice define o ponto ao redor do player
    /// onde este inimigo deve se posicionar pra atacar.
    /// </summary>
    public int TryReserveSlot(EnemyController enemy)
    {
        if (enemy == null) return -1;
        if (attackerSlots.TryGetValue(enemy, out int existing)) return existing;
        if (attackerSlots.Count >= maxAttackers) return -1;

        for (int i = 0; i < maxAttackers; i++)
        {
            if (!usedSlotIndices.Contains(i))
            {
                usedSlotIndices.Add(i);
                attackerSlots[enemy] = i;
                return i;
            }
        }
        return -1;
    }

    public void ReleaseSlot(EnemyController enemy)
    {
        if (enemy == null) return;
        if (attackerSlots.TryGetValue(enemy, out int slot))
        {
            attackerSlots.Remove(enemy);
            usedSlotIndices.Remove(slot);
        }
    }

    public bool HasSlot(EnemyController enemy) => enemy != null && attackerSlots.ContainsKey(enemy);

    public int GetSlot(EnemyController enemy) =>
        enemy != null && attackerSlots.TryGetValue(enemy, out int slot) ? slot : -1;
}
