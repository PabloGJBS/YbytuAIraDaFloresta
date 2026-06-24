using UnityEngine;
using System;
using System.Collections;

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

    private float personalSpeedMultiplier = 1f;

    [System.NonSerialized] public float runtimeYOffset = 0f;

    private bool feetAligned;

    private Vector2 waitPosition;
    private float waitRefreshTimer;
    private bool hasAttackSlot;
    private int currentSlotIndex = -1;

    [System.NonSerialized] public float allyAggroChance = 0f;
    [System.NonSerialized] public System.Collections.Generic.List<HealthSystem> allyTargets;
    [System.NonSerialized] public int allyAttackDamage = 0; // 0 = usa data.attackDamage
    private HealthSystem aggroAlly;
    private float aggroAllyTimer;
    private float allyRollTimer;
    private bool damageFromAlly;
    private int allyHitStreak;
    private float allyHitStreakTime;
    private const int AllyHitsToAggro = 3;

    private Transform Target => aggroAlly != null ? aggroAlly.transform : playerTarget;

    private int consecutiveHits;
    private float lastHitTime;
    private bool retaliating;
    private const int HitsToBreakBoss = 8;
    private const int HitsToBreakNormal = 5;
    private const float ConsecutiveResetTime = 1.5f;

    private int HitsToBreak => (data != null && data.isBoss) ? HitsToBreakBoss : HitsToBreakNormal;

    [System.NonSerialized] public bool gunUnlocked = true;
    private bool combatPaused;
    private bool invulnerable;
    public bool CombatPaused => combatPaused;
    public bool IsAlive => currentState != EnemyState.Dead;

    private float shootTimer;
    private const float BossShootInterval = 2.5f;
    private const float BossShootRange = 11f;
    private const float BossShootLaneTol = 1.6f;

    private static readonly System.Collections.Generic.List<EnemyController> ActiveBosses = new System.Collections.Generic.List<EnemyController>();
    private const float BossSeparation = 4.5f;

    public void SetCombatPaused(bool value)
    {
        combatPaused = value;
        if (value) { if (rb != null) rb.linearVelocity = Vector2.zero; retaliating = false; }
        else if (currentState != EnemyState.Dead) ChangeState(EnemyState.Chase);
    }

    public void SetInvulnerable(bool value) => invulnerable = value;

    public void SetMoveAnimSpeed(float speed)
    {
        if (animator != null) animator.SetFloat(SpeedHash, speed);
    }

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
    public event Action<int> OnEnemyDied;

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

    public static bool CombatFrozen;

    protected virtual void Update()
    {
        if (currentState == EnemyState.Dead) return;
        if (CombatFrozen || combatPaused) return;

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
        if (retaliating) return;

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

        if (aggroAlly != null)
        {
            ReleaseAttackSlot();
            if (Vector2.Distance(transform.position, aggroAlly.transform.position) > data.loseTargetRange)
            {
                aggroAlly = null;
                return;
            }
            if (IsAllyInAttackRange() && attackCooldownTimer <= 0f)
                ChangeState(EnemyState.Attack);
            return;
        }

        if (playerTarget == null || !IsPlayerInRange(data.loseTargetRange))
        {
            ReleaseAttackSlot();
            ChangeState(EnemyState.Idle);
            return;
        }

        if (data.isBoss && gunUnlocked && !retaliating && TryRangedShot())
            return;

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

    private bool IsAllyInAttackRange()
    {
        if (aggroAlly == null) return false;
        var bsr = aggroAlly.GetComponentInChildren<SpriteRenderer>();
        float bx = (bsr != null && bsr.sprite != null) ? bsr.bounds.center.x : aggroAlly.transform.position.x;
        return Mathf.Abs(bx - transform.position.x) <= data.attackRange * 1.6f;
    }

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
        if (stateTimer >= data.hurtRecoveryTime)
            ChangeState(EnemyState.Chase);
    }

    protected void ChangeState(EnemyState newState)
    {
        currentState = newState;
        stateTimer = 0f;

        if (newState != EnemyState.Attack && newState != EnemyState.Chase)
            ReleaseAttackSlot();

        switch (newState)
        {
            case EnemyState.Attack:
                FaceTarget();
                PerformAttack();
                break;
            case EnemyState.Cooldown:
                ReleaseAttackSlot();
                break;
        }
    }

    private void FacePlayer()
    {
        if (playerTarget == null) return;
        float dx = playerTarget.position.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.01f) return;
        bool playerRight = dx > 0f;
        spriteRenderer.flipX = playerRight ? !DefaultFacesRight : DefaultFacesRight;
    }

    public void FaceTowards(float worldX)
    {
        if (spriteRenderer == null) return;
        float dx = worldX - transform.position.x;
        if (Mathf.Abs(dx) < 0.01f) return;
        bool right = dx > 0f;
        spriteRenderer.flipX = right ? !DefaultFacesRight : DefaultFacesRight;
    }

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
        waitRefreshTimer = 0f;
    }

    // --- Acoes ---

    protected virtual void PerformAttack()
    {
        rb.linearVelocity = Vector2.zero;

        string trigger = "Attack";
        if (data.attackTriggers != null && data.attackTriggers.Length > 0)
            trigger = data.attackTriggers[UnityEngine.Random.Range(0, data.attackTriggers.Length)];

        animator.SetTrigger(trigger);
        attackCooldownTimer = data.attackCooldown;

        var sm = SoundManager.Instance;
        if (sm != null && sm.Library != null && sm.Library.enemyPunch != null)
            sm.PlaySFX(sm.Library.enemyPunch);
    }

    public void OnAttackHit()
    {
        if (currentState != EnemyState.Attack) return;

        DamageAlliesInFront();

        if (aggroAlly == null && playerTarget != null && IsPlayerInAttackRange(1.5f))
        {
            var playerCombat = playerTarget.GetComponent<PlayerCombatManager>();
            if (playerCombat != null)
                playerCombat.ReceiveDamage(data.attackDamage);
        }
    }

    private void DamageAlliesInFront()
    {
        if (spriteRenderer == null) return;
        bool facingRight = DefaultFacesRight ? !spriteRenderer.flipX : spriteRenderer.flipX;
        float facing = facingRight ? 1f : -1f;

        Vector2 center = new Vector2(transform.position.x + facing * data.attackRange,
                                     transform.position.y + 1.0f);
        Vector2 size = new Vector2(data.attackRange * 2.6f, 3.4f);

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

        if (!retaliating && !willRetaliate)
        {
            ChangeState(EnemyState.Hurt);
            animator.SetTrigger(HurtHash);
            attackCooldownTimer = Mathf.Max(attackCooldownTimer, data.hurtRecoveryTime);
        }

        var sm = SoundManager.Instance;
        if (sm != null && sm.Library != null)
            sm.PlaySFX(sm.Library.enemyHurt);

        if (playerTarget != null && !damageFromAlly)
        {
            var combo = playerTarget.GetComponent<ComboSystem>();
            if (combo != null) combo.RegisterHit();
        }

        if (willRetaliate)
            StartCoroutine(RetaliateRoutine());
    }

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

    private IEnumerator ShootRoutine()
    {
        retaliating = true;
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
            if (animator != null) animator.SetTrigger(JabHash);
            TryShotBark();
            yield return new WaitForSeconds(0.15f);
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

    private void TryShotBark()
    {
        if (data == null || data.shotBarks == null || data.shotBarks.Length == 0) return;
        string line = data.shotBarks[UnityEngine.Random.Range(0, data.shotBarks.Length)];
        EnemyBark.Spawn(transform.position + Vector3.up * 2.4f, line);
    }

    private void MeleeCounter()
    {
        var sm = SoundManager.Instance;
        if (sm != null && sm.Library != null && sm.Library.enemyPunch != null)
            sm.PlaySFX(sm.Library.enemyPunch);

        if (playerTarget != null && IsPlayerInAttackRange(1.3f))
        {
            var pc = playerTarget.GetComponent<PlayerCombatManager>();
            if (pc != null) pc.ReceiveDamage(data.attackDamage);
        }
    }

    private void FireLineShot()
    {
        float dir = 1f;
        if (playerTarget != null)
        {
            float dx = playerTarget.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.01f) dir = Mathf.Sign(dx);
        }

        float shotY = spriteRenderer != null
            ? spriteRenderer.bounds.center.y - spriteRenderer.bounds.size.y * 0.15f
            : transform.position.y;
        Vector2 origin = new Vector2(transform.position.x + dir * 1.0f, shotY);

        float edgeX = origin.x + dir * 20f;
        var cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            float halfW = cam.orthographicSize * cam.aspect;
            edgeX = cam.transform.position.x + dir * (halfW + 1f);
        }
        Vector2 endPos = new Vector2(edgeX, shotY);

        const float thickness = 2f / 32f; // 2 px a PPU 32

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
                push += new Vector2(d.normalized.x * strength * 1.6f, d.normalized.y * strength * 0.5f);
            }
        }
        return push;
    }

    private void MoveTowards(Vector2 target)
    {
        Vector2 toTarget = target - (Vector2)transform.position;
        float distance = toTarget.magnitude;

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
        float speed = data.moveSpeed * personalSpeedMultiplier * (circling ? 0.7f : 1f);
        rb.linearVelocity = direction * speed;

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
            ApplyAutoFeetAlignment();
        }
    }

    private void ApplyAutoFeetAlignment()
    {
        if (playerTarget == null) return;
        var enemyCap = GetComponent<CapsuleCollider2D>();
        var playerCap = playerTarget.GetComponent<CapsuleCollider2D>();
        if (enemyCap == null || playerCap == null) return;

        float enemyFeet = (enemyCap.offset.y - enemyCap.size.y * 0.5f) * transform.localScale.y;
        float playerFeet = (playerCap.offset.y - playerCap.size.y * 0.5f) * playerTarget.localScale.y;

        float skinOffset = skin != null ? skin.feetYOffset : 0f;
        runtimeYOffset = skinOffset + (playerFeet - enemyFeet);

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

    public void TakeDamage(int damage)
    {
        if (invulnerable) return;
        health.TakeDamage(damage);
    }

    public void ForceAggroAlly(HealthSystem ally, float duration = 999f)
    {
        enabled = true;
        aggroAlly = ally;
        aggroAllyTimer = duration;
        ReleaseAttackSlot();
    }

    public void DamageFromAlly(int damage, HealthSystem source)
    {
        if (invulnerable) return;
        damageFromAlly = true;
        health.TakeDamage(damage);
        damageFromAlly = false;

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
