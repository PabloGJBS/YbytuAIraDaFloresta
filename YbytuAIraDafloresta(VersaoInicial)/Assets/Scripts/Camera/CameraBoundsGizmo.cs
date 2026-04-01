using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraBoundsGizmo : MonoBehaviour
{
    public Color color = new Color(1f, 0.85f, 0.2f, 1f);
    public bool drawDiagonals = false;

    void OnDrawGizmos()
    {
        var cam = GetComponent<Camera>();
        if (cam == null || !cam.orthographic) return;

        float h = cam.orthographicSize * 2f;
        float w = h * cam.aspect;
        Vector3 c = new Vector3(transform.position.x, transform.position.y, 0f);
        Vector3 tl = c + new Vector3(-w * 0.5f,  h * 0.5f, 0f);
        Vector3 tr = c + new Vector3( w * 0.5f,  h * 0.5f, 0f);
        Vector3 bl = c + new Vector3(-w * 0.5f, -h * 0.5f, 0f);
        Vector3 br = c + new Vector3( w * 0.5f, -h * 0.5f, 0f);

        Gizmos.color = color;
        Gizmos.DrawLine(tl, tr);
        Gizmos.DrawLine(tr, br);
        Gizmos.DrawLine(br, bl);
        Gizmos.DrawLine(bl, tl);

        if (drawDiagonals)
        {
            Gizmos.DrawLine(tl, br);
            Gizmos.DrawLine(tr, bl);
        }
    }
}
