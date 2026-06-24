using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Base reutilizavel de CRIATURA ALIADA que luta ao lado do player (javali, cobra, onça...).
/// Regras de combate centralizadas aqui pra reuso entre fases; cada bicho vira uma subclasse
/// (ex.: BoarAlly) e so configura stats/anim/sprite no Inspector.
///
/// Fluxo: SPAR (encena luta sem dano, antes do player chegar) -> Activate() (combate real:
/// persegue e ataca o inimigo mais proximo com dano baixo, alinhando PE-COM-PE) -> ao fim da
/// briga FleeLeft() (foge se sobreviveu) ou, se morrer, vira CORPO que fica na cena.
/// Movida por transform; dano direto na vida (sem colliders) via EnemyController.DamageFromAlly.
/// </summary>
[RequireComponent(typeof(HealthSystem))]
public class AllyCreature : MonoBehaviour
{
    [Header("Vida / Combate")]
    public int maxHealth = 100;
    [Tooltip("Dano por golpe. 6 = golpe rapido (jab) do player.")]
    public int attackDamage = 6;
    [Tooltip("Peso de ameaca (0..1): chance, por avaliacao do inimigo, dele focar ESTE aliado em vez do player. Onça alta (~0.5 = 50/50), cobra baixa (~0.1), javali media (~0.25).")]
    [Range(0f, 1f)] public float aggroWeight = 0.25f;
    public float moveSpeed = 2.4f;

    [Header("Dano crescente (cobra)")]
    [Tooltip("Se acerta o MESMO inimigo repetidamente, o dano sobe a cada golpe.")]
    public bool escalatingDamage = false;
    public int escalateBonusPerHit = 2;
    public int escalateMaxBonus = 12;
    [Tooltip("Distancia minima entre aliados (pra nao ficarem um por cima do outro).")]
    public float minSeparation = 1.3f;
    public float attackRange = 1.0f;
    public float attackYTolerance = 0.7f;
    [Tooltip("Ajuste vertical pra alinhar com o inimigo (lane). + sobe, - desce.")]
    public float laneYOffset = 0f;
    [Tooltip("Distancia horizontal que a criatura fica do CENTRO do inimigo ao atacar. Menor = mais colado.")]
    public float horizontalStandoff = 0.7f;
    public float attackCooldown = 1.4f;
    public float detectionRange = 14f;
    [Tooltip("Marque se o sprite aponta pra direita.")]
    public bool spriteFacesRight = true;

    [Header("Anim states (controller do bicho)")]
    public string idleState = "Idle";
    public string runState = "Run";
    public string attackState = "Attack";
    public string hurtState = "Hurt";
    public string deathState = "Death";

    [Header("Grito ao ativar")]
    public string shoutLine = "Socorro! Levaram meus filhotes!";
    public string shoutLocalizationKey = "world.fauna.javali_socorro";
    [Tooltip("O coordenador marca true em UM dos aliados pra so um gritar (sem texto sobreposto).")]
    public bool shoutOnActivate = false;
    [Tooltip("Fala de SOCORRO enquanto preso na jaula (pedindo pra ser solto). A AnimalCage mostra periodicamente.")]
    [TextArea(1, 2)] public string caughtLine = "Ei amigo, me tira daqui!!!";

    [Header("Despedida (ao fim da briga, se sobreviver)")]
    public string farewellLine = "Se você puder procurar meus filhotes, por favor!";
    public string farewellLocalizationKey = "world.fauna.javali_despedida";
    public float fleeSeconds = 6f;

    [Header("Hurtbox (hitbox do bicho)")]
    [Tooltip("Tamanho da caixa de dano (trigger). Default calibrado pra altura do herói.")]
    public Vector2 hurtboxSize = new Vector2(1.3f, 2.5f);
    [Tooltip("Deslocamento da hurtbox (sobe pra cobrir o corpo, ja que o pivo fica nos pes).")]
    public Vector2 hurtboxOffset = new Vector2(0f, 1.2f);

    [Header("Spar (antes do player chegar)")]
    [Tooltip("Oponente da encenacao: encara e finge bater nele, sem dano.")]
    public Transform sparTarget;

