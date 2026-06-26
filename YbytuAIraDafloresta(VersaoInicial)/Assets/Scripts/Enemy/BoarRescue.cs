using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoarRescue : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("A CombatZone3.")]
    public CombatZone zone;
    [Tooltip("Os javalis pais (aliados).")]
    public List<BoarAlly> boars = new List<BoarAlly>();
    [Tooltip("Os inimigos que ja comecam na arena (startingEnemies da zona).")]
    public List<EnemyController> startingEnemies = new List<EnemyController>();

    [Header("Inimigos x javalis")]
    [Tooltip("Chance (0..1) de um inimigo mirar um javali em vez do player. Pequena: priorizam o Ybytu.")]
    [Range(0f, 1f)] public float enemyAllyAggroChance = 0.15f;

    [Header("Grito / pausa")]
    [Tooltip("Segundos que TODO MUNDO fica parado (player + inimigos + javalis) enquanto o javali grita, pra dar tempo de ler antes do combate.")]
    public float pauseBeforeCombat = 1.1f;

    [Header("Bonus")]
    [Tooltip("Pontos extras por javali que SOBREVIVE ate o fim da briga.")]
    public int survivorBonus = 5000;

    private bool activated;
    private float sparTimer;
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    public static int LastSurvivorCount;

    private void Start()
    {
        if (zone != null)
        {
            zone.OnZoneActivated += HandleZoneActivated;
            zone.OnZoneCompleted += HandleZoneCompleted;
        }

        for (int i = 0; i < boars.Count; i++)
        {
            if (boars[i] != null && i < startingEnemies.Count && startingEnemies[i] != null)
                boars[i].sparTarget = startingEnemies[i].transform;
        }
    }

    private void OnDestroy()
    {
        if (zone != null)
        {
            zone.OnZoneActivated -= HandleZoneActivated;
            zone.OnZoneCompleted -= HandleZoneCompleted;
        }
    }

    private void HandleZoneCompleted(CombatZone z)
    {
        bool farewellGiven = false;
        int survivors = 0;
        var stage = FindFirstObjectByType<StageManager>();
        foreach (var b in boars)
        {
            if (b == null) continue;
            var h = b.GetComponent<HealthSystem>();
            if (h != null && h.IsDead) continue; // morto: corpo fica
            survivors++;
            b.FleeLeft(!farewellGiven);
            farewellGiven = true;

            if (stage != null && survivorBonus > 0)
            {
                stage.AddScore(survivorBonus);
                FloatingDamageText.Spawn(b.transform.position + Vector3.up * 2.6f, survivorBonus, 5);
            }
        }
        LastSurvivorCount = survivors;
    }

    private void Update()
    {
        if (activated) return;

        for (int i = 0; i < startingEnemies.Count; i++)
        {
            var e = startingEnemies[i];
            if (e == null || e.enabled) continue;
            if (i < boars.Count && boars[i] != null)
                e.FaceTowards(boars[i].transform.position.x);
        }

        sparTimer -= Time.deltaTime;
        if (sparTimer <= 0f)
        {
            sparTimer = Random.Range(1.1f, 2.0f);
            foreach (var e in startingEnemies)
            {
                if (e == null || e.enabled) continue;
                var anim = e.GetComponent<Animator>();
                if (anim != null) anim.SetTrigger(AttackHash);
            }
        }
    }

    private void HandleZoneActivated(CombatZone z)
    {
        if (activated) return;
        activated = true;
        StartCoroutine(ActivationSequence());
    }

    private IEnumerator ActivationSequence()
    {
        PlayerController.InputFrozen = true;
        foreach (var b in boars)
        {
            if (b == null) continue;
            b.SetFrozen(true);
            if (b.shoutOnActivate) b.Shout();
        }

        yield return new WaitForSeconds(pauseBeforeCombat);

        PlayerController.InputFrozen = false;

        var boarHealths = new List<HealthSystem>();
        foreach (var b in boars)
        {
            if (b == null) continue;
            b.SetFrozen(false);
            b.Activate();
            var h = b.GetComponent<HealthSystem>();
            if (h != null) boarHealths.Add(h);
        }

        foreach (var e in startingEnemies)
        {
            if (e == null) continue;
            e.enabled = true;
            e.allyTargets = boarHealths;
            e.allyAttackDamage = 0;
            e.allyAggroChance = enemyAllyAggroChance;
        }
    }
}
