using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class AnimalCage : MonoBehaviour, IDamageable
{
    [Header("Estagios da jaula (GameObjects que ligam/desligam)")]
    [Tooltip("Visual intacto (ex.: Jaula1).")]
    public GameObject intactStage;
    [Tooltip("Visual danificado (ex.: Jaula2).")]
    public GameObject damagedStage;
    [Tooltip("Visual quebrado (ex.: Jaula3).")]
    public GameObject brokenStage;
    [Tooltip("Sorting order do estagio QUEBRADO (detrito no chao): baixo pra ficar ATRAS dos animais libertos.")]
    public int brokenSortingOrder = 4;

    [Header("Resistencia (em GOLPES, nao dano)")]
    [Tooltip("Golpes pra ficar DANIFICADA.")]
    public int hitsToDamage = 3;
    [Tooltip("Golpes pra QUEBRAR 100% (total acumulado; ex.: 5 = 3 + 2).")]
    public int hitsToBreak = 5;

    [Header("Animais presos")]
    [Tooltip("Os animais aliados presos (ex.: 3 cobras). Comecam parados/protegidos; ao quebrar, agradecem e lutam.")]
    public List<AllyCreature> trappedAnimals = new List<AllyCreature>();
    [Tooltip("Um dos animais grita a fala de agradecimento ao ser libertado (so um, pra nao sobrepor texto).")]
    public bool animalThanksOnFree = true;

    [Header("Resgate / fim de combate")]
    [Tooltip("A zona de combate desta jaula. Ao limpar a zona, os animais libertos que sobreviverem fogem pra esquerda + bonus.")]
    public CombatZone zone;
    [Tooltip("Pontos de bonus por animal liberto que SOBREVIVE ate o fim da zona.")]
    public int survivorBonus = 3000;
    [Tooltip("Meia-largura (em X) do trecho onde os aliados libertos podem se mover (em volta da jaula), pra nao sairem da area atras de inimigo que foge.")]
    public float allyLeashHalfWidth = 7f;

    [Header("Pedido de socorro (preso)")]
    [Tooltip("Intervalo medio entre as falas de socorro dos animais presos.")]
    public float pleaInterval = 5.5f;
    [Tooltip("Distancia do player pra os animais comecarem a pedir socorro.")]
    public float pleaPlayerRange = 13f;

    [Header("Eventos")]
    public UnityEvent onDamaged;
    public UnityEvent onBroken;

    private int hits;
    private bool broken;
    private float pleaTimer;
    private Transform player;

    public bool IsBroken => broken;

    public static int TotalSurvivors;

    private void Start()
    {
        ShowStage(0); // intacta

        bool listEmpty = trappedAnimals == null || trappedAnimals.Count == 0 || trappedAnimals.TrueForAll(a => a == null);
        if (listEmpty)
            trappedAnimals = new List<AllyCreature>(GetComponentsInChildren<AllyCreature>(true));

        ProtectAnimal(true);

        if (zone != null) zone.OnZoneCompleted += HandleZoneCompleted;
    }

    private void OnDestroy()
    {
        if (zone != null) zone.OnZoneCompleted -= HandleZoneCompleted;
    }

    private void Update()
    {
        if (broken || trappedAnimals == null || trappedAnimals.Count == 0) return;

        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go == null) return;
            player = go.transform;
        }
        if (Vector2.Distance(player.position, transform.position) > pleaPlayerRange) return;

        pleaTimer -= Time.deltaTime;
        if (pleaTimer > 0f) return;
        pleaTimer = pleaInterval + Random.Range(-1f, 1.5f);

        AllyCreature speaker = null;
        int seen = 0;
        foreach (var a in trappedAnimals)
        {
            if (a == null || a.IsDead) continue;
            seen++;
            if (Random.Range(0, seen) == 0) speaker = a;
        }
        if (speaker != null) speaker.Say(speaker.caughtLine);
    }

    private void HandleZoneCompleted(CombatZone z)
    {
        if (!broken) return;
        var stage = FindFirstObjectByType<StageManager>();
        bool farewellGiven = false;
        foreach (var a in trappedAnimals)
        {
            if (a == null || a.IsDead) continue; // mortos: corpo fica na cena
            a.FleeLeft(!farewellGiven);
            farewellGiven = true;
            TotalSurvivors++;
            if (stage != null && survivorBonus > 0)
            {
                stage.AddScore(survivorBonus);
                FloatingDamageText.Spawn(a.transform.position + Vector3.up * 2.6f, survivorBonus, 5);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (broken) return;
        hits++;
        PlayHitSfx();

        if (hits >= hitsToBreak)
        {
            Break();
        }
        else if (hits >= hitsToDamage)
        {
            ShowStage(1); // danificada
            onDamaged?.Invoke();
        }
    }

    private void Break()
    {
        broken = true;
        ShowStage(2); // quebrada

        if (brokenStage != null)
        {
            var ys = brokenStage.GetComponent<YSortRenderer>();
            if (ys != null) ys.enabled = false;
            var bsr = brokenStage.GetComponent<SpriteRenderer>();
            if (bsr != null) bsr.sortingOrder = brokenSortingOrder;
        }

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false; // jaula quebrada nao e mais alvo

        FreeAnimal();
        onBroken?.Invoke();
    }

    private void FreeAnimal()
    {
        ProtectAnimal(false);

        float cx = (zone != null) ? zone.transform.position.x : transform.position.x;
        float leashMin = cx - allyLeashHalfWidth;
        float leashMax = cx + allyLeashHalfWidth;

        bool thanked = false;
        foreach (var a in trappedAnimals)
        {
            if (a == null) continue;
            a.SetLeash(leashMin, leashMax);
            if (animalThanksOnFree && !thanked) { a.Shout(); thanked = true; }
            a.Activate();
        }
    }

    private void ShowStage(int stage)
    {
        if (intactStage != null) intactStage.SetActive(stage == 0);
        if (damagedStage != null) damagedStage.SetActive(stage == 1);
        if (brokenStage != null) brokenStage.SetActive(stage == 2);
    }

    private void ProtectAnimal(bool protect)
    {
        foreach (var a in trappedAnimals)
        {
            if (a == null) continue;
            a.SetFrozen(protect);
            var hb = a.GetComponent<BoxCollider2D>();
            if (hb != null) hb.enabled = !protect;
        }
    }

    private void PlayHitSfx()
    {
        var sm = SoundManager.Instance;
        if (sm != null && sm.Library != null && sm.Library.enemyHurt != null)
            sm.PlaySFX(sm.Library.enemyHurt);
    }

    private void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return;
        Gizmos.color = broken ? Color.green : new Color(1f, 0.6f, 0.1f, 0.9f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
