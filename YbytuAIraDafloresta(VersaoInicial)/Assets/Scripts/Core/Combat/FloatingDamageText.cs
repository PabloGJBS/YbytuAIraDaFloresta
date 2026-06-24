using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingDamageText : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.9f;
    [SerializeField] private float riseSpeed = 2.5f;
    [SerializeField] private float fontSize = 4f;

    private static TMP_FontAsset _cachedFont;
    private static TMP_FontAsset GetDefaultFont()
    {
        if (_cachedFont != null) return _cachedFont;
        _cachedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (_cachedFont == null) _cachedFont = TMP_Settings.defaultFontAsset;
        return _cachedFont;
    }

    private static readonly Color[] RankColors = {
        Color.white,                                  // C
        new Color(1f, 1f, 0.4f),                       // B amarelo
        new Color(1f, 0.7f, 0.2f),                     // A laranja
        new Color(1f, 0.35f, 0.25f),                   // S vermelho
        new Color(1f, 0.4f, 0.85f),                    // SS rosa
        new Color(0.4f, 0.95f, 1f),                    // SSS ciano
    };

    public static FloatingDamageText Spawn(Vector3 worldPos, int damage, int rankIndex = 0, float multiplier = 1f)
    {
        var go = new GameObject("FloatingDamage", typeof(TextMeshPro));
        go.transform.position = worldPos;

        var tmp = go.GetComponent<TextMeshPro>();
        tmp.font = GetDefaultFont();
        tmp.text = multiplier > 1.01f
            ? $"{damage}  x{multiplier:0.0}"
            : damage.ToString();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 2f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = rankIndex >= 0 && rankIndex < RankColors.Length
            ? RankColors[rankIndex]
            : Color.white;
        tmp.sortingOrder = 50;
        var rt = go.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(4f, 1.5f);

        var floater = go.AddComponent<FloatingDamageText>();
        floater.StartCoroutine(floater.RiseAndFade(tmp));
        return floater;
    }

    private IEnumerator RiseAndFade(TextMeshPro tmp)
    {
        float t = 0f;
        Color baseColor = tmp.color;
        Vector3 start = transform.position;

        while (t < lifetime)
        {
            t += Time.deltaTime;
            float k = t / lifetime;
            // Sobe desacelerando
            transform.position = start + Vector3.up * (riseSpeed * (1f - k * 0.5f) * t);
            // Fade out na segunda metade
            if (k > 0.5f)
            {
                float a = 1f - (k - 0.5f) * 2f;
                var c = baseColor;
                c.a = a;
                tmp.color = c;
            }
            yield return null;
        }

        Destroy(gameObject);
    }
}
