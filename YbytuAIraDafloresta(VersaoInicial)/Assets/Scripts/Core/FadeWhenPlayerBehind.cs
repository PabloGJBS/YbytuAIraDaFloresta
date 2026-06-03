using UnityEngine;

/// <summary>
/// Deixa o SpriteRenderer semitransparente quando o player encosta/passa
/// por dentro da area do prop, dando a sensacao de que o personagem esta
/// passando por tras dele. Pensado para os props de frente (PropsFront).
///
/// Deteccao por AABB: cruza o bounds do sprite (ajustavel por padding) com
/// o bounds do collider do player (ou um ponto, se ele nao tiver collider).
/// O alpha faz lerp suave entre 1 e <see cref="fadedAlpha"/>.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class FadeWhenPlayerBehind : MonoBehaviour
{
    [Header("Fade")]
    [Tooltip("Alpha do prop enquanto o player esta dentro da area dele.")]
    [Range(0f, 1f)] public float fadedAlpha = 0.45f;
    [Tooltip("Velocidade do fade, em alpha por segundo.")]
    public float fadeSpeed = 8f;

    [Header("Area de deteccao")]
    [Tooltip("Encolhe (negativo) ou expande (positivo) o bounds do sprite, em unidades do mundo.")]
    public float padding = -0.15f;
    [Tooltip("So escurece quando o prop estiver realmente desenhado na frente do player (compara sortingOrder).")]
    public bool requireInFront = false;

    [Header("Player")]
    [Tooltip("Tag usada para localizar o player na cena.")]
    public string playerTag = "Player";

    private SpriteRenderer sr;
    private Transform playerTf;
    private Collider2D playerCol;
    private SpriteRenderer playerSr;
    private int enemyMask;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        enemyMask = LayerMask.GetMask("Enemy");
    }

    private void OnEnable()
    {
        SetAlpha(1f);
    }

    private void TryResolvePlayer()
    {
        GameObject go = GameObject.FindGameObjectWithTag(playerTag);
        if (go == null) return;
        playerTf = go.transform;
        playerCol = go.GetComponentInChildren<Collider2D>();
        playerSr = go.GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if (playerTf == null)
        {
            TryResolvePlayer();
            if (playerTf == null) return;
        }

        bool behind = IsPlayerInside();
        if (behind && requireInFront && playerSr != null)
            behind = RendersInFrontOf(playerSr);

        // Tambem desbota quando um inimigo esta atras da folhagem, senao ele fica
        // escondido (FadeWhenPlayerBehind originalmente so olhava o player).
        if (!behind) behind = IsEnemyInside();

        float target = behind ? fadedAlpha : 1f;
        float current = sr.color.a;
        if (!Mathf.Approximately(current, target))
            SetAlpha(Mathf.MoveTowards(current, target, fadeSpeed * Time.deltaTime));
    }

    private bool IsPlayerInside()
    {
        Bounds b = sr.bounds;
        b.Expand(new Vector3(padding * 2f, padding * 2f, 0f));

        if (playerCol != null)
        {
            Bounds pb = playerCol.bounds;
            // ignora Z para o teste 2D
            return b.min.x <= pb.max.x && b.max.x >= pb.min.x
                && b.min.y <= pb.max.y && b.max.y >= pb.min.y;
        }

        Vector3 p = playerTf.position;
        p.z = b.center.z;
        return b.Contains(p);
    }

    private bool IsEnemyInside()
    {
        if (enemyMask == 0) return false;
        Bounds b = sr.bounds;
        b.Expand(new Vector3(padding * 2f, padding * 2f, 0f));
        return Physics2D.OverlapBox(b.center, b.size, 0f, enemyMask) != null;
    }

    private bool RendersInFrontOf(SpriteRenderer other)
    {
        if (sr.sortingLayerID != other.sortingLayerID)
            return SortingLayer.GetLayerValueFromID(sr.sortingLayerID)
                 > SortingLayer.GetLayerValueFromID(other.sortingLayerID);
        return sr.sortingOrder > other.sortingOrder;
    }

    private void SetAlpha(float a)
    {
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }
}
