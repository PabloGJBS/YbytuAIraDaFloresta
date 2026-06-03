using UnityEngine;

/// <summary>
/// Ordena o SpriteRenderer pela posicao Y a cada frame.
/// Quem esta mais abaixo na cena (Y menor) aparece na frente.
/// Padrao classico de beat'em up / RPG 2D top-down.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class YSortRenderer : MonoBehaviour
{
    [Tooltip("Multiplica o Y antes de virar sortingOrder. Maior = mais sensivel a diferencas pequenas.")]
    [SerializeField] private int orderMultiplier = 100;
    [Tooltip("Offset adicional somado ao sortingOrder final.")]
    [SerializeField] private int orderOffset = 0;
    [Tooltip("Ordena pela BASE do sprite (pe) em vez do transform. Use em objetos altos com pivo no centro (troncos), pra nao afundarem atras do cenario.")]
    [SerializeField] private bool useSpriteBottom = false;
    [Tooltip("Ordem minima: evita o sprite afundar atras do cenario de fundo quando sobe muito (Y alto). O fundo fica em <=12; atores/troncos nunca devem ir abaixo disso.")]
    [SerializeField] private int minOrder = 50;

    private SpriteRenderer sr;
    private float bottomOffset;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        RecalcBottomOffset();
    }

    // Distancia (em unidades de mundo) do pivo ate a base do sprite. Negativa quando o pivo esta acima da base.
    private void RecalcBottomOffset()
    {
        bottomOffset = 0f;
        if (useSpriteBottom && sr != null && sr.sprite != null)
            bottomOffset = (sr.sprite.bounds.center.y - sr.sprite.bounds.extents.y) * transform.lossyScale.y;
    }

    private void LateUpdate()
    {
        if (sr == null) return;
        float y = transform.position.y + bottomOffset;
        int order = Mathf.RoundToInt(-y * orderMultiplier) + orderOffset;
        sr.sortingOrder = Mathf.Max(order, minOrder);
    }
}