    protected SpriteRenderer sr;
    protected Animator anim;
    protected HealthSystem health;
    private bool hasWalkArea;
    private Vector2 walkMin, walkMax;
    private bool fighting;       // false = SPAR (sem dano), true = combate real
    private bool dead;
    private bool frozen;         // pausado (durante o grito)
    private bool attacking;      // durante a corrotina de ataque
    private bool fleeing;
    private float fleeTimer;
    private float attackTimer;
    private float searchTimer;
    private float hurtLockTimer;     // travado tomando Hurt (pra a anim de dano aparecer)
    private const float HurtLockSeconds = 0.35f;
    private EnemyController currentEnemy;
    private string currentAnim;

    // Dano crescente (cobra): rastreia o ultimo inimigo acertado e os golpes seguidos nele.
    private EnemyController lastHitEnemy;
    private int escalateStacks;

    // Registro de todas as criaturas aliadas vivas (pra separacao).
    private static readonly List<AllyCreature> All = new List<AllyCreature>();

    // Aliados ATIVOS (libertos e lutando) - os inimigos consultam pra decidir mira.
    private static readonly List<AllyCreature> ActiveList = new List<AllyCreature>();
    public static System.Collections.Generic.IReadOnlyList<AllyCreature> Active => ActiveList;

    public bool IsDead => dead;
    public HealthSystem Health => health;

    protected virtual void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        health = GetComponent<HealthSystem>();
        health.SetMaxHealth(maxHealth);

        // Hurtbox (trigger), mesma ideia do hurtbox do player; calibravel por bicho no Inspector.
        var box = GetComponent<BoxCollider2D>();
        if (box == null) box = gameObject.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = hurtboxSize;
        box.offset = hurtboxOffset;

