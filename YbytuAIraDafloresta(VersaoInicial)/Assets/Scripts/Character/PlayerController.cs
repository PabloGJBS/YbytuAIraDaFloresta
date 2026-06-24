using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Pulo")]
    [SerializeField] private float jumpHeight = 0.8f;
    [SerializeField] private float jumpDuration = 0.4f;
    [Tooltip("Percentual do pulo onde ataques aereos sao permitidos (0.0 a 1.0)")]
    [SerializeField] private float airAttackWindow = 0.4f;

    [Header("Combate")]
    [SerializeField] private float attackCooldown = 0.3f;

    [Header("Knockback (ex.: atropelamento do chefe trator)")]
    [Tooltip("Amortecimento da velocidade de knockback ao longo do tempo.")]
    [SerializeField] private float knockbackDamping = 6f;

    [Header("Hurt / Invulnerabilidade")]
    [Tooltip("Tempo (s) totalmente travado apos receber dano: nao move nem ataca.")]
    [SerializeField] private float hurtStunDuration = 0.2f;
    [Tooltip("Tempo (s) sem poder atacar apos receber dano. Apos hurtStun, pode mover livremente.")]
    [SerializeField] private float hurtNoAttackDuration = 0.5f;
    [Tooltip("Intervalo do piscar visual durante a invulnerabilidade.")]
    [SerializeField] private float hurtBlinkInterval = 0.06f;

    [Header("Dano dos Ataques")]
    [SerializeField] private int punchDamage = 10;
    [SerializeField] private int kickDamage = 14;
    [SerializeField] private int jabDamage = 6;
    [SerializeField] private int jumpKickDamage = 12;
    [SerializeField] private int diveKickDamage = 16;

    [Header("Hitbox de Ataque")]
    [Tooltip("Tamanho da caixa de acerto, projetada na frente do player")]
    [SerializeField] private Vector2 attackHitboxSize = new Vector2(1.4f, 1.2f);
    [Tooltip("Distancia da caixa de acerto a frente do player")]
    [SerializeField] private float attackHitboxForwardOffset = 0.9f;
    [SerializeField] private float attackHitboxVerticalOffset = 0f;

    [Header("Area Caminhavel")]
    [Tooltip("Limite inferior da area onde o player pode andar (X esquerdo, Y inferior)")]
    [SerializeField] private Vector2 walkAreaMin = new Vector2(-14f, -3.5f);
    [Tooltip("Limite superior da area onde o player pode andar (X direito, Y superior)")]
    [SerializeField] private Vector2 walkAreaMax = new Vector2(46f, -2.5f);

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;

    public bool IsPushingUpAtBoundary { get; private set; }

    public static bool InputFrozen;

    [Header("Esquiva (Pulo)")]
    [Tooltip("Fracao inicial do pulo em que o player fica invulneravel (esquiva). 0.7 = 70% do pulo.")]
    [SerializeField] private float dodgeInvulnFraction = 0.7f;

    public bool IsInvulnerable =>
        isJumping && jumpDuration > 0f && (jumpTimer / jumpDuration) <= dodgeInvulnFraction;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private CharacterSkinController skinController;
    private PlayerCombatManager combatManager;

    private InputAction moveAction;
    private InputAction attackAction;
    private InputAction kickAction;
    private InputAction jabAction;
    private InputAction jumpAction;

    private Vector2 moveInput;
    private bool isAttacking;
    private float lastAttackTime;
    private int currentAttackDamage;
    private bool isJumping;
    private float groundY;
    private float jumpTimer;
    private Vector2 jumpMoveDir;
    private float hurtStunTimer;
    private float hurtNoAttackTimer;
    private float knockbackTimer;
    private Vector2 knockbackVel;
    private Coroutine hurtFlashRoutine;
    private AudioSource footstepSource;

    private const string AttackTag = "Attack";
    private const string JumpTag = "Jump";

    // Animator parameter hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int KickHash = Animator.StringToHash("Kick");
    private static readonly int JabHash = Animator.StringToHash("Jab");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int JumpKickHash = Animator.StringToHash("JumpKick");
    private static readonly int DiveKickHash = Animator.StringToHash("DiveKick");

    private void Awake()
    {
        InputFrozen = false;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        skinController = GetComponent<CharacterSkinController>();
        combatManager = GetComponent<PlayerCombatManager>();

        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        SetupInputActions();
    }

    private void SetupInputActions()
    {
        if (inputActions == null) return;

        var playerMap = inputActions.FindActionMap("Player");
        if (playerMap == null) return;

        moveAction = playerMap.FindAction("Move");
        attackAction = playerMap.FindAction("Attack");
        kickAction = playerMap.FindAction("Kick");
        jabAction = playerMap.FindAction("Jab");
        jumpAction = playerMap.FindAction("Jump");
    }

    private void Start()
    {
        SetupFootsteps();

        var zone = FindAnyObjectByType<StageZone>();
        if (zone == null) return;

        if (zone.HasSpawn) transform.position = zone.SpawnPos;
        if (zone.HasWalkArea)
        {
            walkAreaMin = zone.WalkMin;
            walkAreaMax = zone.WalkMax;
        }
    }

    private void SetupFootsteps()
    {
        var sm = SoundManager.Instance;
        if (sm == null || sm.Library == null || sm.Library.playerStep == null) return;

        var go = new GameObject("FootstepSource");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        footstepSource = go.AddComponent<AudioSource>();
        footstepSource.clip = sm.Library.playerStep;
        footstepSource.loop = true;
        footstepSource.playOnAwake = false;
        footstepSource.volume = 0.55f;
        footstepSource.outputAudioMixerGroup = sm.SfxGroup;
    }

    private void HandleFootsteps()
    {
        if (footstepSource == null) return;
        bool walking = !isJumping && !isAttacking && hurtStunTimer <= 0f
            && moveInput.sqrMagnitude > 0.01f;
        if (walking)
        {
            if (!footstepSource.isPlaying) footstepSource.Play();
        }
        else if (footstepSource.isPlaying)
        {
            footstepSource.Pause();
        }
    }

    private void OnEnable()
    {
        inputActions?.Enable();
    }

    private void OnDisable()
    {
        inputActions?.Disable();
    }

    private void Update()
    {
        if (InputFrozen)
        {
            moveInput = Vector2.zero;
            if (animator != null) animator.SetFloat(SpeedHash, 0f);
            HandleFootsteps();
            return;
        }

        if (hurtStunTimer > 0f) hurtStunTimer -= Time.deltaTime;
        if (hurtNoAttackTimer > 0f) hurtNoAttackTimer -= Time.deltaTime;

        CheckActionStates();
        HandleFootsteps();

        if (isAttacking) return;
        if (hurtStunTimer > 0f) return;

        ReadInput();
        HandleFlip();
        HandleAnimations();
    }

    private void CheckActionStates()
    {
        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (isAttacking && !stateInfo.IsTag(AttackTag))
        {
            isAttacking = false;
            animator.speed = 1f;
        }
    }

    private void FixedUpdate()
    {
        if (InputFrozen)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = knockbackVel;
            knockbackVel = Vector2.Lerp(knockbackVel, Vector2.zero, Time.fixedDeltaTime * knockbackDamping);
            ClampToWalkArea();
            return;
        }

        if (hurtStunTimer > 0f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        if (isAttacking && !isJumping) return;

        if (isJumping)
            UpdateJump();
        else
            Move();
    }

    private void UpdateJump()
    {
        jumpTimer += Time.fixedDeltaTime;
        float t = jumpTimer / jumpDuration;

        if (t >= 1f)
        {
            // Pulo terminou - voltar ao chao
            isJumping = false;
            transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float heightOffset = Mathf.Sin(t * Mathf.PI) * jumpHeight;
        float newY = groundY + heightOffset;

        float speed = skinController != null ? skinController.GetMoveSpeed() : moveSpeed;
        Vector2 pos = rb.position;
        pos.x += jumpMoveDir.x * speed * Time.fixedDeltaTime;
        pos.y = newY;
        rb.MovePosition(pos);
        rb.linearVelocity = Vector2.zero;
    }

    private void ReadInput()
    {
        moveInput = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;

        if (hurtNoAttackTimer > 0f) return;

        // Ataques - verificar cooldown
        if (Time.time - lastAttackTime < attackCooldown) return;

        bool canAirAttack = isJumping && (jumpTimer / jumpDuration) <= airAttackWindow;

        // Punch (Attack) - J / Gamepad X
        if (attackAction != null && attackAction.WasPressedThisFrame())
        {
            if (canAirAttack)
                TriggerAttack(JumpKickHash, jumpKickDamage);
            else if (!isJumping)
                TriggerAttack(AttackHash, punchDamage);
        }
        // Kick - K / Gamepad Y
        else if (kickAction != null && kickAction.WasPressedThisFrame())
        {
            if (canAirAttack)
                TriggerAttack(DiveKickHash, diveKickDamage);
            else if (!isJumping)
                TriggerAttack(KickHash, kickDamage);
        }
        else if (!isJumping && jabAction != null && jabAction.WasPressedThisFrame())
        {
            TriggerAttack(JabHash, jabDamage);
        }
        // Jump - Space / Gamepad A
        else if (jumpAction != null && jumpAction.WasPressedThisFrame() && !isJumping)
        {
            StartJump();
        }
    }

    private void Move()
    {
        float speed = skinController != null ? skinController.GetMoveSpeed() : moveSpeed;
        rb.linearVelocity = moveInput * speed;
        ClampToWalkArea();
        UpdateBoundaryFlags();
    }

    private void ClampToWalkArea()
    {
        Vector2 pos = rb.position;
        bool atMaxY = pos.y >= walkAreaMax.y;
        bool atMinY = pos.y <= walkAreaMin.y;
        bool atMaxX = pos.x >= walkAreaMax.x;
        bool atMinX = pos.x <= walkAreaMin.x;

        pos.x = Mathf.Clamp(pos.x, walkAreaMin.x, walkAreaMax.x);
        pos.y = Mathf.Clamp(pos.y, walkAreaMin.y, walkAreaMax.y);

        Vector2 v = rb.linearVelocity;
        if ((atMaxX && v.x > 0f) || (atMinX && v.x < 0f)) v.x = 0f;
        if ((atMaxY && v.y > 0f) || (atMinY && v.y < 0f)) v.y = 0f;
        rb.linearVelocity = v;

        if (pos != rb.position) rb.position = pos;
    }

    private void UpdateBoundaryFlags()
    {
        IsPushingUpAtBoundary = rb.position.y >= walkAreaMax.y - 0.01f && moveInput.y > 0.1f;
    }

    private void HandleFlip()
    {
        if (moveInput.x > 0.01f)
            spriteRenderer.flipX = false;
        else if (moveInput.x < -0.01f)
            spriteRenderer.flipX = true;
    }

    private void HandleAnimations()
    {
        animator.SetFloat(SpeedHash, moveInput.magnitude);
    }

    private void TriggerAttack(int triggerHash, int damage)
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        currentAttackDamage = damage;
        rb.linearVelocity = Vector2.zero;
        animator.SetTrigger(triggerHash);
        animator.speed = (triggerHash == KickHash) ? 1.15f : 1f;

        var sm = SoundManager.Instance;
        if (sm != null && sm.Library != null)
        {
            AudioClip clip = null;
            if (triggerHash == AttackHash) clip = sm.Library.playerPunch;
            else if (triggerHash == KickHash) clip = sm.Library.playerKick;
            else if (triggerHash == JabHash) clip = sm.Library.playerJab;
            else if (triggerHash == JumpKickHash) clip = sm.Library.playerJumpKick;
            else if (triggerHash == DiveKickHash) clip = sm.Library.playerDiveKick;
            sm.PlaySFX(clip);
        }
    }

    private void StartJump()
    {
        isJumping = true;
        groundY = transform.position.y;
        jumpTimer = 0f;
        jumpMoveDir = moveInput;
        rb.linearVelocity = Vector2.zero;
        animator.SetTrigger(JumpHash);

        var sm = SoundManager.Instance;
        if (sm != null && sm.Library != null)
            sm.PlaySFX(sm.Library.playerJump);
    }

    // --- Animation Events ---

    public void OnAttackHit()
    {
        float dir = spriteRenderer.flipX ? -1f : 1f;
        Vector2 center = (Vector2)transform.position
            + new Vector2(dir * attackHitboxForwardOffset, attackHitboxVerticalOffset);

        var hits = Physics2D.OverlapBoxAll(center, attackHitboxSize, 0f);
        var alreadyHit = new HashSet<IDamageable>();

        foreach (var hit in hits)
        {
            if (hit == null || hit.transform.IsChildOf(transform)) continue;

            var damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null || !alreadyHit.Add(damageable)) continue;

            int finalDamage = combatManager != null
                ? combatManager.CalculateAttackDamage(currentAttackDamage)
                : currentAttackDamage;

            damageable.TakeDamage(finalDamage);
        }
    }

    public void OnAttackEnd()
    {
        isAttacking = false;
        animator.speed = 1f;
    }

    public void OnJumpEnd()
    {
        isJumping = false;
        transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
    }

    public void TakeHit()
    {
        isAttacking = false;
        animator.speed = 1f;
        animator.SetTrigger(HurtHash);
        hurtStunTimer = hurtStunDuration;
        hurtNoAttackTimer = hurtNoAttackDuration;
        rb.linearVelocity = Vector2.zero;

        var sm = SoundManager.Instance;
        if (sm != null && sm.Library != null)
            sm.PlaySFX(sm.Library.playerHurt);

        if (hurtFlashRoutine != null) StopCoroutine(hurtFlashRoutine);
        hurtFlashRoutine = StartCoroutine(HurtFlash(hurtNoAttackDuration));
    }

    public void ApplyKnockback(Vector2 velocity, float duration)
    {
        knockbackVel = velocity;
        knockbackTimer = Mathf.Max(knockbackTimer, duration);
    }

    private System.Collections.IEnumerator HurtFlash(float duration)
    {
        float elapsed = 0f;
        bool visible = true;
        while (elapsed < duration)
        {
            visible = !visible;
            spriteRenderer.enabled = visible;
            yield return new WaitForSeconds(hurtBlinkInterval);
            elapsed += hurtBlinkInterval;
        }
        spriteRenderer.enabled = true;
        hurtFlashRoutine = null;
    }

    [Header("Debug")]
    [SerializeField] private bool drawAttackHitbox = true;

    private void OnDrawGizmos()
    {
        if (!drawAttackHitbox || !DebugFlags.ShowAttackHitbox) return;
        var sr = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
        float dir = (sr != null && sr.flipX) ? -1f : 1f;
        Vector3 center = transform.position
            + new Vector3(dir * attackHitboxForwardOffset, attackHitboxVerticalOffset, 0f);
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.85f);
        Gizmos.DrawWireCube(center, attackHitboxSize);
    }
}
