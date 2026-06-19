using UnityEngine;
using TMPro;
using System.Collections;

public class FleeingAnimal : MonoBehaviour
{
    [Header("Identidade")]
    [Tooltip("Nome usado no objeto de fala (ex.: Sapo, Inseto).")]
    public string creatureName = "Bicho";

    [Header("Movimento")]
    [Tooltip("Velocidade da corrida (x negativo = correndo pra esquerda).")]
    public Vector2 velocity = new Vector2(-5f, 0f);
    [Tooltip("Altura do pulinho. Sapo: alto (~0.4). Inseto: baixo (~0.05).")]
    public float hopAmplitude = 0.18f;
    [Tooltip("Frequencia do pulinho. Sapo: baixa (~3). Inseto: alta (~12).")]
    public float hopFrequency = 7f;
    [Tooltip("Segundos ate sumir (so vale pros que fogem; quem morre vira corpo e fica).")]
    public float lifetime = 16f;
    [Tooltip("Marque se o sprite original aponta pra direita.")]
    public bool spriteFacesRight = true;
    public string runStateName = "Run";
    public string deathStateName = "Death";

    [Header("Limite (some ao cruzar a linha)")]
    [Tooltip("Se true, some ao cruzar despawnX (definido pelo herd). Senao, some por tempo (lifetime).")]
    public bool useDespawnX = false;
    public float despawnX = 0f;

    [Header("Passar mal pela fumaca")]
    [Tooltip("Liga o comportamento de passar mal e morrer. Desligue pra bichos que so fogem (ex.: enxame de insetos).")]
    public bool enableSickDeath = true;
    public bool isSick = false;
    [Tooltip("Segundos correndo antes de comecar a passar mal.")]
    public float sickAfterSeconds = 1.3f;
    [Tooltip("Quantos 'danos' da fumaca ate morrer.")]
    public int damageTicks = 5;
    public int damagePerTick = 1;
    public float tickInterval = 0.85f;
    [Tooltip("Fala dita ao passar mal (se a chave de localizacao estiver vazia).")]
    public string sickLine = "";
    public string sickLocalizationKey = "";

    private SpriteRenderer sr;
    private Animator anim;
    private float baseY;
    private float phase;
    private bool dead;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
        baseY = transform.position.y;
        phase = transform.position.x * 0.7f;
    }

    public void Launch(Vector2 vel, bool sick)
    {
        velocity = vel;
        isSick = sick && enableSickDeath;
        ApplyFacing();
    }

    private void Start()
    {
        ApplyFacing();
        PlayState(runStateName);
        if (isSick) StartCoroutine(SickRoutine());
        else Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (dead) return;
        var p = transform.position;
        p.x += velocity.x * Time.deltaTime;
        baseY += velocity.y * Time.deltaTime;
        phase += hopFrequency * Time.deltaTime;
        p.y = baseY + Mathf.Abs(Mathf.Sin(phase)) * hopAmplitude; // pulinho sempre pra cima
        transform.position = p;

        if (useDespawnX && ReachedDespawn(p.x)) Destroy(gameObject);
    }

    private bool ReachedDespawn(float x)
    {
        return (velocity.x < 0f && x <= despawnX) || (velocity.x > 0f && x >= despawnX);
    }

    private IEnumerator SickRoutine()
    {
        yield return new WaitForSeconds(sickAfterSeconds);

        velocity *= 0.45f;
        ShowLine(ResolveLine());
        yield return new WaitForSeconds(0.7f);

        int ticks = Mathf.Max(1, damageTicks);
        for (int i = 0; i < ticks; i++)
        {
            TakeSmokeHit(damagePerTick);
            yield return new WaitForSeconds(tickInterval);
        }

        Die();
    }

    private void TakeSmokeHit(int dmg)
    {
        velocity *= 0.6f;                  // cambaleia
        StartCoroutine(Flash());
    }

    private IEnumerator Flash()
    {
        if (sr == null) yield break;
        var orig = sr.color;
        sr.color = new Color(1f, 0.55f, 0.55f);
        yield return new WaitForSeconds(0.1f);
        if (sr != null) sr.color = orig;
    }

    private void Die()
    {
        dead = true;
        velocity = Vector2.zero;
        var p = transform.position;
        p.y = baseY;
        transform.position = p;
        PlayState(deathStateName);
    }

    private void PlayState(string state)
    {
        if (anim != null && anim.runtimeAnimatorController != null && !string.IsNullOrEmpty(state))
            anim.Play(state, 0, 0f);
    }

    private void ApplyFacing()
    {
        if (sr == null) return;
        bool goingLeft = velocity.x < 0f;
        sr.flipX = spriteFacesRight ? goingLeft : !goingLeft;
    }

    private string ResolveLine()
    {
        if (!string.IsNullOrEmpty(sickLocalizationKey))
        {
            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                string s = loc.GetText(sickLocalizationKey);
                if (!string.IsNullOrEmpty(s) && !s.StartsWith("[")) return s;
            }
        }
        return sickLine;
    }

    private void ShowLine(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        var go = new GameObject(creatureName + "Line", typeof(TextMeshPro));
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        float s = transform.lossyScale.x;
        if (s > 0.001f) go.transform.localScale = Vector3.one / s;

        var tmp = go.GetComponent<TextMeshPro>();
        tmp.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        tmp.text = text;
        tmp.fontSize = 2.4f;
        tmp.fontStyle = FontStyles.Bold | FontStyles.Italic;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.85f, 0.95f, 1f);
        tmp.outlineWidth = 0.22f;
        tmp.outlineColor = new Color32(0, 0, 0, 255);
        tmp.enableWordWrapping = true;
        tmp.rectTransform.sizeDelta = new Vector2(7f, 2.5f);
        tmp.sortingOrder = 1000;

        Destroy(go, 4f);
    }
}