        var zone = FindFirstObjectByType<StageZone>();
        if (zone != null && zone.HasWalkArea)
        {
            hasWalkArea = true;
            walkMin = zone.WalkMin;
            walkMax = zone.WalkMax;
        }
    }

    protected virtual void OnEnable()
    {
        health.OnDamageTaken += HandleHurt;
        health.OnDeath += HandleDeath;
        if (!All.Contains(this)) All.Add(this);
    }

    protected virtual void OnDisable()
    {
        health.OnDamageTaken -= HandleHurt;
        health.OnDeath -= HandleDeath;
        All.Remove(this);
        ActiveList.Remove(this);
    }

    /// <summary>Vira aliado de verdade: passa a dar/receber dano. (O grito e a pausa sao do coordenador.)</summary>
    public void Activate()
    {
        fighting = true;
        if (!ActiveList.Contains(this)) ActiveList.Add(this); // inimigos passam a poder mira-lo
    }

    /// <summary>Pausa/retoma (durante o grito, pra dar tempo de ler).</summary>
    public void SetFrozen(bool value)
    {
        frozen = value;
        if (frozen) Play(idleState);
    }

    private void Update()
    {
        if (dead) return;
        if (fleeing) { UpdateFlee(); return; }
        if (frozen) { Play(idleState); return; }
        // Stun de dano: fica no Hurt sem mover/atacar (senao o Update sobrescreve a anim de Hurt
        // com Run/Idle no frame seguinte e o Hurt nunca aparece).
        if (hurtLockTimer > 0f)
        {
            hurtLockTimer -= Time.deltaTime;
            return;
        }
        if (!fighting) UpdateSpar();
        else UpdateFight();
        ApplySeparation();
        transform.position = ClampToWalk(transform.position);
    }

    /// <summary>Ao fim da briga: corre pra esquerda e some (se sobreviveu). withFarewell = grita a despedida.</summary>
    public void FleeLeft(bool withFarewell)
    {
        if (dead || fleeing) return;
        fleeing = true;
        fleeTimer = fleeSeconds;
        ActiveList.Remove(this); // fugindo: inimigos param de mira-lo
        if (withFarewell) ShoutFarewell();
    }

    private void UpdateFlee()
    {
        fleeTimer -= Time.deltaTime;
        if (sr != null) sr.flipX = spriteFacesRight;     // vira pra esquerda
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;
        if (hasWalkArea)
        {
            var p = transform.position;
            p.y = Mathf.Clamp(p.y, walkMin.y, walkMax.y);
            transform.position = p;
        }
        Play(runState);
        if (fleeTimer <= 0f) Destroy(gameObject);
    }

    private void ApplySeparation()
    {
        foreach (var o in All)
        {
            if (o == this || o == null || o.dead) continue;
            Vector2 d = (Vector2)transform.position - (Vector2)o.transform.position;
            float dist = d.magnitude;
            if (dist < minSeparation && dist > 0.0001f)
                transform.position += (Vector3)(d.normalized * (minSeparation - dist) * 0.5f);
        }
    }

    // --- Encenacao (engaja de verdade, mas SEM dano) ---
    private void UpdateSpar()
    {
        if (attacking) return;
        if (sparTarget == null) { Play(idleState); return; }

        attackTimer -= Time.deltaTime;
        if (ApproachTarget(sparTarget) && attackTimer <= 0f)
        {
            attackTimer = attackCooldown;
            StartCoroutine(SparAttackRoutine());
        }
    }

    private IEnumerator SparAttackRoutine()
    {
        attacking = true;
        PlayOnce(attackState);
        yield return new WaitForSeconds(0.46f);
        attacking = false;
    }

    // --- Combate real ---
    private void UpdateFight()
    {
        if (attacking) return;

        attackTimer -= Time.deltaTime;
        searchTimer -= Time.deltaTime;

        if (currentEnemy == null || currentEnemy.CurrentState == EnemyState.Dead)
        {
            if (searchTimer <= 0f)
            {
                searchTimer = 0.3f;
                currentEnemy = FindNearestEnemy();
            }
        }
        if (currentEnemy == null) { Play(idleState); return; }

        if (ApproachTarget(currentEnemy.transform) && attackTimer <= 0f)
        {
            attackTimer = attackCooldown;
            StartCoroutine(AttackRoutine(currentEnemy));
        }
    }

    /// <summary>
    /// Anda pra ficar coladinho no alvo, alinhando PE-COM-PE (usa os limites reais dos sprites,
    /// resolvendo a diferenca de pivo). Retorna true quando esta alinhado o bastante pra atacar.
    /// </summary>
    private bool ApproachTarget(Transform target)
    {
        Vector3 tp = target.position;
        var tsr = target.GetComponentInChildren<SpriteRenderer>();
        bool haveSprites = sr != null && sr.sprite != null && tsr != null && tsr.sprite != null;

        float enemyCenterX = haveSprites ? tsr.bounds.center.x : tp.x;
        float enemyFeetY = haveSprites ? tsr.bounds.min.y : tp.y;
        float footGap = haveSprites ? transform.position.y - sr.bounds.min.y : 0f;

        float side = (transform.position.x <= enemyCenterX) ? -1f : 1f;
        float desiredX = enemyCenterX + side * horizontalStandoff;
        float desiredY = enemyFeetY + footGap + laneYOffset;
        if (hasWalkArea) desiredY = Mathf.Clamp(desiredY, walkMin.y, walkMax.y);

        Vector3 p = transform.position;
        p.x = Mathf.MoveTowards(p.x, desiredX, moveSpeed * Time.deltaTime);
        p.y = Mathf.MoveTowards(p.y, desiredY, moveSpeed * Time.deltaTime);
        transform.position = ClampToWalk(p);
        FaceX(enemyCenterX);

        // Gatilho de ataque generoso (so X): se esta colado do lado do inimigo, bate.
        // Evita ficar "travadao" correndo no lugar quando o Y nao bate exatinho.
        bool canAttack = Mathf.Abs(transform.position.x - enemyCenterX) <= horizontalStandoff + 0.6f;
        Play(canAttack ? idleState : runState);
        return canAttack;
    }

    private Vector3 ClampToWalk(Vector3 p)
    {
        if (!hasWalkArea) return p;
        p.x = Mathf.Clamp(p.x, walkMin.x, walkMax.x);
        p.y = Mathf.Clamp(p.y, walkMin.y, walkMax.y);
        return p;
    }

    private IEnumerator AttackRoutine(EnemyController enemy)
    {
        attacking = true;
        PlayOnce(attackState);
        yield return new WaitForSeconds(0.28f);

        if (!dead && enemy != null && enemy.CurrentState != EnemyState.Dead)
        {
            var esr = enemy.GetComponentInChildren<SpriteRenderer>();
            float ex = (esr != null && esr.sprite != null) ? esr.bounds.center.x : enemy.transform.position.x;
            if (Mathf.Abs(ex - transform.position.x) <= 2.0f)
            {
                enemy.DamageFromAlly(ComputeDamage(enemy), health); // passa a si mesma (alvo da retaliacao)
                var sm = SoundManager.Instance;
                if (sm != null && sm.Library != null && sm.Library.enemyPunch != null)
                    sm.PlaySFX(sm.Library.enemyPunch);
            }
        }

        yield return new WaitForSeconds(0.18f);
        attacking = false;
    }

    /// <summary>Dano do golpe. Se 'escalatingDamage' (cobra), acertar o MESMO inimigo seguidas vezes aumenta o dano.</summary>
    private int ComputeDamage(EnemyController enemy)
    {
        if (!escalatingDamage) return attackDamage;
        if (enemy == lastHitEnemy) escalateStacks++;
        else { lastHitEnemy = enemy; escalateStacks = 0; }
        int bonus = Mathf.Min(escalateStacks * escalateBonusPerHit, escalateMaxBonus);
        return attackDamage + bonus;
    }

    private EnemyController FindNearestEnemy()
    {
        var list = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        EnemyController best = null;
        float bestD = detectionRange;
        foreach (var e in list)
        {
            if (e == null || e.CurrentState == EnemyState.Dead) continue;
            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d <= bestD) { bestD = d; best = e; }
        }
        return best;
    }

    private void HandleHurt(int dmg)
    {
        if (dead) return;
        PlayOnce(hurtState);
        hurtLockTimer = HurtLockSeconds;     // trava no Hurt um tiquinho (anim aparece)
    }

    private void HandleDeath()
    {
        dead = true;
        hurtLockTimer = 0f;
        ActiveList.Remove(this);
        StopAllCoroutines();
        PlayOnce(deathState);
        // NAO destroi: o corpo fica na cena.
    }

    public void Shout()
    {
        string text = Resolve(shoutLine, shoutLocalizationKey);
        if (!string.IsNullOrEmpty(text)) StartCoroutine(ShoutRoutine(text));
    }

    /// <summary>Mostra uma fala arbitraria no balao (ex.: pedido de socorro preso na jaula).</summary>
    public void Say(string line)
    {
        if (!string.IsNullOrEmpty(line) && isActiveAndEnabled) StartCoroutine(ShoutRoutine(line));
    }

    private void ShoutFarewell()
    {
        string text = Resolve(farewellLine, farewellLocalizationKey);
        if (!string.IsNullOrEmpty(text)) StartCoroutine(ShoutRoutine(text));
    }

    private IEnumerator ShoutRoutine(string text)
    {
        if (string.IsNullOrEmpty(text)) yield break;

        var sb = SpeechBubble.Create(transform, new Vector3(0f, 3.0f, 0f), 1000, 3.2f, 8f,
                                     new Color(1f, 0.97f, 0.86f), true);
        sb.SetText(text);

        yield return new WaitForSeconds(3.0f);

        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            if (sb != null) sb.SetAlpha(1f - t / 0.5f);
            yield return null;
        }
        if (sb != null) Destroy(sb.gameObject);
    }

    private string Resolve(string literal, string key)
    {
        if (!string.IsNullOrEmpty(key))
        {
            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                string s = loc.GetText(key);
                if (!string.IsNullOrEmpty(s) && !s.StartsWith("[")) return s;
            }
        }
        return literal;
    }

    // --- helpers de anim/facing ---
    private void FaceX(float targetX)
    {
        float dx = targetX - transform.position.x;
        if (Mathf.Abs(dx) < 0.02f) return;
        bool right = dx > 0f;
        if (sr != null) sr.flipX = spriteFacesRight ? !right : right;
    }

    private void Play(string state)
    {
        if (anim == null || string.IsNullOrEmpty(state) || currentAnim == state) return;
        currentAnim = state;
        anim.Play(state, 0, 0f);
    }

    private void PlayOnce(string state)
    {
        if (anim == null || string.IsNullOrEmpty(state)) return;
        currentAnim = state;
        anim.Play(state, 0, 0f);
    }
}
