using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ZoneMiniBoss : MonoBehaviour
{
    [Header("Refs")]
    public CombatZone zone;
    [Tooltip("O chefe rebaixado (startingEnemy da zona, EnemyController desabilitado no inicio).")]
    public EnemyController boss;
    [Tooltip("Onde os reforcos aparecem. Reusar os spawn points da zona.")]
    public List<Transform> spawnPoints = new List<Transform>();
    [Tooltip("Prefabs de reforco chamados ao bater o limiar (sugestao: 2-3 comuns Alt).")]
    public List<GameObject> reinforcements = new List<GameObject>();

    [Header("Gatilho")]
    [Range(0f, 1f)]
    [Tooltip("Fracao da vida do chefe que dispara o recuo+reforco (uma unica vez).")]
    public float threshold = 0.5f;
    public float spawnInterval = 0.35f;

    [Header("Recuo")]
    public float fleeSpeed = 8f;
    public float returnWalkSpeed = 5f;

    [Header("Falas")]
    public string callBark = "Capangas! Acabem com ele!";
    public string returnBark = "Você vai se arrepender disso!";

    private HealthSystem bossHp;
    private bool armed;
    private bool triggered;   // ja disparou (uma vez so)
    private bool running;     // coreografia em andamento
    private readonly List<EnemyController> liveMinions = new List<EnemyController>();

    private void Start()
    {
        if (zone != null) zone.OnZoneActivated += HandleZoneActivated;
    }

    private void OnDestroy()
    {
        if (zone != null) zone.OnZoneActivated -= HandleZoneActivated;
    }

    private void HandleZoneActivated(CombatZone z)
    {
        if (boss != null) bossHp = boss.GetComponent<HealthSystem>();
        armed = true;
    }

    private void Update()
    {
        if (!armed || triggered || running) return;
        if (boss == null || bossHp == null || !boss.IsAlive) return;
        if (bossHp.CurrentHealth <= Mathf.RoundToInt(bossHp.MaxHealth * threshold))
        {
            triggered = true;
            StartCoroutine(RetreatReinforceReturn());
        }
    }

    private IEnumerator RetreatReinforceReturn()
    {
        running = true;

        if (!string.IsNullOrEmpty(callBark))
            EnemyBark.Spawn(boss.transform.position + Vector3.up * 2.6f, callBark);

        var cam = Camera.main;
        float halfW = (cam != null && cam.orthographic) ? cam.orthographicSize * cam.aspect : 12f;
        float offRight = (cam != null ? cam.transform.position.x : 0f) + halfW + 3f;
        Vector3 home = boss.transform.position;

        yield return RetreatRun(boss, offRight);
        yield return SpawnMinions();
        yield return new WaitUntil(AllMinionsDead);
        yield return ReturnWalk(boss, offRight, home);

        if (boss != null && boss.IsAlive && !string.IsNullOrEmpty(returnBark))
            EnemyBark.Spawn(boss.transform.position + Vector3.up * 2.6f, returnBark);

        running = false;
    }

    private IEnumerator RetreatRun(EnemyController b, float offRightX)
    {
        b.SetInvulnerable(true);
        b.SetCombatPaused(true);
        while (b != null && b.IsAlive && b.transform.position.x < offRightX)
        {
            b.FaceTowards(offRightX);
            b.SetMoveAnimSpeed(fleeSpeed);
            b.transform.position += Vector3.right * fleeSpeed * Time.deltaTime;
            yield return null;
        }
        if (b != null) b.SetMoveAnimSpeed(0f);
    }

    private IEnumerator ReturnWalk(EnemyController b, float offRightX, Vector3 home)
    {
        if (b == null) yield break;
        b.transform.position = new Vector3(offRightX, home.y, home.z);
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
    }

    private IEnumerator SpawnMinions()
    {
        liveMinions.Clear();
        if (reinforcements == null || reinforcements.Count == 0 || spawnPoints.Count == 0) yield break;

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        foreach (var prefab in reinforcements)
        {
            if (prefab == null) continue;
            var sp = spawnPoints[Random.Range(0, spawnPoints.Count)];
            Vector3 pos = sp.position + (Vector3)(Random.insideUnitCircle * 0.5f);
            var go = Instantiate(prefab, pos, Quaternion.identity);
            if (enemyLayer >= 0) go.layer = enemyLayer;
            var ec = go.GetComponent<EnemyController>();
            if (ec != null) liveMinions.Add(ec);
            if (spawnInterval > 0f) yield return new WaitForSeconds(spawnInterval);
        }
    }

    private bool AllMinionsDead()
    {
        foreach (var m in liveMinions)
            if (m != null && m.IsAlive) return false;
        return true;
    }
}
