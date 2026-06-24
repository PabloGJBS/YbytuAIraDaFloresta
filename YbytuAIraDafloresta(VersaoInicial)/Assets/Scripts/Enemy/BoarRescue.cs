using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Coordena o set-piece dos javalis na CombatZone3. Antes do player chegar: ENCENACAO
/// (os 2 inimigos iniciais - com EnemyController desligado - e os javalis trocam golpes
/// SEM dano). Quando a zona ativa (player entrou): os javalis viram aliados (gritam +
/// dano real) e os inimigos ganham uma PEQUENA CHANCE de atacar os javalis em vez do
/// player (priorizam o Ybytu). A vida de todos so comeca a cair a partir daqui.
/// </summary>
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

    /// <summary>Quantos javalis pais sobreviveram ao confronto 3 (lido pelo desfecho da luta final).</summary>
    public static int LastSurvivorCount;

    private void Start()
    {
        if (zone != null)
        {
            zone.OnZoneActivated += HandleZoneActivated;
            zone.OnZoneCompleted += HandleZoneCompleted;
        }

        // Pareia a encenacao: cada javali encara um inimigo.
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

    // Briga acabou: os javalis que SOBREVIVERAM correm pra esquerda (um pede pra procurar os
    // filhotes). Os mortos ficam de corpo na cena.
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

            // BONUS por javali sobrevivente: pontos extras + numero flutuante de comemoracao.
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

        // Vira cada inimigo pro javali (encara o oponente, nao ficam todos pro mesmo lado).
        for (int i = 0; i < startingEnemies.Count; i++)
        {
            var e = startingEnemies[i];
            if (e == null || e.enabled) continue;
            if (i < boars.Count && boars[i] != null)
                e.FaceTowards(boars[i].transform.position.x);
        }

        // Golpes encenados periodicos (a anim toca, mas OnAttackHit nao da dano porque o
        // estado do controller continua Idle). Vida nao cai ate o player chegar.
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
        // PAUSA DRAMATICA: todo mundo para enquanto o javali grita (da tempo de ler).
        PlayerController.InputFrozen = true;
        foreach (var b in boars)
        {
            if (b == null) continue;
            b.SetFrozen(true);
            if (b.shoutOnActivate) b.Shout();
        }
        // (os inimigos iniciais ainda estao com o EnemyController desligado = nao se mexem)

        yield return new WaitForSeconds(pauseBeforeCombat);

        // Solta o combate: player volta a andar, javalis viram aliados, inimigos entram.
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

        // Inimigos: entram no combate JA (reagem aos golpes dos javalis com Hurt na hora) e
        // priorizam o player com pequena chance de atacar os javalis.
        foreach (var e in startingEnemies)
        {
            if (e == null) continue;
            e.enabled = true;
            e.allyTargets = boarHealths;
            e.allyAttackDamage = 0; // usa o data.attackDamage do proprio inimigo
            e.allyAggroChance = enemyAllyAggroChance;
        }
    }
}
