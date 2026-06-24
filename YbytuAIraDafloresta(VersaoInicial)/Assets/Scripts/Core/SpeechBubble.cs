using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Balao de fala estilizado em world-space: caixa escura + borda + rabicho apontando pra
/// baixo (pro falante) + texto. Reutilizado pela arara, pelos inimigos (barks) e pelos
/// javalis. A caixa se redimensiona pro texto via SetText().
///
/// Uso transiente (some sozinho): SpeechBubble.Pop(worldPos, "texto", ...).
/// Uso controlado (typewriter): SpeechBubble.Create(parent...) + SetText() + Label.
/// </summary>
public class SpeechBubble : MonoBehaviour
{
    private TextMeshPro tmp;
    private SpriteRenderer border, box, tailBorder, tail;
    private SpriteRenderer[] quads;
    private Color[] quadBase;
    private Color textBase;
    private bool withTail;
    private bool clampScreen;
    private const float PadX = 0.7f, PadY = 0.42f;

    private static readonly Color BorderColor = new Color(0.85f, 0.7f, 0.35f, 0.95f);
    private static readonly Color FillColor = new Color(0.06f, 0.07f, 0.10f, 0.93f);

    public TMP_Text Label => tmp;

    // Sprite branco 1x1 reutilizavel.
    private static Sprite _quad;
    private static Sprite Quad()
    {
        if (_quad != null) return _quad;
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, Color.white);
        t.Apply();
        _quad = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _quad;
    }

    public static SpeechBubble Create(Transform parent, Vector3 localPos, int baseOrder, float fontSize, float wrapWidth, Color textColor, bool tail = true)
    {
        var go = new GameObject("SpeechBubble");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        if (parent != null)
        {
            float s = parent.lossyScale.x;
            if (s > 0.001f) go.transform.localScale = Vector3.one / s;
        }
        var sb = go.AddComponent<SpeechBubble>();
        sb.Build(baseOrder, fontSize, wrapWidth, textColor, tail);
        return sb;
    }

    public static SpeechBubble CreateWorld(Vector3 worldPos, int baseOrder, float fontSize, float wrapWidth, Color textColor, bool tail = true)
    {
        var go = new GameObject("SpeechBubble");
        go.transform.position = worldPos;
        var sb = go.AddComponent<SpeechBubble>();
        sb.Build(baseOrder, fontSize, wrapWidth, textColor, tail);
        return sb;
    }

    /// <summary>Fala transiente: aparece, sobe um pouco, segura e some sozinha.</summary>
    public static SpeechBubble Pop(Vector3 worldPos, string text, float fontSize = 3.0f, Color? textColor = null, int baseOrder = 800, float hold = 2.2f, float rise = 0.4f, float wrapWidth = 8f)
    {
        var sb = CreateWorld(worldPos, baseOrder, fontSize, wrapWidth, textColor ?? new Color(1f, 0.97f, 0.86f), true);
        sb.clampScreen = true; // barks transientes: nao deixar sair da tela (ex.: chefe no canto)
        sb.SetText(text);
        sb.StartCoroutine(sb.LifeRoutine(hold, rise));
        return sb;
    }

    private void Build(int baseOrder, float fontSize, float wrapWidth, Color textColor, bool tailOn)
    {
        withTail = tailOn;
        border = MakeQuad("Border", BorderColor, baseOrder);
        box = MakeQuad("Box", FillColor, baseOrder + 1);
        if (withTail)
        {
            tailBorder = MakeQuad("TailBorder", BorderColor, baseOrder);
            tailBorder.transform.localRotation = Quaternion.Euler(0, 0, 45f);
            tail = MakeQuad("Tail", FillColor, baseOrder + 2);
            tail.transform.localRotation = Quaternion.Euler(0, 0, 45f);
        }

        var textGo = new GameObject("Text", typeof(TextMeshPro));
        textGo.transform.SetParent(transform, false);
        tmp = textGo.GetComponent<TextMeshPro>();
        tmp.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = textColor;
        tmp.enableWordWrapping = true;
        tmp.rectTransform.sizeDelta = new Vector2(wrapWidth, 3f);
        tmp.sortingOrder = baseOrder + 3;
        tmp.text = "";

        quads = withTail ? new[] { border, box, tailBorder, tail } : new[] { border, box };
        quadBase = new Color[quads.Length];
        for (int i = 0; i < quads.Length; i++) quadBase[i] = quads[i].color;
        textBase = textColor;
        Relayout();
    }

    private SpriteRenderer MakeQuad(string n, Color color, int order)
    {
        var q = new GameObject(n, typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
        q.transform.SetParent(transform, false);
        q.sprite = Quad();
        q.color = color;
        q.sortingOrder = order;
        return q;
    }

    /// <summary>Troca o texto e redimensiona a caixa.</summary>
    public void SetText(string text)
    {
        if (tmp == null) return;
        tmp.maxVisibleCharacters = 99999;
        tmp.text = text;
        tmp.ForceMeshUpdate();
        Relayout();
    }

    /// <summary>Pro efeito de digitacao: quantos caracteres mostrar (caixa ja dimensionada).</summary>
    public void SetVisibleChars(int n)
    {
        if (tmp != null) tmp.maxVisibleCharacters = Mathf.Max(0, n);
    }

    private void Relayout()
    {
        bool empty = string.IsNullOrEmpty(tmp.text);
        Vector2 ts = empty ? Vector2.zero : tmp.GetRenderedValues(false);
        if (float.IsNaN(ts.x) || ts.x < 0.1f) ts.x = 1f;
        if (float.IsNaN(ts.y) || ts.y < 0.1f) ts.y = 1f;
        float w = ts.x + PadX * 2f;
        float h = ts.y + PadY * 2f;

        if (border != null) { border.transform.localScale = new Vector3(w + 0.22f, h + 0.22f, 1f); border.enabled = !empty; }
        if (box != null) { box.transform.localScale = new Vector3(w, h, 1f); box.enabled = !empty; }
        if (tail != null)
        {
            tail.transform.localScale = new Vector3(0.46f, 0.46f, 1f);
            tail.transform.localPosition = new Vector3(0f, -h * 0.5f - 0.02f, 0f);
            tail.enabled = !empty;
        }
        if (tailBorder != null)
        {
            tailBorder.transform.localScale = new Vector3(0.62f, 0.62f, 1f);
            tailBorder.transform.localPosition = new Vector3(0f, -h * 0.5f - 0.02f, 0f);
            tailBorder.enabled = !empty;
        }
    }

    // Mantem o balao dentro da area visivel da camera (so pros barks transientes).
    private void LateUpdate()
    {
        if (!clampScreen || box == null || !box.enabled) return;
        var cam = Camera.main;
        if (cam == null || !cam.orthographic) return;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector3 c = cam.transform.position;
        Vector3 ext = box.bounds.extents;
        const float margin = 0.15f;

        float minX = c.x - halfW + ext.x + margin;
        float maxX = c.x + halfW - ext.x - margin;
        float maxY = c.y + halfH - ext.y - margin;
        float minY = c.y - halfH + ext.y + margin;

        Vector3 p = transform.position;
        if (minX <= maxX) p.x = Mathf.Clamp(p.x, minX, maxX);
        if (maxY >= minY) p.y = Mathf.Clamp(p.y, minY, maxY);
        transform.position = p;
    }

    public void SetAlpha(float a)
    {
        if (quads != null)
            for (int i = 0; i < quads.Length; i++)
            {
                if (quads[i] == null) continue;
                var c = quadBase[i]; c.a *= a; quads[i].color = c;
            }
        if (tmp != null) { var tc = textBase; tc.a = a; tmp.color = tc; }
    }

    private IEnumerator LifeRoutine(float hold, float rise)
    {
        float t = 0f;
        Vector3 start = transform.position;
        while (t < hold)
        {
            t += Time.deltaTime;
            transform.position = start + Vector3.up * (rise * t);
            yield return null;
        }
        float f = 0f;
        while (f < 0.5f)
        {
            f += Time.deltaTime;
            SetAlpha(1f - f / 0.5f);
            yield return null;
        }
        Destroy(gameObject);
    }
}
