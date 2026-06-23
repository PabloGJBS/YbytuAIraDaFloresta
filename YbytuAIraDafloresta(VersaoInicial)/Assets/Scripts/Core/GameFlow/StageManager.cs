using UnityEngine;
using System;
using System.Collections;

public class StageManager : MonoBehaviour
{
    [Header("Zonas de Combate (em ordem)")]
    [SerializeField] private CombatZone[] combatZones;

    [Header("Fase (pra TESTE DIRETO: Play nesta cena, sem o menu)")]
    [Tooltip("StageData desta fase. Permite testar o fim de fase (score -> cutscene finalizadora correta) dando Play direto na cena, sem jogar desde o inicio. No fluxo normal nao interfere.")]
    [SerializeField] private StageData stageData;

    [Header("Eventos da Fase")]
    [SerializeField] private bool autoCompleteOnAllZones = true;

    [Header("Recompensa ao limpar zona")]
    [Tooltip("Fracao da vida maxima curada ao concluir cada zona de combate.")]
    [SerializeField, Range(0f, 1f)] private float healPercentPerZone = 0.15f;
    [Tooltip("Fracao da vida maxima curada ao derrotar cada inimigo.")]
    [SerializeField, Range(0f, 1f)] private float healPercentPerKill = 0.02f;
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

    private void Awake()
    {
        if (stageData != null)
        {
            if (GameFlowManager.Instance == null)
                new GameObject("GameFlowManager (DirectPlay)").AddComponent<GameFlowManager>();
            GameFlowManager.Instance?.PrimeDirectPlay(stageData);
        }
    }

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
        totalScore = GameFlowManager.Instance != null ? GameFlowManager.Instance.CarryOverScore : 0;
        enemiesKilled = 0;
        stageTime = 0f;
        currentZoneIndex = 0;

        EnemyController.OnAnyEnemyDied -= HandleEnemyKilled;
        EnemyController.OnAnyEnemyDied += HandleEnemyKilled;

        if (combatZones != null)
        {
            foreach (var zone in combatZones)
            {
                if (zone == null) continue;
                zone.OnZoneCompleted += HandleZoneCompleted;
            }
        }

        OnStageStarted?.Invoke();
        OnScoreChanged?.Invoke(totalScore);

        ResumeFromCheckpointIfAny();
        PersistProgress();
    }

    private void ResumeFromCheckpointIfAny()
    {
        if (GameFlowManager.Instance == null || !GameFlowManager.Instance.HasStageCheckpoint) return;
        if (combatZones == null || combatZones.Length == 0) return;

        int checkpoint = Mathf.Clamp(GameFlowManager.Instance.StageCheckpointZone, 0, combatZones.Length - 1);
        if (checkpoint <= 0) return;

        for (int i = 0; i < checkpoint; i++)
            if (combatZones[i] != null) combatZones[i].SkipAsCleared();

        currentZoneIndex = checkpoint;
        StartCoroutine(RepositionToCheckpoint(checkpoint));
    }

    private IEnumerator RepositionToCheckpoint(int checkpoint)
    {
        yield return null;

        var zone = combatZones[checkpoint];
        if (zone == null) yield break;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) yield break;

        float entranceX = zone.transform.position.x;
        var col = zone.GetComponent<BoxCollider2D>();
        if (col != null) entranceX += col.offset.x - col.size.x * 0.5f;

        var p = player.transform.position;
        player.transform.position = new Vector3(entranceX - 2f, p.y, p.z);

        var camCtrl = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;
        if (camCtrl != null) camCtrl.SnapToTarget();
    }

    private void HandleEnemyKilled(int scoreValue)
    {
        totalScore += scoreValue;
        enemiesKilled++;
        OnScoreChanged?.Invoke(totalScore);
        HealPlayerOnKill();
    }

    public void RegisterExternalKill(int scoreValue) => HandleEnemyKilled(scoreValue);

    private void HealPlayerOnKill()
    {
        if (healPercentPerKill <= 0f) return;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        var hp = player.GetComponentInChildren<HealthSystem>();
        if (hp == null) return;
        int amount = Mathf.Max(1, Mathf.RoundToInt(hp.MaxHealth * healPercentPerKill));
        hp.Heal(amount);
    }

    private void HandleZoneCompleted(CombatZone zone)
    {
        currentZoneIndex++;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.GetComponent<ComboSystem>()?.Pause();

            RewardZoneClear(player);
        }

        PersistProgress();

        if (autoCompleteOnAllZones && AllZonesCompleted())
            CompleteStage();
    }

    private void PersistProgress()
    {
        if (SaveManager.Instance == null) return;
        int idx = (GameFlowManager.Instance != null && GameFlowManager.Instance.CurrentStage != null)
            ? GameFlowManager.Instance.CurrentStage.stageIndex
            : (stageData != null ? stageData.stageIndex : 0);
        int lives = GameFlowManager.Instance != null ? GameFlowManager.Instance.PlayerLives : 3;
        SaveManager.Instance.SaveProgress(idx, currentZoneIndex, totalScore, lives);
    }

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

    public void AddScore(int amount)
    {
        totalScore += amount;
        OnScoreChanged?.Invoke(totalScore);
    }

    private void OnDestroy()
    {
        EnemyController.OnAnyEnemyDied -= HandleEnemyKilled;
        if (combatZones == null) return;
        foreach (var zone in combatZones)
        {
            if (zone == null) continue;
            zone.OnZoneCompleted -= HandleZoneCompleted;
        }
    }
}
