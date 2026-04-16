using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Zona de combate dentro de uma fase.
/// Quando o jogador entra na zona, barreiras aparecem trancando o jogador na area.
/// Waves de inimigos sao spawnadas. Ao eliminar todos, as barreiras abrem.
///
/// Uso no editor:
/// 1. Criar um GameObject vazio com CombatZone
/// 2. Adicionar um trigger collider (BoxCollider2D, isTrigger=true) como area de ativacao
/// 3. Configurar as barreiras (colliders que trancam o jogador)
/// 4. Posicionar spawn points onde os inimigos aparecem
/// 5. Configurar as waves no Inspector
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class CombatZone : MonoBehaviour
{
    [Header("Waves")]
    [SerializeField] private SpawnWave[] waves;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Barreiras")]
    [Tooltip("GameObjects com colliders que trancam o jogador na zona. Ativados durante o combate.")]
    [SerializeField] private GameObject[] barriers;

    [Header("Camera (Opcional)")]
    [Tooltip("Limites da camera durante o combate. Se vazio, camera segue normal.")]
    [SerializeField] private Transform cameraLimitLeft;
    [SerializeField] private Transform cameraLimitRight;

    [Header("Combo")]
    [Tooltip("Pausar o combo timer durante transicoes entre waves")]
    [SerializeField] private bool pauseComboBetweenWaves = true;

    [Header("Modo Teste (sem waves)")]
    [Tooltip("Se > 0 e a zona nao tem waves, trava a camera por esse tempo (debug/feel). 0 = completa imediatamente.")]
    [SerializeField] private float testEmptyZoneDuration = 0f;

    private int currentWaveIndex;
    private int enemiesAliveInWave;
    private int totalEnemiesKilled;
    private int totalScoreEarned;
    private bool isActive;
    private bool isCompleted;
    private List<EnemyController> activeEnemies = new List<EnemyController>();

    // Propriedades publicas
    public bool IsActive => isActive;
    public bool IsCompleted => isCompleted;
    public int CurrentWaveIndex => currentWaveIndex;
    public int TotalWaves => waves != null ? waves.Length : 0;
    public int EnemiesAlive => enemiesAliveInWave;
    public int TotalEnemiesKilled => totalEnemiesKilled;

    // Eventos
    public event Action<CombatZone> OnZoneActivated;
    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCompleted;
    public event Action<CombatZone> OnZoneCompleted;
    public event Action<int> OnEnemyKilled; // scoreValue

    private void Awake()
    {
        // Garantir que o collider da zona eh trigger
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;

        // Desativar barreiras no inicio
        SetBarriersActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isActive || isCompleted) return;
        if (!other.CompareTag("Player")) return;

        ActivateZone();
    }

    /// <summary>
    /// Ativa a zona de combate: fecha barreiras e inicia waves.
    /// </summary>
    public void ActivateZone()
    {
        if (isActive || isCompleted) return;

        bool hasWaves = waves != null && waves.Length > 0;
        if (!hasWaves && testEmptyZoneDuration <= 0f)
        {
            CompleteZone();
            return;
        }

        isActive = true;
        currentWaveIndex = 0;
        totalEnemiesKilled = 0;
        totalScoreEarned = 0;

        SetBarriersActive(true);
        NotifyCameraEnter();
        OnZoneActivated?.Invoke(this);

        if (hasWaves)
            StartCoroutine(StartWaveSequence());
        else
            StartCoroutine(TestLockRoutine());
    }

    private IEnumerator TestLockRoutine()
    {
        yield return new WaitForSeconds(testEmptyZoneDuration);
        CompleteZone();
    }

    private void NotifyCameraEnter()
    {
        var cam = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;
        if (cam == null) return;

        float minX, maxX;
        if (cameraLimitLeft != null && cameraLimitRight != null)
        {
            minX = Mathf.Min(cameraLimitLeft.position.x, cameraLimitRight.position.x);
            maxX = Mathf.Max(cameraLimitLeft.position.x, cameraLimitRight.position.x);
        }
        else
        {
            var col = GetComponent<BoxCollider2D>();
            var b = col.bounds;
            minX = b.min.x;
            maxX = b.max.x;
        }
        Vector2 center = new Vector2((minX + maxX) * 0.5f, transform.position.y);
        cam.EnterCombatZone(center, minX, maxX);
    }

    private void NotifyCameraExit()
    {
        var cam = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;
        cam?.ExitCombatZone();
    }

    private IEnumerator StartWaveSequence()
    {
        while (currentWaveIndex < waves.Length)
        {
            var wave = waves[currentWaveIndex];

            // Delay antes da wave
            if (wave.delayBeforeWave > 0f)
            {
                // Pausar combo durante transicao entre waves
                if (pauseComboBetweenWaves && currentWaveIndex > 0)
                    PausePlayerCombo();

                yield return new WaitForSeconds(wave.delayBeforeWave);

                if (pauseComboBetweenWaves && currentWaveIndex > 0)
                    ResumePlayerCombo();
            }

            // Spawnar wave
            SpawnWaveEnemies(wave);
            OnWaveStarted?.Invoke(currentWaveIndex);

            // Esperar todos os inimigos da wave morrerem
            yield return new WaitUntil(() => enemiesAliveInWave <= 0);

            OnWaveCompleted?.Invoke(currentWaveIndex);
            currentWaveIndex++;
        }

        // Todas as waves completadas
        CompleteZone();
    }

    private void SpawnWaveEnemies(SpawnWave wave)
    {
        enemiesAliveInWave = 0;

        foreach (var entry in wave.enemies)
        {
            for (int i = 0; i < entry.count; i++)
            {
                StartCoroutine(SpawnEnemyDelayed(entry.enemyPrefab, i * wave.spawnInterval));
                enemiesAliveInWave++;
            }
        }
    }

    private IEnumerator SpawnEnemyDelayed(GameObject prefab, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (prefab == null || spawnPoints == null || spawnPoints.Length == 0)
            yield break;

        var spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];

        // Pequeno offset aleatorio para nao spawnar todos no mesmo ponto
        Vector2 offset = UnityEngine.Random.insideUnitCircle * 0.5f;
        Vector3 spawnPos = spawnPoint.position + (Vector3)offset;

        var enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        enemy.layer = LayerMask.NameToLayer("Enemy");

        var controller = enemy.GetComponent<EnemyController>();
        if (controller != null)
        {
            controller.OnEnemyDied += HandleEnemyDied;
            activeEnemies.Add(controller);
        }
    }

    private void HandleEnemyDied(int scoreValue)
    {
        enemiesAliveInWave--;
        totalEnemiesKilled++;
        totalScoreEarned += scoreValue;
        OnEnemyKilled?.Invoke(scoreValue);
    }

    private void CompleteZone()
    {
        isActive = false;
        isCompleted = true;

        // Abrir barreiras
        SetBarriersActive(false);
        NotifyCameraExit();

        // Limpar referencias
        activeEnemies.Clear();

        OnZoneCompleted?.Invoke(this);
    }

    private void SetBarriersActive(bool active)
    {
        if (barriers == null) return;
        foreach (var barrier in barriers)
        {
            if (barrier != null)
                barrier.SetActive(active);
        }
    }

    private void PausePlayerCombo()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        var combo = player.GetComponent<ComboSystem>();
        combo?.Pause();
    }

    private void ResumePlayerCombo()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        var combo = player.GetComponent<ComboSystem>();
        combo?.Resume();
    }

    /// <summary>
    /// Forcar o fim da zona (debug, cutscene, etc).
    /// </summary>
    public void ForceComplete()
    {
        StopAllCoroutines();

        // Destruir inimigos restantes
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null)
                Destroy(enemy.gameObject);
        }

        enemiesAliveInWave = 0;
        CompleteZone();
    }

    // --- Gizmos para visualizar a zona no editor ---
    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col == null) return;

        Gizmos.color = isCompleted ? new Color(0, 1, 0, 0.15f) :
                       isActive ? new Color(1, 0, 0, 0.25f) :
                       new Color(1, 0.4f, 0, 0.15f);

        Gizmos.DrawCube(transform.position + (Vector3)col.offset, col.size);

        // Limites laterais da camera (linhas verticais laranja - sempre visiveis)
        var orange = new Color(1f, 0.5f, 0f, 1f);
        if (cameraLimitLeft != null) DrawZoneLine(cameraLimitLeft.position.x, orange, "Combat L");
        if (cameraLimitRight != null) DrawZoneLine(cameraLimitRight.position.x, orange, "Combat R");

        // Spawn points
        if (spawnPoints != null)
        {
            Gizmos.color = Color.red;
            foreach (var sp in spawnPoints)
            {
                if (sp != null)
                    Gizmos.DrawWireSphere(sp.position, 0.3f);
            }
        }
    }

    private void DrawZoneLine(float x, Color color, string label)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(new Vector3(x, -50f, 0f), new Vector3(x, 50f, 0f));
#if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.Label(new Vector3(x, 4f, 0f), label);
#endif
    }
}
