using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Um coelho da fuga: corre no chao (com pulinho) fugindo do fogo e some ao sair da
/// tela. Vira o sprite pra direcao da corrida e toca o estado "Run" do Animator.
///
/// Se for o coelho DOENTE (isSick), depois de alguns segundos ele passa mal: desacelera,
/// fala que esta sem ar, toma alguns "danos" da fumaca (texto flutuante) e MORRE no meio
/// do caminho, virando um corpo que FICA na cena (animais mortos nao somem como inimigos).
/// </summary>
public class Rabbit : MonoBehaviour
{
    [Header("Movimento")]
    [Tooltip("Velocidade da corrida (x negativo = correndo pra esquerda).")]
    public Vector2 velocity = new Vector2(-5f, 0f);
    public float hopAmplitude = 0.18f;
    public float hopFrequency = 7f;
    [Tooltip("Segundos ate sumir (so vale pros que fogem; o doente vira corpo e fica).")]
    public float lifetime = 16f;
    [Tooltip("Marque se o sprite original aponta pra direita.")]
    public bool spriteFacesRight = true;
    public string runStateName = "Run";
    public string deathStateName = "Death";

    [Header("Limite (some ao cruzar a linha)")]
    [Tooltip("Se true, o coelho some ao cruzar despawnX (definido pelo RabbitHerd). Senao, some por tempo (lifetime).")]
    public bool useDespawnX = false;
    public float despawnX = 0f;

    [Header("Coelho doente (fumaca)")]
    public bool isSick = false;
    [Tooltip("Segundos correndo antes de comecar a passar mal.")]
    public float sickAfterSeconds = 1.3f;
    [Tooltip("Quantos 'danos' da fumaca ate morrer (aguenta mais um tempo antes de cair).")]
    public int damageTicks = 5;
    public int damagePerTick = 1;
    public float tickInterval = 0.85f;
    [Tooltip("Fala dita ao passar mal (se a chave de localizacao estiver vazia).")]
    public string sickLine = "Tá difícil de respirar...";
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
        phase = transform.position.x * 0.7f; // dessincroniza o pulinho entre coelhos
    }

    /// <summary>Lanca o coelho numa direcao. sick = este eh o que passa mal e morre.</summary>
    public void Launch(Vector2 vel, bool sick)
    {
        velocity = vel;
        isSick = sick;
        ApplyFacing();
    }

    private void Start()
    {
        ApplyFacing();
        PlayState(runStateName);
        if (isSick) StartCoroutine(SickRoutine());
        else Destroy(gameObject, lifetime); // os saudaveis somem fora da tela
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

        velocity *= 0.45f;                 // desacelera, comecando a passar mal
        ShowLine(ResolveLine());
        yield return new WaitForSeconds(0.7f);

        int ticks = Random.Range(3, 6); // aleatorio entre 3 e 5
        for (int i = 0; i < ticks; i++)
        {
            TakeSmokeHit(damagePerTick);
            yield return new WaitForSeconds(tickInterval);
        }

        Die();
    }

    private void TakeSmokeHit(int dmg)
    {
        // Sem numero de dano flutuante: a fumaca o sufoca aos poucos (so cambaleia + pisca).
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
        p.y = baseY;                       // pousa o corpo no chao (sem o offset do pulo)
        transform.position = p;
        PlayState(deathStateName);
        // NAO destroi: o corpo fica na cena (animais mortos nao somem como inimigos).
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

        var go = new GameObject("RabbitLine", typeof(TextMeshPro));
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

        Destroy(go, 4f); // a fala some depois de um tempo (o corpo fica)
    }
}
