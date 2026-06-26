using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class InteractableHighlight : MonoBehaviour
{
    [Header("Contorno")]
    [Tooltip("Escala da silhueta (>1 = espessura do contorno).")]
    public float outlineScale = 1.08f;
    public Color outlineColor = Color.white;

    [Header("Brilho (pulso)")]
    public float pulseSpeed = 4f;
    [Range(0f, 1f)] public float minAlpha = 0.35f;
    [Range(0f, 1f)] public float maxAlpha = 0.95f;

    private SpriteRenderer source;
    private SpriteRenderer outline;
    private bool active;

    private void Awake()
    {
        source = GetComponent<SpriteRenderer>();

        var go = new GameObject("Outline");
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * outlineScale;

        outline = go.AddComponent<SpriteRenderer>();
        outline.sprite = source.sprite;
        outline.sortingLayerID = source.sortingLayerID;
        outline.sortingOrder = source.sortingOrder - 1;
        outline.color = new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0f);
        outline.enabled = false;
    }

    public void SetHighlighted(bool on)
    {
        active = on;
        if (outline == null) return;
        if (on)
        {
            outline.sprite = source.sprite;                 // sincroniza se o sprite mudou
            outline.sortingOrder = source.sortingOrder - 1;
            outline.enabled = true;
        }
        else
        {
            outline.enabled = false;
        }
    }

    private void Update()
    {
        if (!active || outline == null) return;
        float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
        var c = outlineColor;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
        outline.color = c;
    }
}
