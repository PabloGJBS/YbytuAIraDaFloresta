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

    // Quando nao tem slot de ataque, fica circulando o player nesta posicao.
    private Vector2 waitPosition;
    private float waitRefreshTimer;
    private bool hasAttackSlot;
    private int currentSlotIndex = -1;

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
    }

    protected virtual void OnDisable()
    {
        health.OnDeath -= HandleDeath;
        health.OnDamageTaken -= HandleDamageTaken;
        ReleaseAttackSlot();
    }

    protected virtual void Update()
    {
        if (currentState == EnemyState.Dead) return;

        FindPlayer();
        UpdateState();
        UpdateAnimations();
    }

    protected virtual void FixedUpdate()
    {
        if (currentState == EnemyState.Dead) return;

        switch (currentState)
        {
            case EnemyState.Patrol:
                MoveTowards(patrolTarget);
                break;
            case EnemyState.Chase:
                if (playerTarget != null)
                {
                    Vector2 target;
                    if (hasAttackSlot && currentSlotIndex >= 0 && currentSlotIndex < SlotOffsets.Length)
                        target = (Vector2)playerTarget.position + SlotOffsets[currentSlotIndex];
                    else
                        target = GetWaitPosition();
                    target.y += runtimeYOffset;
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
        if (playerTarget == null || !IsPlayerInRange(data.loseTargetRange))
        {
            ReleaseAttackSlot();
            ChangeState(EnemyState.Idle);
            return;
        }

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
                FacePlayer();
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

        var sm = SoundManager.Instance;
        if (sm != null && sm.Library != null)
            sm.PlaySFX(sm.Library.enemyPunch);
    }

    /// <summary>
    /// Chamado via Animation Event no frame de impacto do ataque.
    /// </summary>
    public void OnAttackHit()
    {
        // Se foi interrompido por Hurt/Dead antes do frame de impacto, ignorar.
        if (currentState != EnemyState.Attack) return;
        if (playerTarget == null) return;
        if (!IsPlayerInAttackRange(1.5f)) return;

        var playerCombat = playerTarget.GetComponent<PlayerCombatManager>();
        if (playerCombat != null)
            playerCombat.ReceiveDamage(data.attackDamage);
    }

    private void HandleDamageTaken(int damage)
    {
        if (currentState == EnemyState.Dead) return;
        ChangeState(EnemyState.Hurt);
        animator.SetTrigger(HurtHash);

        // Janela de recuperacao: nao ataca imediatamente apos ser atingido,
        // permitindo que o player encadeie golpes para subir o combo.
        attackCooldownTimer = Mathf.Max(attackCooldownTimer, data.hurtRecoveryTime);

        var sm = SoundManager.Instance;
        if (sm != null && sm.Library != null)
            sm.PlaySFX(sm.Library.enemyHurt);

        // Texto flutuante de dano + cor do rank do combo.
        int rank = 0;
        float mult = 1f;
        if (playerTarget != null)
        {
            var combo = playerTarget.GetComponent<ComboSystem>();
            if (combo != null)
            {
                rank = (int)combo.CurrentRank;
                mult = combo.DamageMultiplier;
                combo.RegisterHit();
            }
        }
        FloatingDamageText.Spawn(transform.position + Vector3.up * 1.4f, damage, rank, mult);
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

    private void MoveTowards(Vector2 target)
    {
        Vector2 toTarget = target - (Vector2)transform.position;
        float distance = toTarget.magnitude;

        // Em chase direto ao player (com slot), para um pouco antes pra nao empurrar.
        // Circulando (sem slot), para mais longe pra dar espaco aos atacantes ativos.
        // Patrol/outros alvos: para no destino.
        bool chasingPlayer = currentState == EnemyState.Chase && hasAttackSlot
                             && playerTarget != null
                             && (Vector2)playerTarget.position == target;
        bool circling = currentState == EnemyState.Chase && !hasAttackSlot;
        float stopDistance = chasingPlayer ? data.attackRange * 0.8f
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
    private bool IsPlayerInAttackRange(float multiplier = 1f)
    {
        if (playerTarget == null) return false;
        Vector2 delta = (Vector2)playerTarget.position - (Vector2)transform.position;
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
        health.TakeDamage(damage);
    }
}
