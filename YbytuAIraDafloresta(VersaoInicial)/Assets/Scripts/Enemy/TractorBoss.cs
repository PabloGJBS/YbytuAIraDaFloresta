using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(HealthSystem))]
public class TractorBoss : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    [SerializeField] private int bossMaxHealth = 600;

    [Header("Lanes de investida (Y dentro da zona caminhavel)")]
    [SerializeField] private float laneHighY = -3.44f;
    [SerializeField] private float laneLowY = -6.97f;

    [Header("Investida")]
    [SerializeField] private float chargeSpeed = 18f;
    [SerializeField] private int chargeDamage = 24;
    [SerializeField] private float knockbackForce = 14f;
    [SerializeField] private float knockbackDuration = 0.35f;
    [Tooltip("Tolerancia em Y pro atropelamento acertar (player precisa estar na lane).")]
    [SerializeField] private float laneHitTolerance = 1.6f;
    [Tooltip("Largura (em X) da caixa de atropelamento, centrada no trator. Menor = mais preciso.")]
    [SerializeField] private float chargeHitWidth = 4.5f;
    [Tooltip("Sprite olha pra direita por padrao? (define o flip nas investidas)")]
    [SerializeField] private bool spriteFacesRight = false;

    [Header("Superaquecimento (janelas vulneraveis)")]
    [SerializeField] private float overheatPhase1 = 5f;
    [SerializeField] private float overheatPhase2 = 8f;

    [Header("Thresholds de fase (% de vida)")]
    [SerializeField] private float phase2Threshold = 0.6f;
    [SerializeField] private float phase3Threshold = 0.3f;

    [Header("Explosoes verticais (provisorio, calibrar depois)")]
    [SerializeField] private RuntimeAnimatorController explosionController;
    [SerializeField] private Sprite explosionFirstFrame;
    [SerializeField] private int verticalExplosionCount = 3;
    [Tooltip("Tempo que as colunas vermelhas piscam antes de explodir (telegrafia).")]
    [SerializeField] private float telegraphTime = 0.8f;
    [Tooltip("Largura de cada coluna de telegrafia/explosao (unidades).")]
    [SerializeField] private float telegraphWidth = 2.6f;
    [Tooltip("Respiro depois das explosoes antes do proximo ataque.")]
    [SerializeField] private float postExplosionPause = 1.4f;
    [SerializeField] private int explosionDamage = 18;

    [Header("Arara (companheira) - opcional")]
    [Tooltip("Transform da arara que telegrafa e foge. Se vazio, so mostra o balao de fala.")]
    [SerializeField] private Transform arara;

    [Header("Falas")]
    [SerializeField] private string introLine = "Ei, olha a maquina que ta destruindo tudo!!!";
    [SerializeField] private string warnLine = "CUIDADO!!!";

    [Header("Ativacao")]
    [Tooltip("true = a luta comeca sozinha no Start (cena de teste). false = espera StartFight() (trigger na fase).")]
    [SerializeField] private bool autoStart = true;
    [Tooltip("Esconde o trator ate a luta comecar (pra quando ele fica estacionado fora da tela). Desligue se ele ja fica spawnado VISIVEL na arena.")]
    [SerializeField] private bool hideUntilFight = true;

    [Header("Morte (finale)")]
    [Tooltip("Prefabs de animais que surgem na morte do trator pra arrebenta-lo. Vazio = sem animais (so explosoes).")]
    [SerializeField] private GameObject[] finaleAnimals;
    [TextArea(1, 2)] [SerializeField] private string finaleAttackLine = "DESTRUAM LOGO ELE!";
    [TextArea(1, 2)] [SerializeField] private string finaleThanksLine = "Conseguimos! A floresta agradece!";

    public event System.Action OnDefeated;

    private SpriteRenderer sr;
    private Animator anim;
    private HealthSystem health;
    private Transform player;
    private PlayerCombatManager playerCombat;
    private PlayerController playerCtrl;
    private Camera cam;

    private bool vulnerable;
    private bool dead;
    private bool fightStarted;
    private CompanionFollow araraFollow;
    private float lastPlayerHit;
    private const float HitCooldown = 0.6f;
    private Coroutine blinkRoutine;

    private float camCenterX, leftEdge, rightEdge;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        health = GetComponent<HealthSystem>();
    }

    private void OnEnable()
    {
        health.OnDamageTaken += HandleDamageTaken;
        health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        health.OnDamageTaken -= HandleDamageTaken;
        health.OnDeath -= HandleDeath;
    }

    private void Start()
    {
        cam = Camera.main;
        health.SetMaxHealth(bossMaxHealth, true);
        CacheRefs();
        if (!autoStart && hideUntilFight && sr != null) sr.enabled = false;
        if (autoStart) StartFight();
    }

    private void CacheRefs()
    {
        var playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
        {
            player = playerGo.transform;
            playerCombat = playerGo.GetComponent<PlayerCombatManager>();
            playerCtrl = playerGo.GetComponent<PlayerController>();
        }
        if (arara == null)
        {
            var follow = FindFirstObjectByType<CompanionFollow>();
            if (follow != null) arara = follow.transform;
        }
    }

    public void StartFight()
    {
        if (fightStarted || dead) return;
        fightStarted = true;
        if (cam == null) cam = Camera.main;
        if (sr != null) sr.enabled = true; // reaparece pra luta
        CacheRefs();

        if (arara != null)
        {
            araraFollow = arara.GetComponent<CompanionFollow>();
            if (araraFollow != null) araraFollow.enabled = false;
        }

        StartCoroutine(FightRoutine());
    }

    public void TakeDamage(int damage)
    {
        if (dead || !vulnerable || health.IsDead) return;
        health.TakeDamage(damage);
    }

    private void HandleDamageTaken(int damage)
    {
        if (player != null)
        {
            var combo = player.GetComponent<ComboSystem>();
            if (combo != null) combo.RegisterHit();
        }
        PlayLib(Lib?.hitConnect);
    }

    private void PlayLib(AudioClip clip, float vol = 1f)
    {
        var sm = SoundManager.Instance;
        if (sm != null && clip != null) sm.PlaySFX(clip, vol);
    }

    private SoundLibrary Lib => SoundManager.Instance != null ? SoundManager.Instance.Library : null;

    private IEnumerator FightRoutine()
    {
        anim.Play("Idle");
        yield return Intro();

        // FASE 1: investidas ate 60%
        Debug.Log("[TractorBoss] FASE 1 (investidas) - vida 100%");
        while (!dead && health.HealthPercent > phase2Threshold)
        {
            yield return ChargeCycle();
        }
        if (dead) yield break;

        Debug.Log("[TractorBoss] MARCO 60% - explosao de transicao, libera explosoes verticais");
        yield return TransitionExplosion();

        Debug.Log("[TractorBoss] FASE 2 (explosoes verticais) - vida <= 60%");
        while (!dead && health.HealthPercent > phase3Threshold)
        {
            yield return VerticalAttack();
            if (dead) yield break;
            yield return VerticalAttack();
            if (dead) yield break;
            yield return OverheatPass(laneLowY, overheatPhase2);
        }
        if (dead) yield break;

        Debug.Log("[TractorBoss] FASE 3 (misto) - vida <= 30%");
        while (!dead)
        {
            float laneA = RandomLane(); bool dirA = Random.value < 0.5f;
            yield return AraraWarn(laneA, dirA);
            yield return ChargeWithExplosions(dirA, laneA);
            if (dead) yield break;
            float laneB = RandomLane(); bool dirB = Random.value < 0.5f;
            yield return AraraWarn(laneB, dirB);
            yield return ChargeWithExplosions(dirB, laneB);
            if (dead) yield break;
            yield return OverheatPass(laneLowY, 9999f);
        }
    }

    private float RandomLane() => Random.value < 0.5f ? laneHighY : laneLowY;

    private IEnumerator ChargeWithExplosions(bool fromRight, float laneY)
    {
        ComputeEdges();
        float startX = fromRight ? rightEdge : leftEdge;
        float endX = fromRight ? leftEdge : rightEdge;
        bool movingRight = endX > startX;
        float dir = movingRight ? 1f : -1f;

        transform.position = new Vector3(startX, laneY, 0f);
        SetFacing(movingRight);
        anim.Play("Walk");
        PlayLib(Lib?.tractorEngine);

        float nextBoom = 0.2f;
        while ((movingRight && transform.position.x < endX) || (!movingRight && transform.position.x > endX))
        {
            if (dead) yield break;
            transform.position += Vector3.right * dir * chargeSpeed * Time.deltaTime;
            TryHitPlayer(dir);

            nextBoom -= Time.deltaTime;
            if (nextBoom <= 0f)
            {
                nextBoom = 0.3f;
                float bx = transform.position.x - dir * 1.5f; // rastro logo atras do trator
                float by = laneLowY + 0.4f;
                SpawnExplosion(new Vector3(bx, by, 0f), 1.1f, sound: false);
                if (player != null && Mathf.Abs(player.position.x - bx) < 1.3f
                    && Mathf.Abs(player.position.y - by) < laneHitTolerance)
                    playerCombat?.ReceiveDamage(explosionDamage);
            }
            yield return null;
        }
    }

    private IEnumerator Intro()
    {
        Say(introLine, PlayerHeadPos(), 2.0f);
        yield return new WaitForSeconds(2.0f);
        if (dead) yield break;
        ComputeEdges();
        PlayLib(Lib?.tractorEngine);
        yield return DriveToX(rightEdge, laneHighY);
    }

    private IEnumerator ChargeCycle()
    {
        for (int i = 0; i < 4; i++)
        {
            if (dead) yield break;
            float lane = RandomLane();
            bool fromR = Random.value < 0.5f;
            yield return AraraWarn(lane, fromR);
            yield return ChargeOnce(fromR, lane, false);
        }
        if (dead) yield break;
        yield return AraraWarn(laneLowY, true);
        yield return OverheatPass(laneLowY, overheatPhase1);
    }

    private IEnumerator ChargeOnce(bool fromRight, float laneY, bool overheated)
    {
        ComputeEdges();
        float startX = fromRight ? rightEdge : leftEdge;
        float endX = fromRight ? leftEdge : rightEdge;
        bool movingRight = endX > startX;
        float dir = movingRight ? 1f : -1f;

        transform.position = new Vector3(startX, laneY, 0f);
        SetFacing(movingRight);
        anim.Play("Walk");
        PlayLib(Lib?.tractorEngine);

        while ((movingRight && transform.position.x < endX) || (!movingRight && transform.position.x > endX))
        {
            if (dead) yield break;
            transform.position += Vector3.right * dir * chargeSpeed * Time.deltaTime;
            TryHitPlayer(dir);
            yield return null;
        }
    }

    private IEnumerator OverheatPass(float laneY, float duration)
    {
        ComputeEdges();
        float startX = rightEdge;
        float stopX = camCenterX;
        transform.position = new Vector3(startX, laneY, 0f);
        SetFacing(false); // movendo pra esquerda
        anim.Play("Walk");
        PlayLib(Lib?.tractorEngine);

        while (transform.position.x > stopX)
        {
            if (dead) yield break;
            transform.position += Vector3.left * chargeSpeed * Time.deltaTime;
            TryHitPlayer(-1f);
            yield return null;
        }

        yield return OverheatWindow(duration);
        if (dead) yield break;

        // sai pela esquerda
        anim.Play("Walk");
        while (transform.position.x > leftEdge)
        {
            if (dead) yield break;
            transform.position += Vector3.left * chargeSpeed * Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator OverheatWindow(float duration)
    {
        anim.Play("Idle");
        PlayLib(Lib?.tractorOverheat);
        vulnerable = true;
        StartBlink();
        float t = 0f;
        while (t < duration && !dead)
        {
            t += Time.deltaTime;
            yield return null;
        }
        vulnerable = false;
        StopBlink();
    }

    private IEnumerator TransitionExplosion()
    {
        vulnerable = false;
        ComputeEdges();
        PlayLib(Lib?.tractorEngine);
        yield return DriveToX(camCenterX, laneLowY);
        if (dead) yield break;
        anim.Play("Special");
        SpawnExplosion(BodyCenter() + Vector3.up * 0.5f);
        yield return new WaitForSeconds(1.2f);
    }

    private IEnumerator VerticalAttack()
    {
        ComputeEdges();
        float halfW = cam != null ? cam.orthographicSize * cam.aspect : 12f;
        float bossHalf = (sr != null ? sr.bounds.extents.x : 3f);
        transform.position = new Vector3(camCenterX + halfW - bossHalf, laneLowY, 0f);
        SetFacing(false);
        anim.Play("Attack1");

        float groundY = laneLowY;
        int count = Mathf.Max(1, verticalExplosionCount);

        float margin = 2.5f;
        float left = camCenterX - halfW + margin;
        float usable = Mathf.Max(1f, (halfW - margin) * 2f);
        float slot = usable / count;
        var xs = new List<float>();
        for (int i = 0; i < count; i++)
        {
            float center = left + slot * (i + 0.5f);
            float jitter = Mathf.Max(0f, slot * 0.5f - telegraphWidth * 0.5f - 0.2f);
            xs.Add(center + Random.Range(-jitter, jitter));
        }

        var tels = new List<GameObject>();
        foreach (var x in xs) tels.Add(SpawnTelegraph(x, groundY));
        var quads = new List<SpriteRenderer>();
        var baseA = new List<float>();
        foreach (var tg in tels)
            foreach (var s in tg.GetComponentsInChildren<SpriteRenderer>()) { quads.Add(s); baseA.Add(s.color.a); }
        float t = 0f;
        while (t < telegraphTime && !dead)
        {
            t += Time.deltaTime;
            float k = 0.45f + 0.55f * Mathf.PingPong(t * 5f, 1f); // pulso de opacidade
            for (int i = 0; i < quads.Count; i++)
                if (quads[i] != null) { var c = quads[i].color; c.a = baseA[i] * k; quads[i].color = c; }
            yield return null;
        }
        foreach (var tg in tels) if (tg != null) Destroy(tg);
        if (dead) yield break;

        foreach (var x in xs)
        {
            StartCoroutine(ExplosionColumn(x, groundY));
            if (player != null && Mathf.Abs(player.position.x - x) < telegraphWidth * 0.6f)
                playerCombat?.ReceiveDamage(explosionDamage);
        }
        yield return new WaitForSeconds(postExplosionPause);
    }

    private IEnumerator AraraWarn(float laneY, bool fromRight)
    {
        if (dead) yield break;
        ComputeEdges();
        float halfW = cam != null ? cam.orthographicSize * cam.aspect : 10f;
        float side = fromRight ? (camCenterX + halfW - 3f) : (camCenterX - halfW + 3f);

        yield return MoveAraraTo(new Vector3(side, laneY + 1.6f, 0f), 0.5f);
        Say(warnLine, new Vector3(side, laneY + 2.7f, 0f), 1.0f);
        yield return new WaitForSeconds(0.8f);

        yield return MoveAraraTo(new Vector3(side, laneY + 5.5f, 0f), 0.4f);
    }

    private IEnumerator MoveAraraTo(Vector3 target, float duration)
    {
        if (arara == null) yield break;
        Vector3 start = arara.position;
        var asr = arara.GetComponent<SpriteRenderer>();
        if (asr != null && Mathf.Abs(target.x - start.x) > 0.05f) asr.flipX = target.x < start.x; // arara olha p/ onde vai
        float t = 0f;
        while (t < duration && arara != null && !dead)
        {
            t += Time.deltaTime;
            arara.position = Vector3.Lerp(start, target, Mathf.Clamp01(t / duration));
            yield return null;
        }
        if (arara != null) arara.position = target;
    }

    private IEnumerator DriveToX(float targetX, float laneY)
    {
        SetFacing(targetX > transform.position.x);
        anim.Play("Walk");
        while (!dead && (Mathf.Abs(transform.position.x - targetX) > 0.1f || Mathf.Abs(transform.position.y - laneY) > 0.1f))
        {
            Vector3 p = transform.position;
            p.x = Mathf.MoveTowards(p.x, targetX, chargeSpeed * Time.deltaTime);
            p.y = Mathf.MoveTowards(p.y, laneY, chargeSpeed * Time.deltaTime);
            transform.position = p;
            yield return null;
        }
    }

    private Vector3 BodyCenter() => transform.position + Vector3.up * 2f;

    private void Say(string text, Vector3 pos, float hold)
    {
        PlayLib(Lib?.araraTalk);
        SpeechBubble.Pop(pos, text, 3.0f, new Color(1f, 0.95f, 0.5f), 850, hold);
    }

    private Vector3 PlayerHeadPos()
    {
        if (player != null) return player.position + Vector3.up * 2.5f;
        return new Vector3(camCenterX, laneHighY + 2f, 0f);
    }

    private void TryHitPlayer(float dir)
    {
        if (player == null) return;
        if (Time.time - lastPlayerHit < HitCooldown) return;

        Vector3 pp = player.position;
        bool inX = Mathf.Abs(pp.x - transform.position.x) < chargeHitWidth * 0.5f;
        bool inLane = Mathf.Abs(pp.y - transform.position.y) < laneHitTolerance;
        if (inX && inLane)
        {
            lastPlayerHit = Time.time;
            playerCombat?.ReceiveDamage(chargeDamage);
            playerCtrl?.ApplyKnockback(new Vector2(dir * knockbackForce, 0f), knockbackDuration);
            PlayLib(Lib?.tractorHitMetal);
        }
    }

    private void ComputeEdges()
    {
        if (cam == null) cam = Camera.main;
        camCenterX = cam != null ? cam.transform.position.x : 0f;
        float halfW = cam != null ? cam.orthographicSize * cam.aspect : 12f;
        float bossHalf = (sr != null ? sr.bounds.extents.x : 3f) + 1.5f;
        leftEdge = camCenterX - halfW - bossHalf;
        rightEdge = camCenterX + halfW + bossHalf;
    }

    private void MoveOffscreen()
    {
        ComputeEdges();
        transform.position = new Vector3(rightEdge, laneLowY, 0f);
    }

    private void SetFacing(bool movingRight)
    {
        if (sr == null) return;
        sr.flipX = movingRight != spriteFacesRight;
    }

    private void StartBlink()
    {
        StopBlink();
        blinkRoutine = StartCoroutine(BlinkRed());
    }

    private void StopBlink()
    {
        if (blinkRoutine != null) StopCoroutine(blinkRoutine);
        blinkRoutine = null;
        if (sr != null) sr.color = Color.white;
    }

    private IEnumerator BlinkRed()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * 6f;
            float k = (Mathf.Sin(t) + 1f) * 0.5f; // 0..1
            sr.color = Color.Lerp(new Color(1f, 0.20f, 0.15f, 1f), new Color(1f, 0.55f, 0.35f, 1f), k);
            yield return null;
        }
    }

    private void SpawnExplosion(Vector3 pos, float scale = 2f, bool sound = true)
    {
        var go = new GameObject("Explosion");
        go.transform.position = pos;
        var esr = go.AddComponent<SpriteRenderer>();
        esr.sortingOrder = 60;
        if (explosionFirstFrame != null) esr.sprite = explosionFirstFrame;
        if (explosionController != null)
        {
            var ea = go.AddComponent<Animator>();
            ea.runtimeAnimatorController = explosionController;
            string[] states = { "Explosion1", "Explosion2", "Explosion3", "Explosion4" };
            ea.Play(states[Random.Range(0, states.Length)]);
        }
        go.transform.localScale = Vector3.one * scale;
        Destroy(go, 1f);

        if (!sound) return;
        var sm = SoundManager.Instance;
        if (sm != null && sm.Library != null && sm.Library.explosions != null && sm.Library.explosions.Length > 0)
        {
            var clip = sm.Library.explosions[Random.Range(0, sm.Library.explosions.Length)];
            if (clip != null) sm.PlaySFX(clip, 0.6f);
        }
    }

    private IEnumerator ExplosionColumn(float x, float groundY)
    {
        const int n = 5;
        for (int i = 0; i < n; i++)
        {
            float y = groundY + 0.4f + i * 1.0f;
            float jx = x + Random.Range(-0.7f, 0.7f);
            SpawnExplosion(new Vector3(jx, y, 0f), 1.0f, sound: i == 0);
            yield return new WaitForSeconds(0.07f);
        }
    }

    private GameObject SpawnTelegraph(float x, float groundY)
    {
        const float bandTop = -1.2f;
        float bottom = groundY - 0.3f;        // um tico abaixo do chao da lane
        float h = Mathf.Max(1f, bandTop - bottom);
        float cy = (bandTop + bottom) * 0.5f;

        var go = new GameObject("ExplosionTelegraph");
        go.transform.position = new Vector3(x, cy, 0f);

        var fill = MakeTelegraphQuad(go.transform, telegraphWidth, h, new Color(1f, 0.22f, 0.12f, 0.32f), 55);
        MakeTelegraphQuad(go.transform, telegraphWidth * 0.42f, h, new Color(1f, 0.55f, 0.25f, 0.5f), 56);
        var baseLine = MakeTelegraphQuad(go.transform, telegraphWidth * 1.05f, 0.5f, new Color(1f, 0.85f, 0.4f, 0.8f), 57);
        baseLine.transform.localPosition = new Vector3(0f, -h * 0.5f + 0.25f, 0f);
        return go;
    }

    private SpriteRenderer MakeTelegraphQuad(Transform parent, float w, float h, Color col, int order)
    {
        var q = new GameObject("q", typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        q.transform.SetParent(parent, false);
        q.sprite = WhiteQuad();
        q.color = col;
        q.sortingOrder = order;
        q.transform.localScale = new Vector3(w, h, 1f);
        return q;
    }

    private static Sprite _quad;
    private static Sprite WhiteQuad()
    {
        if (_quad != null) return _quad;
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, Color.white);
        t.Apply();
        _quad = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _quad;
    }

    private void HandleDeath()
    {
        if (dead) return;
        dead = true;
        vulnerable = false;
        StopAllCoroutines();
        StopBlink();
        if (araraFollow != null) araraFollow.enabled = true; // arara volta a seguir o player
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        StopBlink();
        if (sr != null) sr.color = Color.white;
        anim.Play("Idle");
        SpawnExplosion(BodyCenter(), 2.5f);
        yield return new WaitForSeconds(0.7f);

        var animals = SpawnFinaleAnimals();
        if (animals.Count > 0 && !string.IsNullOrEmpty(finaleAttackLine))
            SpeechBubble.Pop(BodyCenter() + Vector3.up * 2.5f, finaleAttackLine, 3.2f, new Color(0.7f, 1f, 0.7f), 900, 2.2f);

        // correm ate perto do trator
        float stopX = transform.position.x - 3.5f;
        float runT = 0f;
        while (runT < 3f && animals.Count > 0)
        {
            runT += Time.deltaTime;
            bool anyMoving = false;
            foreach (var a in animals)
            {
                if (a == null || a.transform.position.x >= stopX) continue;
                a.transform.position += Vector3.right * 6f * Time.deltaTime;
                anyMoving = true;
            }
            if (!anyMoving) break;
            yield return null;
        }

        Vector3[] offs = { new Vector3(-2.2f, 1f, 0f), new Vector3(2.2f, 2.6f, 0f), new Vector3(0f, 0.4f, 0f) };
        for (int i = 0; i < 3; i++)
        {
            SpawnExplosion(BodyCenter() + offs[i], 1.6f);
            yield return new WaitForSeconds(0.45f);
        }

        anim.Play("Death");

        if (animals.Count > 0 && !string.IsNullOrEmpty(finaleThanksLine))
            SpeechBubble.Pop(BodyCenter() + Vector3.up * 2.5f, finaleThanksLine, 3.2f, new Color(0.7f, 1f, 0.7f), 900, 2.6f);
        yield return new WaitForSeconds(2.6f);

        Debug.Log("[TractorBoss] Trator destruido. Fim de fase.");
        OnDefeated?.Invoke();
    }

    private List<GameObject> SpawnFinaleAnimals()
    {
        var list = new List<GameObject>();
        if (finaleAnimals == null || finaleAnimals.Length == 0) return list;
        float baseX = transform.position.x - 12f;
        for (int i = 0; i < finaleAnimals.Length; i++)
        {
            if (finaleAnimals[i] == null) continue;
            var pos = new Vector3(baseX - i * 1.6f, laneLowY + Random.Range(0f, 1.5f), 0f);
            var go = Instantiate(finaleAnimals[i], pos, Quaternion.identity);
            var ally = go.GetComponent<AllyCreature>();
            if (ally != null) ally.enabled = false;     // sem IA: eu movo pela cutscene
            var hsAlly = go.GetComponent<HealthSystem>();
            if (hsAlly != null) hsAlly.enabled = false;
            var an = go.GetComponentInChildren<Animator>();
            if (an != null) an.Play("Walk");            // correndo pra cima do trator
            list.Add(go);
        }
        return list;
    }
}
