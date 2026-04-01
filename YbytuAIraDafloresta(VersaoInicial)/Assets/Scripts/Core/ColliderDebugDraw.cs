using UnityEngine;

/// <summary>
/// Desenha o Collider2D do GameObject sempre (mesmo nao selecionado),
/// usando Gizmos. Util para debugar tamanho/offset de hitbox/colliders
/// no Scene view ou no Game view (com Gizmos ligado no toolbar).
/// </summary>
public class ColliderDebugDraw : MonoBehaviour
{
    [SerializeField] private Color wallColor = new Color(0.2f, 1f, 0.4f, 0.8f);
    [SerializeField] private Color triggerColor = new Color(1f, 0.4f, 0.4f, 0.8f);

    private void OnDrawGizmos()
    {
        if (!DebugFlags.ShowColliders) return;
        var cols = GetComponents<Collider2D>();
        if (cols == null || cols.Length == 0) return;

        foreach (var col in cols)
        {
            if (col == null) continue;
            Gizmos.color = col.isTrigger ? triggerColor : wallColor;

            switch (col)
            {
                case CapsuleCollider2D capsule:
                    DrawCapsule(capsule);
                    break;
                case BoxCollider2D box:
                    DrawBox(box);
                    break;
                case CircleCollider2D circle:
                    DrawCircle(circle);
                    break;
            }
        }
    }

    private void DrawCapsule(CapsuleCollider2D capsule)
    {
        Vector3 worldOffset = transform.TransformPoint(capsule.offset);
        Vector3 scale = transform.lossyScale;
        Vector2 size = new Vector2(capsule.size.x * Mathf.Abs(scale.x), capsule.size.y * Mathf.Abs(scale.y));
        Gizmos.matrix = Matrix4x4.TRS(worldOffset, transform.rotation, Vector3.one);
        // Aproxima a capsula com um wirecube
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, 0.01f));
        Gizmos.matrix = Matrix4x4.identity;
    }

    private void DrawBox(BoxCollider2D box)
    {
        Vector3 worldOffset = transform.TransformPoint(box.offset);
        Vector3 scale = transform.lossyScale;
        Vector2 size = new Vector2(box.size.x * Mathf.Abs(scale.x), box.size.y * Mathf.Abs(scale.y));
        Gizmos.matrix = Matrix4x4.TRS(worldOffset, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, 0.01f));
        Gizmos.matrix = Matrix4x4.identity;
    }

    private void DrawCircle(CircleCollider2D circle)
    {
        Vector3 worldOffset = transform.TransformPoint(circle.offset);
        float r = circle.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        Gizmos.DrawWireSphere(worldOffset, r);
    }
}
