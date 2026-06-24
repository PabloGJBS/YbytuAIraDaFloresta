using UnityEngine;

[DisallowMultipleComponent]
public class EnemyZoneClamp : MonoBehaviour
{
    public float MinX;
    public float MaxX;
    public float MinY;
    public float MaxY;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void LateUpdate()
    {
        Vector3 p = transform.position;
        bool clampedX = false;
        bool clampedY = false;

        if (p.x < MinX) { p.x = MinX; clampedX = true; }
        else if (p.x > MaxX) { p.x = MaxX; clampedX = true; }

        if (p.y < MinY) { p.y = MinY; clampedY = true; }
        else if (p.y > MaxY) { p.y = MaxY; clampedY = true; }

        if (clampedX || clampedY)
        {
            transform.position = p;
            if (rb != null)
            {
                Vector2 v = rb.linearVelocity;
                if (clampedX) v.x = 0f;
                if (clampedY) v.y = 0f;
                rb.linearVelocity = v;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = new Vector3((MinX + MaxX) * 0.5f, (MinY + MaxY) * 0.5f, 0f);
        Vector3 size = new Vector3(Mathf.Abs(MaxX - MinX), Mathf.Abs(MaxY - MinY), 0.01f);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.DrawWireCube(center, size);
    }
}
