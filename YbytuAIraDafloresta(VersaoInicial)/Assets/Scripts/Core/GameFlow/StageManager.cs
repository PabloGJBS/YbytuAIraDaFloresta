using UnityEngine;
using System;

/// <summary>
/// Gerenciador de uma fase (stage) em execucao.
/// Controla a progressao entre CombatZones, score e tempo.
/// Colocar na cena de cada fase.
/// </summary>
public class StageManager : MonoBehaviour
{
    [Header("Zonas de Combate (em ordem)")]
    [SerializeField] private CombatZone[] combatZones;

    [Header("Eventos da Fase")]
    [SerializeField] private bool autoCompleteOnAllZones = true;

    [Header("Recompensa ao limpar zona")]
    [Tooltip("Fracao da vida maxima curada ao concluir cada zona de combate.")]
    [SerializeField, Range(0f, 1f)] private float healPercentPerZone = 0.15f;
    [Tooltip("Pontos por ponto de vida excedente quando a vida ja esta cheia.")]
    [SerializeField] private int overflowPointsPerHp = 10;

    private int currentZoneIndex;
    private int totalScore;
    private int enemiesKilled;
    private float stageTime;
    private bool stageActive;
    private bool stageCompleted;

    // Propriedades publicas
    public int TotalScore => totalScore;
    public float StageTime => stageTime;
    public bool IsActive => stageActive;
    public bool IsCompleted => stageCompleted;
    public int CurrentZoneIndex => currentZoneIndex;
    public int TotalZones => combatZones != null ? combatZones.Length : 0;
    public CombatZone[] CombatZones => combatZones;

    // Eventos
    public event Action OnStageStarted;
    public event Action<int> OnScoreChanged;
    public event Action<int, float> OnStageCompleted; // score, time

    private void Start()
    {
        StartStage();
    }

    private void Update()
    {
        if (stageActive && !stageCompleted)
            stageTime += Time.deltaTime;
    }

    public void StartStage()
    {
        stageActive = true;
        stageCompleted = false;
        totalScore = 0;
        enemiesKilled = 0;
        stageTime = 0f;
        currentZoneIndex = 0;

        // Registrar eventos de todas as zonas
        if (combatZones != null)
        {
            foreach (var zone in combatZones)
            {
                if (zone == null) continue;
                zone.OnEnemyKilled += HandleEnemyKilled;
                zone.OnZoneCompleted += HandleZoneCompleted;
            }
        }

        OnStageStarted?.Invoke();
    }

    private void HandleEnemyKilled(int scoreValue)
    {
        totalScore += scoreValue;
        enemiesKilled++;
        OnScoreChanged?.Invoke(totalScore);
    }

    private void HandleZoneCompleted(CombatZone zone)
    {
        currentZoneIndex++;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Pausar combo na transicao entre zonas
            player.GetComponent<ComboSystem>()?.Pause();

            // Recompensa: cura 15% da vida; excedente (vida cheia) vira pontos
            RewardZoneClear(player);
        }

        // Verificar se todas as zonas foram completadas
        if (autoCompleteOnAllZones && AllZonesCompleted())
            CompleteStage();
    }

    /// <summary>
    /// Ao limpar uma zona: cura uma fracao da vida do player. Se a vida ja estiver
    /// cheia, o que sobraria da cura vira pontos de bonus.
    /// </summary>
    private void RewardZoneClear(GameObject player)
    {
        if (healPercentPerZone <= 0f) return;
        var hp = player.GetComponentInChildren<HealthSystem>();
        if (hp == null) return;

        int healAmount = Mathf.RoundToInt(hp.MaxHealth * healPercentPerZone);
        if (healAmount <= 0) return;

        int before = hp.CurrentHealth;
        hp.Heal(healAmount);
        int overflow = healAmount - (hp.CurrentHealth - before);

        if (overflow > 0 && overflowPointsPerHp > 0)
            AddScore(overflow * overflowPointsPerHp);
    }

    private bool AllZonesCompleted()
    {
        if (combatZones == null) return true;
        foreach (var zone in combatZones)
        {
            if (zone != null && !zone.IsCompleted)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Completar a fase. Envia score e tempo para o GameFlowManager.
    /// </summary>
    public void CompleteStage()
    {
        if (stageCompleted) return;
        stageCompleted = true;
        stageActive = false;

        OnStageCompleted?.Invoke(totalScore, stageTime);

        int hits = 0;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var combo = player.GetComponent<ComboSystem>();
            if (combo != null) hits = combo.TotalHitsLanded;
        }

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.CompleteStage(totalScore, stageTime, hits, enemiesKilled);
    }

    /// <summary>
    /// Adicionar score extra (bonus, itens, etc).
    /// </summary>
    public void AddScore(int amount)
    {
        totalScore += amount;
        OnScoreChanged?.Invoke(totalScore);
    }

    private void OnDestroy()
    {
        if (combatZones == null) return;
        foreach (var zone in combatZones)
        {
            if (zone == null) continue;
            zone.OnEnemyKilled -= HandleEnemyKilled;
            zone.OnZoneCompleted -= HandleZoneCompleted;
        }
    }
}
