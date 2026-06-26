using UnityEngine;

public class WalkGapMarker : MonoBehaviour
{
    public Vector2 size = new Vector2(10f, 8f);
    public Color fill = new Color(0.3f, 1f, 0.4f, 0.22f);

    private void OnDrawGizmos()
    {
        var s = new Vector3(size.x, size.y, 0.1f);
        Gizmos.color = fill;
        Gizmos.DrawCube(transform.position, s);
        Gizmos.color = new Color(fill.r, fill.g, fill.b, 0.95f);
        Gizmos.DrawWireCube(transform.position, s);
    }
}
