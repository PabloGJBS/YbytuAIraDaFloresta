using UnityEngine;
using TMPro;
using System.Collections;

public class RetaliateTelegraph : MonoBehaviour
{
    public static RetaliateTelegraph Spawn(Transform enemy, SpriteRenderer source, float duration)
    {
        var go = new GameObject("RetaliateTelegraph");
        go.transform.SetParent(enemy, false);
        go.transform.localPosition = Vector3.zero;
        var fx = go.AddComponent<RetaliateTelegraph>();
        fx.Init(source, duration);
        return fx;
    }

    private SpriteRenderer source;
    private SpriteRenderer glow;
    private TextMeshPro bang;
    private float duration;

    private void Init(SpriteRenderer src, float dur)
    {
        source = src;
        duration = dur;

        if (source != null && source.sprite != null)
        {
            var gGo = new GameObject("Glow");
            gGo.transform.SetParent(transform, false);
            gGo.transform.localScale = Vector3.one * 1.14f;
            glow = gGo.AddComponent<SpriteRenderer>();
            glow.sprite = source.sprite;
            glow.flipX = source.flipX;
            glow.sortingLayerID = source.sortingLayerID;
            glow.sortingOrder = source.sortingOrder - 1;
            glow.color = new Color(1f, 0.12f, 0.1f, 0.85f);
        }

        // "!" grande acima do inimigo.
        var bGo = new GameObject("Bang", typeof(TextMeshPro));
        bGo.transform.SetParent(transform, false);
        bGo.transform.localPosition = Vector3.up * 2.6f;
        bang = bGo.GetComponent<TextMeshPro>();
        var f = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (f != null) bang.font = f;
        bang.text = "!";
        bang.fontSize = 8f;
        bang.fontStyle = FontStyles.Bold;
        bang.alignment = TextAlignmentOptions.Center;
        bang.color = new Color(1f, 0.2f, 0.15f);
        bang.sortingOrder = 1000;

        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = (Mathf.Sin(Time.unscaledTime * 20f) + 1f) * 0.5f;

            if (glow != null)
            {
                if (source != null)
                {
                    glow.sprite = source.sprite;
                    glow.flipX = source.flipX;
                    glow.sortingOrder = source.sortingOrder - 1;
                }
                var c = glow.color; c.a = Mathf.Lerp(0.4f, 1f, p); glow.color = c;
            }
            if (bang != null)
                bang.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.18f, p);

            yield return null;
        }
        Destroy(gameObject);
    }

    public void Stop() => Destroy(gameObject);
}
