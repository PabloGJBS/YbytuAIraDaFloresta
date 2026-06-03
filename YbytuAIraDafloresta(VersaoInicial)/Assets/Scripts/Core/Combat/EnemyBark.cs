using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// "Grito" flutuante acima do inimigo (bark de combate). Texto criado em codigo,
/// sobe levemente e some. Placeholder ate ter a caixa de dialogo estilizada.
/// Uso: EnemyBark.Spawn(worldPos, "Ei, voce nao devia estar aqui!");
/// </summary>
public class EnemyBark : MonoBehaviour
{
    [SerializeField] private float lifetime = 2.2f;
    [SerializeField] private float riseSpeed = 0.4f;

    private static TMP_FontAsset _font;
    private static TMP_FontAsset Font()
    {
        if (_font != null) return _font;
        _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (_font == null) _font = TMP_Settings.defaultFontAsset;
        return _font;
    }

    public static EnemyBark Spawn(Vector3 worldPos, string text)
    {
        var go = new GameObject("EnemyBark", typeof(TextMeshPro));
        go.transform.position = worldPos;

        var tmp = go.GetComponent<TextMeshPro>();
        tmp.font = Font();
        tmp.text = text;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 2.4f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(1f, 0.95f, 0.4f); // amarelo claro (destaque)
        tmp.sortingOrder = 60;
        tmp.enableWordWrapping = true;

        var rt = go.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(7f, 2.5f);

        var bark = go.AddComponent<EnemyBark>();
        bark.StartCoroutine(bark.Life(tmp));
        return bark;
    }

    private IEnumerator Life(TextMeshPro tmp)
    {
        float t = 0f;
        Color baseColor = tmp.color;
        Vector3 start = transform.position;

        while (t < lifetime)
        {
            t += Time.deltaTime;
            float k = t / lifetime;
            transform.position = start + Vector3.up * (riseSpeed * t);
            if (k > 0.7f) // fade no final
            {
                float a = 1f - (k - 0.7f) / 0.3f;
                var c = baseColor; c.a = Mathf.Clamp01(a);
                tmp.color = c;
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}
