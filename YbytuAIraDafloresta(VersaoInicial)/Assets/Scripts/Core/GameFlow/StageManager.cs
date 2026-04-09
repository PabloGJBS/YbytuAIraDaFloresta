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

    private int currentZoneIndex;
    private int totalScore;
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
        OnScoreChanged?.Invoke(totalScore);
    }

    private void HandleZoneCompleted(CombatZone zone)
    {
        currentZoneIndex++;

        // Pausar combo na transicao entre zonas
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var combo = player.GetComponent<ComboSystem>();
            combo?.Pause();
        }

        // Verificar se todas as zonas foram completadas
        if (autoCompleteOnAllZones && AllZonesCompleted())
            CompleteStage();
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

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.CompleteStage(totalScore, stageTime);
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
