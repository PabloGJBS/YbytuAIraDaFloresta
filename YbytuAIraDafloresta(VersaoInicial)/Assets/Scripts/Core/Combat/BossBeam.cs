using UnityEngine;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class BossBeam : MonoBehaviour
{
    public static void Spawn(Vector2 start, Vector2 end, float thickness)
    {
        var go = new GameObject("BossBeam", typeof(LineRenderer));
        var lr = go.GetComponent<LineRenderer>();

        var shader = Shader.Find("Sprites/Default");
        if (shader != null) lr.material = new Material(shader);

        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = lr.endWidth = thickness;
        lr.numCapVertices = 0;
        lr.alignment = LineAlignment.TransformZ;
        lr.textureMode = LineTextureMode.Stretch;
        lr.sortingLayerName = "Default";
        lr.sortingOrder = 800;

        var c = new Color(1f, 0.8f, 0.25f, 1f); // laranja/amarelo brilhante
        lr.startColor = lr.endColor = c;

        var beam = go.AddComponent<BossBeam>();
        beam.StartCoroutine(beam.Flash(lr));
    }

    private IEnumerator Flash(LineRenderer lr)
    {
        const float dur = 0.2f;
        float t = 0f;
        Color baseC = lr.startColor;
        while (t < dur)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(1f - t / dur);
            var c = baseC; c.a = a;
            lr.startColor = lr.endColor = c;
            yield return null;
        }
        Destroy(gameObject);
    }
}
