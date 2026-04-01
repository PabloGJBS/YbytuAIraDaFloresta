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

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (sr == null) return;
        sr.sortingOrder = Mathf.RoundToInt(-transform.position.y * orderMultiplier) + orderOffset;
    }
}
