using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FinalBossEncounter : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("A CombatZone4 (trigger/barreiras/camera).")]
    public CombatZone zone;
    [Tooltip("Os 2 chefes posicionados na arena (EnemyController DESABILITADO no inicio).")]
    public List<EnemyController> bosses = new List<EnemyController>();
    [Tooltip("Pontos onde os capangas aparecem (pode reusar os spawn points da zona).")]
    public List<Transform> capangaSpawnPoints = new List<Transform>();

    [Header("Capangas crescentes (75% / 50% / 25%)")]
    [Tooltip("Leva 1 (75%): prefabs dos capangas. Sugestao: 2 fracos.")]
    public List<GameObject> wave1 = new List<GameObject>();
    [Tooltip("Leva 2 (50%): 3 capangas.")]
    public List<GameObject> wave2 = new List<GameObject>();
    [Tooltip("Leva 3 (25%): 4 capangas mais fortes.")]
    public List<GameObject> wave3 = new List<GameObject>();
    [Tooltip("Intervalo entre cada spawn de capanga.")]
    public float spawnInterval = 0.4f;

    [Header("Recuo dos chefes")]
    [Tooltip("Velocidade que os chefes CORREM pra fora da tela ao recuar.")]
    public float fleeSpeed = 8f;
    [Tooltip("Velocidade que os chefes voltam ANDANDO (pelo mesmo lado que fugiram).")]
    public float returnWalkSpeed = 5f;
    [Tooltip("Fala de um chefe ao voltar pra arena depois de chamar capangas.")]
    public string bossReturnBark = "Esse maldito é resistente!";

    [Header("Falas")]
    [TextArea(2, 3)] public string araraLine = "Ei, são aqueles caras que tão capturando os animais!";
    public string araraLineKey = "world.fala.arara_chefes";
    [Tooltip("Frases do chefe ao chamar capangas (uma por leva: 75% / 50% / 25%).")]
    public string[] callMinionsBarks = {
        "Capangas! Acabem com ele!",
        "Eu pago vocês pra quê, seus inúteis?!",
        "Não fiquem aí parados, peguem esse moleque!",
    };

    [Header("Desfecho")]
    [Tooltip("Controlador do reencontro dos javalis. Se vazio, a fase completa direto.")]
    public BoarReunion reunion;

    private StageManager stageManager;
    private bool started;
    private bool finished;
    private readonly List<EnemyController> liveMinions = new List<EnemyController>();
    private readonly Dictionary<EnemyController, Vector3> bossHome = new Dictionary<EnemyController, Vector3>();

    private void Start()
    {
        stageManager = FindFirstObjectByType<StageManager>();
        if (zone != null) zone.OnZoneActivated += HandleZoneActivated;

        foreach (var b in bosses)
            if (b != null) b.enabled = false;
    }

    private void OnDestroy()
    {
        if (zone != null) zone.OnZoneActivated -= HandleZoneActivated;
    }

    private void HandleZoneActivated(CombatZone z)
    {
        if (started) return;
        started = true;
        StartCoroutine(RunEncounter());
    }

    private IEnumerator RunEncounter()
    {
        var arara = AraraSpeechBubble.Instance;
        var companion = FindFirstObjectByType<CompanionFollow>();
        if (companion != null) companion.HoldFlee(true);
        if (arara != null)
        {
            arara.Show(ResolveArara());
            yield return new WaitUntil(() => !arara.HasUntypedSpeech);
        }
        if (companion != null) companion.HoldFlee(false);

        var playerCombo = GameObject.FindGameObjectWithTag("Player")?.GetComponent<ComboSystem>();
        if (playerCombo != null) playerCombo.SetSuppressTimeoutBreak(true);

        int totalMax = 0;
        foreach (var b in bosses)
        {
            if (b == null) continue;
            b.enabled = true;
            b.gunUnlocked = false;
            bossHome[b] = b.transform.position;
            var hs = b.GetComponent<HealthSystem>();
            if (hs != null) { totalMax += hs.MaxHealth; }
            b.OnEnemyDied += HandleBossOrMinionScore;
        }
        if (totalMax <= 0) yield break;

        float[] thresholds = { 0.75f, 0.50f, 0.25f };
        int idx = 0;

        while (!finished)
        {
            if (AllBossesDead()) break;

            if (idx < thresholds.Length && CombinedBossHealth() <= totalMax * thresholds[idx])
            {
                bool unlockGun = (idx == 1); // 50% destrava o tiro
                yield return ThresholdRoutine(idx, unlockGun);
                idx++;
            }
            yield return null;
        }

        // 4) Chefes mortos -> desfecho
        yield return EndSequence();
    }

    private IEnumerator ThresholdRoutine(int waveIndex, bool unlockGun)
    {
        var cam = Camera.main;
        float halfW = (cam != null && cam.orthographic) ? cam.orthographicSize * cam.aspect : 12f;
        float camX = cam != null ? cam.transform.position.x : 0f;
        float offRight = camX + halfW + 3f;

        var shouter = FirstAliveBoss();
        string bark = PickMinionBark(waveIndex);
        if (shouter != null && !string.IsNullOrEmpty(bark))
            EnemyBark.Spawn(shouter.transform.position + Vector3.up * 2.6f, bark);

        if (unlockGun)
            foreach (var b in bosses) if (b != null) b.gunUnlocked = true;

        int running = 0;
        foreach (var b in bosses)
        {
            if (b == null || !b.IsAlive) continue;
            running++;
            StartCoroutine(RetreatRun(b, offRight, () => running--));
        }
        yield return new WaitUntil(() => running == 0);

        yield return SpawnMinions(WaveFor(waveIndex));
        yield return new WaitUntil(AllMinionsDead);

        int returning = 0;
        foreach (var b in bosses)
        {
            if (b == null || !b.IsAlive) continue;
            returning++;
            StartCoroutine(ReturnWalk(b, offRight, () => returning--));
        }
        yield return new WaitUntil(() => returning == 0);

        // um chefe reclama ao voltar
        var back = FirstAliveBoss();
        if (back != null && !string.IsNullOrEmpty(bossReturnBark))
            EnemyBark.Spawn(back.transform.position + Vector3.up * 2.6f, bossReturnBark);
    }

    private IEnumerator RetreatRun(EnemyController b, float offRightX, System.Action onDone)
    {
        b.SetInvulnerable(true); // nao morre fugindo
        b.SetCombatPaused(true);
        while (b != null && b.IsAlive && b.transform.position.x < offRightX)
        {
            b.FaceTowards(offRightX);
            b.SetMoveAnimSpeed(fleeSpeed);
            b.transform.position += Vector3.right * fleeSpeed * Time.deltaTime;
            yield return null;
        }
        if (b != null) b.SetMoveAnimSpeed(0f);
        onDone?.Invoke();
    }

    private IEnumerator ReturnWalk(EnemyController b, float offRightX, System.Action onDone)
    {
        Vector3 home = bossHome.TryGetValue(b, out var h) ? h : b.transform.position;
        b.transform.position = new Vector3(offRightX, home.y, home.z); // volta pela direita
        while (b != null && b.transform.position.x > home.x)
        {
            b.FaceTowards(home.x);
            b.SetMoveAnimSpeed(returnWalkSpeed);
            b.transform.position += Vector3.left * returnWalkSpeed * Time.deltaTime;
            yield return null;
        }
        if (b != null)
        {
            b.transform.position = home;
            b.SetMoveAnimSpeed(0f);
            b.SetInvulnerable(false);
            b.SetCombatPaused(false);
        }
        onDone?.Invoke();
    }

    private IEnumerator SpawnMinions(List<GameObject> prefabs)
    {
        liveMinions.Clear();
        if (prefabs == null || capangaSpawnPoints.Count == 0) yield break;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        foreach (var prefab in prefabs)
        {
            if (prefab == null) continue;
            var sp = capangaSpawnPoints[Random.Range(0, capangaSpawnPoints.Count)];
            Vector3 pos = sp.position + (Vector3)(Random.insideUnitCircle * 0.5f);
            var go = Instantiate(prefab, pos, Quaternion.identity);
            if (enemyLayer >= 0) go.layer = enemyLayer;
            var ec = go.GetComponent<EnemyController>();
            if (ec != null)
            {
                liveMinions.Add(ec);
                ec.OnEnemyDied += HandleBossOrMinionScore;
            }
            if (spawnInterval > 0f) yield return new WaitForSeconds(spawnInterval);
        }
    }

    private IEnumerator EndSequence()
    {
        if (finished) yield break;
        finished = true;

        int survivors = BoarRescue.LastSurvivorCount;
        if (reunion != null)
        {
            bool done = false;
            reunion.Play(survivors, () => done = true);
            yield return new WaitUntil(() => done);
        }

        // restaura o combo normal
        var playerCombo = GameObject.FindGameObjectWithTag("Player")?.GetComponent<ComboSystem>();
        if (playerCombo != null) playerCombo.SetSuppressTimeoutBreak(false);

        if (zone != null) zone.ForceComplete();
        if (stageManager != null) stageManager.CompleteStage();

        if (GameFlowManager.Instance == null)
            UnityEngine.SceneManagement.SceneManager.LoadScene("CutsceneFinalizadoraFase1");
    }

    // --- helpers ---
    private List<GameObject> WaveFor(int i) => i == 0 ? wave1 : (i == 1 ? wave2 : wave3);

    private int CombinedBossHealth()
    {
        int sum = 0;
        foreach (var b in bosses)
        {
            if (b == null) continue;
            var hs = b.GetComponent<HealthSystem>();
            if (hs != null && !hs.IsDead) sum += hs.CurrentHealth;
        }
        return sum;
    }

    private bool AllBossesDead()
    {
        foreach (var b in bosses) if (b != null && b.IsAlive) return false;
        return true;
    }

    private EnemyController FirstAliveBoss()
    {
        foreach (var b in bosses) if (b != null && b.IsAlive) return b;
        return null;
    }

    private bool AllMinionsDead()
    {
        foreach (var m in liveMinions) if (m != null && m.IsAlive) return false;
        return true;
    }

    private void HandleBossOrMinionScore(int scoreValue)
    {
    }

    private string PickMinionBark(int waveIndex)
    {
        if (callMinionsBarks == null || callMinionsBarks.Length == 0) return null;
        int i = Mathf.Clamp(waveIndex, 0, callMinionsBarks.Length - 1);
        return callMinionsBarks[i];
    }

    private string ResolveArara()
    {
        if (!string.IsNullOrEmpty(araraLineKey))
        {
            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                string s = loc.GetText(araraLineKey);
                if (!string.IsNullOrEmpty(s) && !s.StartsWith("[")) return s;
            }
        }
        return araraLine;
    }
}
