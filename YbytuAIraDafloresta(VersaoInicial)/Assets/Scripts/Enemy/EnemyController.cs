using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Controlador base de IA para inimigos.
/// Maquina de estados: Idle -> Patrol -> Chase -> Attack -> Cooldown -> Chase
/// Configuracoes vem do EnemyData (ScriptableObject).
/// Para inimigos com comportamento radicalmente diferente, criar script que herda deste.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(HealthSystem))]
public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("Configuracao")]
    [SerializeField] private EnemyData data;
    [SerializeField] private EnemySkin skin;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private HealthSystem health;

    private EnemyState currentState = EnemyState.Idle;
    private Transform playerTarget;
    private Vector2 patrolOrigin;
    private Vector2 patrolTarget;
    private float stateTimer;
    private float attackCooldownTimer;

    // Variacao por instancia para que inimigos parecam mais naturais.
    private float personalSpeedMultiplier = 1f;

    // Offset vertical aplicado ao alvo de chase (e ao spawn position). Inicializado
    // pela skin.feetYOffset em ApplySkin, mas pode ser sobrescrito em runtime pela
    // EnemyTestZone pra calibragem ao vivo.
    [System.NonSerialized] public float runtimeYOffset = 0f;

    // Garante que a correcao de pe (lift do transform) so seja aplicada uma vez.
    private bool feetAligned;

    // Quando nao tem slot de ataque, fica circulando o player nesta posicao.
    private Vector2 waitPosition;
    private float waitRefreshTimer;
    private bool hasAttackSlot;
    private int currentSlotIndex = -1;

    // --- Alvos aliados opcionais (ex.: javalis na CombatZone3) ---
    // Desligado por padrao (allyAggroChance = 0): CurrentTarget eh sempre o player e o
    // comportamento fica identico ao normal. Quando ligado (pelo BoarRescue), o inimigo
    // pode, ao reavaliar, mirar um aliado em vez do player por alguns segundos.
    [System.NonSerialized] public float allyAggroChance = 0f;
    [System.NonSerialized] public System.Collections.Generic.List<HealthSystem> allyTargets;
    [System.NonSerialized] public int allyAttackDamage = 0; // 0 = usa data.attackDamage
    private HealthSystem aggroAlly;     // != null quando o alvo atual eh um aliado
    private float aggroAllyTimer;
    private float allyRollTimer;
    private bool damageFromAlly;        // true durante DamageFromAlly (nao conta no combo)
    private int allyHitStreak;          // golpes seguidos levados de um aliado (regra dos 3 -> revida)
    private float allyHitStreakTime;
    private const int AllyHitsToAggro = 3;

    private Transform Target => aggroAlly != null ? aggroAlly.transform : playerTarget;

    // Combo-break: ao tomar N golpes seguidos, o inimigo sai do stun, telegrafa
    // (brilho vermelho + "!") e revida. Chefe = tiro em linha; normal = contra-ataque melee.
    private int consecutiveHits;
    private float lastHitTime;
    private bool retaliating;
    private const int HitsToBreakBoss = 8;
    private const int HitsToBreakNormal = 5;
    private const float ConsecutiveResetTime = 1.5f; // hits muito espacados nao contam como "seguidos"

    private int HitsToBreak => (data != null && data.isBoss) ? HitsToBreakBoss : HitsToBreakNormal;

    // --- Controle da luta final (FinalBossEncounter) ---
    // Tiro especial do chefe: comeca LIBERADO (preserva o comportamento padrao do boss em
    // qualquer outro uso). A luta final trava no inicio e destrava em 50% da vida somada.
    [System.NonSerialized] public bool gunUnlocked = true;
    // Recuo individual do chefe (pausa a IA so DESTE inimigo, diferente do CombatFrozen global).
    private bool combatPaused;
    // Ignora dano enquanto recuado (fora da camera).
    private bool invulnerable;
    public bool CombatPaused => combatPaused;
    public bool IsAlive => currentState != EnemyState.Dead;

    // Tiro PROATIVO do chefe (quando gunUnlocked): atira a distancia periodicamente,
    // sem depender da regra de revide por golpes seguidos.
    private float shootTimer;
    private const float BossShootInterval = 2.5f;
    private const float BossShootRange = 11f;
    private const float BossShootLaneTol = 1.6f;

    // Chefes ativos: pra se ESPALHAREM (cercar o player de lados opostos) em vez de empilhar.
    private static readonly System.Collections.Generic.List<EnemyController> ActiveBosses = new System.Collections.Generic.List<EnemyController>();
    private const float BossSeparation = 4.5f;

    /// <summary>Pausa/retoma a IA so deste inimigo (recuo do chefe na luta final).</summary>
    public void SetCombatPaused(bool value)
    {
        combatPaused = value;
        if (value) { if (rb != null) rb.linearVelocity = Vector2.zero; retaliating = false; }
        else if (currentState != EnemyState.Dead) ChangeState(EnemyState.Chase);
    }

    /// <summary>Liga/desliga invulnerabilidade (chefe recuado pra fora da camera).</summary>
    public void SetInvulnerable(bool value) => invulnerable = value;

    /// <summary>Dirige a animacao de andar/correr enquanto a IA esta pausada (recuo/volta do chefe).</summary>
    public void SetMoveAnimSpeed(float speed)
    {
        if (animator != null) animator.SetFloat(SpeedHash, speed);
    }

    // Posicoes relativas ao player onde cada slot de atacante se posiciona.
    // Index 0..3 cobre os 4 lados, com leve variacao vertical pra evitar empilhamento exato.
    // X menor que o attackRange minimo dos enemies (1.2) pra que o chase pare dentro do range
    // de ataque com folga (stopDistance de 0.1 pode passar pra ambos os lados do target).
    private static readonly Vector2[] SlotOffsets = {
        new Vector2( 1.0f,  0.0f),
        new Vector2(-1.0f,  0.0f),
        new Vector2( 1.0f,  0.5f),
        new Vector2(-1.0f,  0.5f),
    };

    // Animator hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int JabHash = Animator.StringToHash("Jab");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");

    public EnemyData Data => data;
    public EnemyState CurrentState => currentState;
    public event Action<int> OnEnemyDied; // scoreValue ja multiplicado pelo combo

    /// <summary>
    /// Disparado para qualquer inimigo que morre, com o score final
    /// (scoreValue do EnemyData multiplicado pelo combo do player).
    /// Use para acumular score sem precisar instanciar listeners por inimigo.
    /// </summary>
    public static event Action<int> OnAnyEnemyDied;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        health = GetComponent<HealthSystem>();
        if (data != null) health.SetMaxHealth(data.maxHealth);

        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        patrolOrigin = transform.position;
        personalSpeedMultiplier = UnityEngine.Random.Range(0.85f, 1.15f);

        ApplySkin();
    }

    protected virtual void OnEnable()
    {
        health.OnDeath += HandleDeath;
        health.OnDamageTaken += HandleDamageTaken;
        TryBarkOnSpawn();
        if (data != null && data.isBoss && !ActiveBosses.Contains(this)) ActiveBosses.Add(this);
    }

    private bool spawnBarked;

    /// <summary>Grito ao surgir na arena (chefes). Sorteia uma frase de spawnBarks. Uma vez.</summary>
    private void TryBarkOnSpawn()
    {
        if (spawnBarked || data == null || data.spawnBarks == null || data.spawnBarks.Length == 0) return;
        spawnBarked = true;
        string line = data.spawnBarks[UnityEngine.Random.Range(0, data.spawnBarks.Length)];
        EnemyBark.Spawn(transform.position + Vector3.up * 2.4f, line);
    }

    protected virtual void OnDisable()
    {
        health.OnDeath -= HandleDeath;
        health.OnDamageTaken -= HandleDamageTaken;
        ReleaseAttackSlot();
        ActiveBosses.Remove(this);
    }

    /// <summary>Congela TODOS os inimigos (ex.: enquanto a arara fala uma dica). Eles nao avancam.</summary>
    public static bool CombatFrozen;

    protected virtual void Update()
    {
        if (currentState == EnemyState.Dead) return;
        if (CombatFrozen || combatPaused) return; // parado: dica da arara (global) ou recuo do chefe (individual)

        FindPlayer();
        UpdateState();
        UpdateAnimations();
    }

    protected virtual void FixedUpdate()
    {
        if (currentState == EnemyState.Dead) return;
        if (CombatFrozen || combatPaused) { rb.linearVelocity = Vector2.zero; return; }
        if (retaliating) { rb.linearVelocity = Vector2.zero; return; }

        switch (currentState)
        {
            case EnemyState.Patrol:
                MoveTowards(patrolTarget);
                break;
            case EnemyState.Chase:
                if (aggroAlly != null)
                {
                    // mira direto no aliado, alinhando PE-COM-PE (aliado baixinho)
                    MoveTowards(AllyChasePoint());
                }
                else if (playerTarget != null)
                {
                    Vector2 target;
                    if (hasAttackSlot && currentSlotIndex >= 0 && currentSlotIndex < SlotOffsets.Length)
                        target = (Vector2)playerTarget.position + SlotOffsets[currentSlotIndex];
                    else
                        target = GetWaitPosition();
                    target.y += runtimeYOffset;
                    if (data.isBoss) target += BossSeparationOffset(); // chefes se espalham e cercam
                    MoveTowards(target);
                }
                break;
            default:
                rb.linearVelocity = Vector2.zero;
                break;
        }
    }

    /// <summary>
    /// Posicao em torno do player onde o inimigo fica circulando enquanto
    /// nao tem slot de ataque. Atualizada periodicamente para gerar movimento natural.
    /// </summary>
    private Vector2 GetWaitPosition()
    {
        waitRefreshTimer -= Time.fixedDeltaTime;
        if (waitRefreshTimer <= 0f || waitPosition == Vector2.zero)
        {
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float dist = UnityEngine.Random.Range(2f, 3.5f);
            waitPosition = (Vector2)playerTarget.position
                + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
            waitRefreshTimer = UnityEngine.Random.Range(1.2f, 2.5f);
        }
        return waitPosition;
    }

    private void UpdateAnimations()
    {
        float speed = rb.linearVelocity.magnitude;
        animator.SetFloat(SpeedHash, speed);
    }

    // --- State Machine ---

    protected virtual void UpdateState()
    {
        if (retaliating) return; // o tiro do chefe controla o estado durante a retaliacao

        stateTimer += Time.deltaTime;
        attackCooldownTimer -= Time.deltaTime;

        switch (currentState)
        {
            case EnemyState.Idle:
                UpdateIdle();
                break;
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyState.Attack:
                UpdateAttack();
                break;
            case EnemyState.Cooldown:
                UpdateCooldown();
                break;
            case EnemyState.Hurt:
                UpdateHurt();
                break;
        }
    }

    protected virtual void UpdateIdle()
    {
        if (IsPlayerInRange(data.detectionRange))
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        if (stateTimer >= data.patrolWaitTime)
        {
            PickPatrolTarget();
            ChangeState(EnemyState.Patrol);
        }
    }

    protected virtual void UpdatePatrol()
    {
        if (IsPlayerInRange(data.detectionRange))
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        if (Vector2.Distance(transform.position, patrolTarget) < 0.2f)
            ChangeState(EnemyState.Idle);
    }

    protected virtual void UpdateChase()
    {
        UpdateAllyAggro();

        // --- Mirando um aliado (javali): persegue direto, sem o anel de slots ---
        if (aggroAlly != null)
        {
            ReleaseAttackSlot();
            if (Vector2.Distance(transform.position, aggroAlly.transform.position) > data.loseTargetRange)
            {
                aggroAlly = null; // aliado longe demais: volta a mirar o player
                return;
            }
            if (IsAllyInAttackRange() && attackCooldownTimer <= 0f)
                ChangeState(EnemyState.Attack);
            return;
        }

        // --- Comportamento normal (player) ---
        if (playerTarget == null || !IsPlayerInRange(data.loseTargetRange))
        {
            ReleaseAttackSlot();
            ChangeState(EnemyState.Idle);
            return;
        }

        // Chefe com tiro liberado (a partir de 50%): atira a distancia de vez em quando.
        if (data.isBoss && gunUnlocked && !retaliating && TryRangedShot())
            return;

        // Tenta reservar slot. Slot >= 0 = pode atacar e tem posicao no anel;
        // slot < 0 = circula esperando vez.
        var coord = EnemyAttackCoordinator.Instance;
        if (coord != null)
        {
            int slot = coord.TryReserveSlot(this);
            hasAttackSlot = slot >= 0;
            currentSlotIndex = slot;
        }

        if (hasAttackSlot && IsPlayerInAttackRange() && attackCooldownTimer <= 0f)
            ChangeState(EnemyState.Attack);
    }

    /// <summary>
    /// Decide (com pequena chance) se o inimigo deve mirar um aliado (javali) por uns
    /// segundos em vez do player. So faz algo se allyAggroChance > 0 (ligado pelo BoarRescue).
    /// </summary>
    private void UpdateAllyAggro()
    {
        if (aggroAlly != null)
        {
            aggroAllyTimer -= Time.deltaTime;
            if (aggroAllyTimer <= 0f || aggroAlly.IsDead) aggroAlly = null;
        }
        if (aggroAlly != null) return;

        var active = AllyCreature.Active;
        bool hasActive = active != null && active.Count > 0;
        if (!hasActive && (allyAggroChance <= 0f || allyTargets == null)) return;

        allyRollTimer -= Time.deltaTime;
        if (allyRollTimer > 0f) return;
        allyRollTimer = UnityEngine.Random.Range(1.5f, 3f);

        // Aliados ATIVOS (animais libertos): rola por bicho pelo peso de ameaca (aggroWeight).
        // Onça ~0.5 (50/50 com o player), cobra ~0.1 (focada raramente), javali ~0.25.
        if (hasActive)
        {
            for (int i = 0; i < active.Count; i++)
            {
                var ac = active[i];
                if (ac == null || ac.IsDead) continue;
                if (UnityEngine.Random.value < ac.aggroWeight)
                {
                    aggroAlly = ac.Health;
                    aggroAllyTimer = UnityEngine.Random.Range(2f, 3.5f);
                    ReleaseAttackSlot();
                    return;
                }
            }
            return;
        }

        // Fallback (allyTargets + allyAggroChance manuais, sem AllyCreature ativo).
        if (UnityEngine.Random.value < allyAggroChance)
        {
            var ally = NearestAlly();
            if (ally != null)
            {
                aggroAlly = ally;
                aggroAllyTimer = UnityEngine.Random.Range(2f, 3.5f);
                ReleaseAttackSlot();
            }
        }
    }

    private HealthSystem NearestAlly()
    {
        HealthSystem best = null;
        float bestD = float.MaxValue;
        foreach (var a in allyTargets)
        {
            if (a == null || a.IsDead) continue;
            float d = Vector2.Distance(transform.position, a.transform.position);
            if (d < bestD) { bestD = d; best = a; }
        }
        return best;
    }

    /// <summary>Da pra acertar o aliado? So checa proximidade horizontal (centro do sprite);
    /// o aliado eh baixinho, entao uma checagem de Y apertada fazia o golpe passar por cima.</summary>
    private bool IsAllyInAttackRange()
    {
        if (aggroAlly == null) return false;
        var bsr = aggroAlly.GetComponentInChildren<SpriteRenderer>();
        float bx = (bsr != null && bsr.sprite != null) ? bsr.bounds.center.x : aggroAlly.transform.position.x;
        return Mathf.Abs(bx - transform.position.x) <= data.attackRange * 1.6f;
    }

    /// <summary>
    /// O golpe encosta no hurtbox (collider) do aliado? Monta uma caixa de ataque na frente
    /// do inimigo e checa interseccao com o collider do bicho ("encostou = acertou").
    /// </summary>
    /// <summary>Ponto pra perseguir o aliado: centro X dele, alinhado PE-COM-PE (resolve a altura).</summary>
    private Vector2 AllyChasePoint()
    {
        var bsr = aggroAlly.GetComponentInChildren<SpriteRenderer>();
        float bx = (bsr != null && bsr.sprite != null) ? bsr.bounds.center.x : aggroAlly.transform.position.x;
        float by;
        if (bsr != null && bsr.sprite != null && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            float myFootGap = transform.position.y - spriteRenderer.bounds.min.y;
            by = bsr.bounds.min.y + myFootGap;
        }
        else by = aggroAlly.transform.position.y + runtimeYOffset;
        return new Vector2(bx, by);
    }

    protected virtual void UpdateAttack()
    {
        // Aguardar a transition do Animator entrar no state de ataque.
        // Sem essa janela minima, stateInfo ainda reporta Idle/Walk no
        // primeiro frame, IsTag("Attack") falha, e Cooldown e disparado
        // antes do Animation Event de impacto chegar (frame de impacto perdido).
        const float minAttackDuration = 0.2f;
        if (stateTimer < minAttackDuration) return;

        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.IsTag("Attack"))
            ChangeState(EnemyState.Cooldown);
    }

    protected virtual void UpdateCooldown()
    {
        if (stateTimer >= data.attackCooldown)
            ChangeState(EnemyState.Chase);
    }

    protected virtual void UpdateHurt()
    {
        // Mantem o stagger durante todo o hurtRecoveryTime: inimigo nao
        // anda, nao ataca e nao empurra o player enquanto esta apanhando.
        if (stateTimer >= data.hurtRecoveryTime)
            ChangeState(EnemyState.Chase);
    }

    protected void ChangeState(EnemyState newState)
    {
        currentState = newState;
        stateTimer = 0f;

        // Liberar slot ao sair da janela ativa de ataque,
        // permitindo que outro inimigo entre no lugar.
        if (newState != EnemyState.Attack && newState != EnemyState.Chase)
            ReleaseAttackSlot();

        switch (newState)
        {
            case EnemyState.Attack:
                FaceTarget();
                PerformAttack();
                break;
            case EnemyState.Cooldown:
                // Apos atacar, cede a vez para outros se aproximarem.
                ReleaseAttackSlot();
                break;
        }
    }

    private void FacePlayer()
    {
        if (playerTarget == null) return;
        float dx = playerTarget.position.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.01f) return;
        // Player a direita: queremos sprite olhando pra direita.
        //  - Sprite default faces right: flipX = false.
        //  - Sprite default faces left:  flipX = true.
        bool playerRight = dx > 0f;
        spriteRenderer.flipX = playerRight ? !DefaultFacesRight : DefaultFacesRight;
    }

    /// <summary>Vira o sprite pra um X do mundo (usado externamente, ex.: encenacao do BoarRescue).</summary>
    public void FaceTowards(float worldX)
    {
        if (spriteRenderer == null) return;
        float dx = worldX - transform.position.x;
        if (Mathf.Abs(dx) < 0.01f) return;
        bool right = dx > 0f;
        spriteRenderer.flipX = right ? !DefaultFacesRight : DefaultFacesRight;
    }

    /// <summary>Vira pro alvo atual (player ou aliado).</summary>
    private void FaceTarget()
    {
        var t = Target;
        if (t == null) return;
        float dx = t.position.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.01f) return;
        bool right = dx > 0f;
        spriteRenderer.flipX = right ? !DefaultFacesRight : DefaultFacesRight;
    }

    private void ReleaseAttackSlot()
    {
        if (!hasAttackSlot) return;
        hasAttackSlot = false;
        currentSlotIndex = -1;
        var coord = EnemyAttackCoordinator.Instance;
        coord?.ReleaseSlot(this);
        // Forca novo waitPosition no proximo frame.
        waitRefreshTimer = 0f;
    }

    // --- Acoes ---

    protected virtual void PerformAttack()
    {
        rb.linearVelocity = Vector2.zero;

        // Sorteia variante de ataque. Triggers extras (Jab, Kick) acionam states
        // ja existentes em PlayerBase.controller - o AnimatorOverrideController
        // do inimigo mapeia esses slots pros clips de Attack2/Attack3/Shot/Recharge.
        string trigger = "Attack";
        if (data.attackTriggers != null && data.attackTriggers.Length > 0)
            trigger = data.attackTriggers[UnityEngine.Random.Range(0, data.attackTriggers.Length)];

        animator.SetTrigger(trigger);
        attackCooldownTimer = data.attackCooldown;

        // Ataque normal sempre toca soco. O som de tiro fica exclusivo do tiro em linha
        // (FireLineShot), pra nao parecer que todo golpe do chefe e um disparo.
        var sm = SoundManager.Instance;
        if (sm != null && sm.Library != null && sm.Library.enemyPunch != null)
            sm.PlaySFX(sm.Library.enemyPunch);
    }

    /// <summary>
    /// Chamado via Animation Event no frame de impacto do ataque.
    /// </summary>
    public void OnAttackHit()
    {
        // Se foi interrompido por Hurt/Dead antes do frame de impacto, ignorar.
        if (currentState != EnemyState.Attack) return;

        // Javalis (aliados) na FRENTE do golpe levam dano via OverlapBox no HURTBOX deles
        // (mesmo esquema do golpe do player nos inimigos). Pega mirando o javali ou nao.
        DamageAlliesInFront();

        // Player: leva o golpe se o inimigo NAO estiver focado num aliado.
        if (aggroAlly == null && playerTarget != null && IsPlayerInAttackRange(1.5f))
        {
            var playerCombat = playerTarget.GetComponent<PlayerCombatManager>();
            if (playerCombat != null)
                playerCombat.ReceiveDamage(data.attackDamage);
        }
    }

    /// <summary>
    /// Aplica o golpe do inimigo nos hurtboxes dos aliados (javalis) que estiverem na caixa
    /// de ataque a frente. Usa OverlapBox no collider, como o ataque do player faz nos inimigos.
    /// </summary>
    private void DamageAlliesInFront()
    {
        if (spriteRenderer == null) return;
        bool facingRight = DefaultFacesRight ? !spriteRenderer.flipX : spriteRenderer.flipX;
        float facing = facingRight ? 1f : -1f;

        Vector2 center = new Vector2(transform.position.x + facing * data.attackRange,
                                     transform.position.y + 1.0f);
        Vector2 size = new Vector2(data.attackRange * 2.6f, 3.4f); // alto pra cobrir o hurtbox do bicho

        int dmg = allyAttackDamage > 0 ? allyAttackDamage : data.attackDamage;
        var hits = Physics2D.OverlapBoxAll(center, size, 0f);
        foreach (var h in hits)
        {
            if (h == null) continue;
            var ally = h.GetComponentInParent<AllyCreature>();
            if (ally == null || ally.IsDead) continue;
            var hp = ally.GetComponent<HealthSystem>();
            if (hp != null) hp.TakeDamage(dmg);
        }
    }

    private void HandleDamageTaken(int damage)
    {
        if (currentState == EnemyState.Dead) return;

        // Chefe: conta golpes seguidos. Ao atingir o limite, revida em vez de levar stun.
        bool willRetaliate = false;
        if (data != null && !retaliating)
        {
            if (Time.time - lastHitTime > ConsecutiveResetTime) consecutiveHits = 0;
            lastHitTime = Time.time;
            consecutiveHits++;
            if (consecutiveHits >= HitsToBreak)
            {
                consecutiveHits = 0;
                willRetaliate = true;
            }
        }

        // Stun normal, exceto quando ja esta revidando ou vai revidar neste golpe.
        if (!retaliating && !willRetaliate)
        {
            ChangeState(EnemyState.Hurt);
            animator.SetTrigger(HurtHash);
            // Janela de recuperacao: nao ataca imediatamente apos ser atingido,
            // permitindo que o player encadeie golpes para subir o combo.
            attackCooldownTimer = Mathf.Max(attackCooldownTimer, data.hurtRecoveryTime);
        }

        var sm = SoundManager.Instance;
        if (sm != null && sm.Library != null)
            sm.PlaySFX(sm.Library.enemyHurt);

        // Registra o hit no combo do player (sem texto flutuante de dano, que era debug).
        // Dano vindo de um aliado (javali) NAO conta no combo do player.
        if (playerTarget != null && !damageFromAlly)
        {
            var combo = playerTarget.GetComponent<ComboSystem>();
            if (combo != null) combo.RegisterHit();
        }

        if (willRetaliate)
            StartCoroutine(RetaliateRoutine());
    }

    // --- Combo-break: retaliacao apos N golpes seguidos ---

    /// <summary>
    /// Chefe (gunUnlocked) atira a distancia: so quando o player esta em media distancia
    /// (nem colado, nem longe demais) e na mesma lane. Retorna true se iniciou o tiro.
    /// </summary>
    private bool TryRangedShot()
    {
        shootTimer -= Time.deltaTime;
        if (shootTimer > 0f || playerTarget == null) return false;

        float dx = Mathf.Abs(playerTarget.position.x - transform.position.x);
        float dy = Mathf.Abs(playerTarget.position.y - transform.position.y);
        if (dx > BossShootRange || dx < data.attackRange * 1.2f || dy > BossShootLaneTol)
            return false;

        shootTimer = BossShootInterval;
        ReleaseAttackSlot();
        StartCoroutine(ShootRoutine());
        return true;
    }

    /// <summary>Sequencia do tiro proativo (telegrafo + anim Shot + tiro em linha).</summary>
    private IEnumerator ShootRoutine()
    {
        retaliating = true; // reusa o guard: pausa movimento/estado enquanto atira
        rb.linearVelocity = Vector2.zero;
        FacePlayer();

        float telegraph = 0.45f;
        var fx = RetaliateTelegraph.Spawn(transform, spriteRenderer, telegraph);
        yield return new WaitForSeconds(telegraph);
        if (currentState == EnemyState.Dead) { if (fx != null) fx.Stop(); retaliating = false; yield break; }

        FacePlayer();
        if (animator != null) animator.SetTrigger(JabHash); // Jab -> clip Shot no override
        TryShotBark();
        yield return new WaitForSeconds(0.15f);
        if (currentState == EnemyState.Dead) { retaliating = false; yield break; }
        FireLineShot();

        yield return new WaitForSeconds(0.3f);
        retaliating = false;
        if (currentState == EnemyState.Dead) yield break;
        attackCooldownTimer = data.attackCooldown;
        ChangeState(EnemyState.Chase);
    }

    private IEnumerator RetaliateRoutine()
    {
        retaliating = true;
        rb.linearVelocity = Vector2.zero;
        FacePlayer();

        // Telegrafo: brilho vermelho nas bordas + "!" grande. Janela pro player recuar.
        float telegraph = (data != null && data.isBoss) ? 0.5f : 0.4f;
        var fx = RetaliateTelegraph.Spawn(transform, spriteRenderer, telegraph);
        yield return new WaitForSeconds(telegraph);

        if (currentState == EnemyState.Dead)
        {
            if (fx != null) fx.Stop();
            retaliating = false;
            yield break;
        }

        FacePlayer();
        if (data != null && data.isBoss && gunUnlocked)
        {
            // Especial do chefe: animacao de tiro (trigger Jab -> clip Shot no override) + tiro em linha.
            if (animator != null) animator.SetTrigger(JabHash);
            TryShotBark();
            yield return new WaitForSeconds(0.15f); // deixa a arma subir antes do disparo
            if (currentState == EnemyState.Dead) { retaliating = false; yield break; }
            FireLineShot();
        }
        else
        {
            if (animator != null) animator.SetTrigger(AttackHash);
            MeleeCounter();
        }

        yield return new WaitForSeconds(0.3f); // recuperacao
        retaliating = false;
        if (currentState == EnemyState.Dead) yield break;
        attackCooldownTimer = data.attackCooldown;
        ChangeState(EnemyState.Chase);
    }

    /// <summary>Grito do chefe ao disparar o tiro em linha (sorteia de shotBarks).</summary>
    private void TryShotBark()
    {
        if (data == null || data.shotBarks == null || data.shotBarks.Length == 0) return;
        string line = data.shotBarks[UnityEngine.Random.Range(0, data.shotBarks.Length)];
        EnemyBark.Spawn(transform.position + Vector3.up * 2.4f, line);
    }

    /// <summary>Contra-ataque corpo-a-corpo do inimigo normal ao revidar.</summary>
    private void MeleeCounter()
    {
        var sm = SoundManager.Instance;
        if (sm != null && sm.Library != null && sm.Library.enemyPunch != null)
            sm.PlaySFX(sm.Library.enemyPunch);

        // So acerta se o player nao recuou durante o telegrafo.
        if (playerTarget != null && IsPlayerInAttackRange(1.3f))
        {
            var pc = playerTarget.GetComponent<PlayerCombatManager>();
            if (pc != null) pc.ReceiveDamage(data.attackDamage);
        }
    }

    /// <summary>
    /// Tiro hitscan horizontal (linha de 2px) na direcao do player, indo ate a borda
    /// da camera. Acerta o player se a lane dele cruzar a linha.
    /// </summary>
    private void FireLineShot()
    {
        float dir = 1f;
        if (playerTarget != null)
        {
            float dx = playerTarget.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.01f) dir = Mathf.Sign(dx);
        }

        // Altura do disparo: centro do sprite abaixado ~20% da altura, pra sair na altura
        // do corpo/arma (o centro puro cai na cabeca do chefe).
        float shotY = spriteRenderer != null
            ? spriteRenderer.bounds.center.y - spriteRenderer.bounds.size.y * 0.15f
            : transform.position.y;
        // Sai um pouco a frente do chefe (na direcao do tiro), nao de dentro do corpo.
        Vector2 origin = new Vector2(transform.position.x + dir * 1.0f, shotY);

        // Ponta na borda horizontal da camera.
        float edgeX = origin.x + dir * 20f;
        var cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            float halfW = cam.orthographicSize * cam.aspect;
            edgeX = cam.transform.position.x + dir * (halfW + 1f);
        }
        Vector2 endPos = new Vector2(edgeX, shotY);

        const float thickness = 2f / 32f; // 2 px a PPU 32

        // Hitscan: faixa fina horizontal entre o chefe e a borda. A hurtbox alta do
        // player e atingida quando a lane dele cruza a linha.
        Vector2 center = new Vector2((origin.x + edgeX) * 0.5f, shotY);
        float length = Mathf.Abs(edgeX - origin.x);
        var hits = Physics2D.OverlapBoxAll(center, new Vector2(length, thickness), 0f);
        foreach (var h in hits)
        {
            if (h == null) continue;
            var pc = h.GetComponentInParent<PlayerCombatManager>();
            if (pc != null)
            {
                pc.ReceiveDamage(data.attackDamage);
                break;
            }
        }

        var sm = SoundManager.Instance;
        if (sm != null && sm.Library != null && sm.Library.enemyGunshot != null)
            sm.PlaySFX(sm.Library.enemyGunshot);

        BossBeam.Spawn(origin, endPos, thickness);
    }

    private void HandleDeath()
    {
        currentState = EnemyState.Dead;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        ReleaseAttackSlot();

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        int finalScore = data.scoreValue;
        if (playerTarget != null)
        {
            var combo = playerTarget.GetComponent<ComboSystem>();
            if (combo != null)
                finalScore = Mathf.RoundToInt(data.scoreValue * combo.ScoreMultiplier);
        }

        OnEnemyDied?.Invoke(finalScore);
        OnAnyEnemyDied?.Invoke(finalScore);

        var sm = SoundManager.Instance;
        if (sm != null && sm.Library != null)
        {
            var deathClip = sm.Library.enemyDeath != null ? sm.Library.enemyDeath : sm.Library.enemyHurt;
            sm.PlaySFX(deathClip);
        }

        StartCoroutine(DeathFlashAndDestroy());
    }

    private IEnumerator DeathFlashAndDestroy()
    {
        const float duration = 0.8f;
        const float interval = 0.08f;
        float elapsed = 0f;
        bool visible = true;
        Color baseColor = spriteRenderer.color;

        while (elapsed < duration)
        {
            visible = !visible;
            spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, visible ? baseColor.a : 0f);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        Destroy(gameObject);
    }

    // --- Helpers ---

    private bool DefaultFacesRight => skin == null || skin.defaultFacesRight;

    /// <summary>
    /// Empurra o destino de perseguição pra LONGE do outro chefe, pra os dois nao empilharem
    /// e cercarem o player de lados opostos. Vies horizontal (pincer) reforcado.
    /// </summary>
    private Vector2 BossSeparationOffset()
    {
        Vector2 push = Vector2.zero;
        foreach (var other in ActiveBosses)
        {
            if (other == this || other == null || !other.IsAlive) continue;
            Vector2 d = (Vector2)transform.position - (Vector2)other.transform.position;
            float dist = d.magnitude;
            if (dist < BossSeparation && dist > 0.01f)
            {
                float strength = (BossSeparation - dist);
                // reforca a componente horizontal pra virar cerco (um de cada lado)
                push += new Vector2(d.normalized.x * strength * 1.6f, d.normalized.y * strength * 0.5f);
            }
        }
        return push;
    }

    private void MoveTowards(Vector2 target)
    {
        Vector2 toTarget = target - (Vector2)transform.position;
        float distance = toTarget.magnitude;

        // Em chase direto ao player (com slot), para um pouco antes pra nao empurrar.
        // Circulando (sem slot), para mais longe pra dar espaco aos atacantes ativos.
        // Patrol/outros alvos: para no destino.
        bool chasingAlly = currentState == EnemyState.Chase && aggroAlly != null;
        bool chasingPlayer = currentState == EnemyState.Chase && hasAttackSlot
                             && playerTarget != null
                             && (Vector2)playerTarget.position == target;
        bool circling = currentState == EnemyState.Chase && !hasAttackSlot && !chasingAlly;
        float stopDistance = (chasingPlayer || chasingAlly) ? data.attackRange * 0.8f
                            : circling ? 0.5f
                            : 0.1f;

        if (distance <= stopDistance)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = toTarget / distance;
        // Circulando: anda um pouco mais devagar pra dar sensacao de espera.
        float speed = data.moveSpeed * personalSpeedMultiplier * (circling ? 0.7f : 1f);
        rb.linearVelocity = direction * speed;

        // Flip sprite: respeitar orientacao default da skin.
        // Sprite "default faces right": flipX=false olha pra direita; entao
        // movendo pra direita = flipX false. Sprite "default faces left":
        // flipX=false olha pra esquerda; entao movendo pra direita = flipX true.
        if (direction.x > 0.01f) spriteRenderer.flipX = !DefaultFacesRight;
        else if (direction.x < -0.01f) spriteRenderer.flipX = DefaultFacesRight;
    }

    private void FindPlayer()
    {
        if (playerTarget != null) return;
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTarget = player.transform;
            // Auto-alinhamento de pes assim que achamos o player. Compara a base do CapsuleCollider2D
            // do inimigo com a do player. Resolve a diferenca de pivo/proporcao do sprite sem
            // precisar tunar feetYOffset manualmente. Sobreposto pelo override da EnemyTestZone
            // (LateUpdate) quando o usuario quer ajustar ao vivo.
            ApplyAutoFeetAlignment();
        }
    }

    private void ApplyAutoFeetAlignment()
    {
        if (playerTarget == null) return;
        var enemyCap = GetComponent<CapsuleCollider2D>();
        var playerCap = playerTarget.GetComponent<CapsuleCollider2D>();
        if (enemyCap == null || playerCap == null) return;

        // Base do capsule (parte de baixo) em coords locais, depois multiplicado pelo scale Y
        // do transform (que ja foi setado em ApplySkin).
        float enemyFeet = (enemyCap.offset.y - enemyCap.size.y * 0.5f) * transform.localScale.y;
        float playerFeet = (playerCap.offset.y - playerCap.size.y * 0.5f) * playerTarget.localScale.y;

        float skinOffset = skin != null ? skin.feetYOffset : 0f;
        runtimeYOffset = skinOffset + (playerFeet - enemyFeet);

        // Reposiciona o transform pelos pes (uma vez so). Sprites "fundos" (pivo no topo,
        // com capsule/hurtbox em offset grande) afundam no chao quando ficam parados: a
        // hurtbox desce pra altura dos pes, abaixo do alcance do golpe do player, deixando
        // o inimigo quase impossivel de acertar. O chase ja soma runtimeYOffset ao alvo;
        // aqui aplicamos a mesma correcao ao transform e ao patrolOrigin pra que inimigos
        // iniciais (idle na arena) fiquem com os pes no chao e a hurtbox no corpo desde o
        // primeiro frame de combate.
        if (!feetAligned)
        {
            transform.position += Vector3.up * runtimeYOffset;
            patrolOrigin.y += runtimeYOffset;
            feetAligned = true;
        }
    }

    private bool IsPlayerInRange(float range)
    {
        if (playerTarget == null) return false;
        return Vector2.Distance(transform.position, playerTarget.position) <= range;
    }

    /// <summary>
    /// Beat'em up: alvo so esta atacavel se estiver dentro do range em X
    /// E numa lane Y proxima. Multiplier amplia o range (usado na hitbox do golpe).
    /// O Y compara as bases dos capsules (pe-com-pe) somando runtimeYOffset ao delta,
    /// pra que o inimigo "alinhado pelos pes" pelo auto-feet-alignment ainda conte
    /// como mesma lane mesmo com transform.y deslocado.
    /// </summary>
    private bool IsPlayerInAttackRange(float multiplier = 1f) => IsTargetInAttackRange(playerTarget, multiplier);

    private bool IsTargetInAttackRange(Transform t, float multiplier = 1f)
    {
        if (t == null) return false;
        Vector2 delta = (Vector2)t.position - (Vector2)transform.position;
        float adjustedDeltaY = delta.y + runtimeYOffset;
        return Mathf.Abs(delta.x) <= data.attackRange * multiplier
            && Mathf.Abs(adjustedDeltaY) <= data.attackYTolerance * multiplier;
    }

    private void PickPatrolTarget()
    {
        patrolTarget = patrolOrigin + UnityEngine.Random.insideUnitCircle * data.patrolRadius;
    }

    private void ApplySkin()
    {
        if (skin == null) return;

        if (skin.animationData != null && skin.animationData.animatorOverride != null)
            animator.runtimeAnimatorController = skin.animationData.animatorOverride;

        spriteRenderer.color = skin.tintColor;
        transform.localScale = new Vector3(skin.spriteScale.x, skin.spriteScale.y, 1f);
        runtimeYOffset = skin.feetYOffset;
    }

    /// <summary>
    /// Receber dano de uma fonte externa.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (invulnerable) return; // chefe recuado pra fora da camera: nao toma dano
        health.TakeDamage(damage);
    }

    /// <summary>
    /// Dano vindo de um ALIADO (ex.: javali). Aplica stun normal, mas NAO conta no combo
    /// do player (evita inflar o combo com golpes que nao foram do jogador).
    /// </summary>
    /// <summary>Forca o inimigo a mirar um aliado (javali) - usado em cena de teste.</summary>
    public void ForceAggroAlly(HealthSystem ally, float duration = 999f)
    {
        enabled = true;
        aggroAlly = ally;
        aggroAllyTimer = duration;
        ReleaseAttackSlot();
    }

    public void DamageFromAlly(int damage, HealthSystem source)
    {
        if (invulnerable) return; // chefe recuado: imune tambem ao dano de aliados
        damageFromAlly = true;
        health.TakeDamage(damage);
        damageFromAlly = false;

        // Regra dos 3: se o MESMO aliado acerta varias vezes seguidas, o inimigo para de
        // ignorar e comeca a revidar nele por um tempo (depois volta a priorizar o player).
        if (source == null || currentState == EnemyState.Dead) return;
        if (Time.time - allyHitStreakTime > 2.5f) allyHitStreak = 0;
        allyHitStreakTime = Time.time;
        allyHitStreak++;
        if (allyHitStreak >= AllyHitsToAggro)
        {
            allyHitStreak = 0;
            aggroAlly = source;
            aggroAllyTimer = UnityEngine.Random.Range(3.5f, 5.5f);
            ReleaseAttackSlot();
        }
    }
}
