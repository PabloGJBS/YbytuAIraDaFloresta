using UnityEngine;

public class StageZone : MonoBehaviour
{
    [Header("Limites do Cenario (Camera)")]
    [Tooltip("Transform que marca a borda esquerda do cenario - camera nunca passa daqui")]
    [SerializeField] private Transform stageStartEdge;
    [SerializeField] private Transform stageEndEdge;

    [Header("Spawn do Player")]
    [SerializeField] private Transform playerSpawn;

    [Header("Area Caminhavel")]
    [Tooltip("Canto inferior esquerdo da area onde o player pode andar")]
    [SerializeField] private Transform walkAreaMin;
    [Tooltip("Canto superior direito da area onde o player pode andar")]
    [SerializeField] private Transform walkAreaMax;

    public bool HasStageBounds => stageStartEdge != null && stageEndEdge != null;
    public float StageMinX => stageStartEdge != null ? stageStartEdge.position.x : 0f;
    public float StageMaxX => stageEndEdge != null ? stageEndEdge.position.x : 0f;

    public bool HasSpawn => playerSpawn != null;
    public Vector3 SpawnPos => playerSpawn != null ? playerSpawn.position : Vector3.zero;

    public bool HasWalkArea => walkAreaMin != null && walkAreaMax != null;
    public Vector2 WalkMin => walkAreaMin != null ? (Vector2)walkAreaMin.position : Vector2.zero;
    public Vector2 WalkMax => walkAreaMax != null ? (Vector2)walkAreaMax.position : Vector2.zero;

    private void OnDrawGizmos()
    {
        if (stageStartEdge != null) DrawVerticalLine(stageStartEdge.position.x, Color.cyan, "Start");
        if (stageEndEdge != null) DrawVerticalLine(stageEndEdge.position.x, Color.cyan, "End");

        if (walkAreaMin != null && walkAreaMax != null)
        {
            Vector2 min = walkAreaMin.position;
            Vector2 max = walkAreaMax.position;
            Vector3 center = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, 0f);
            Vector3 size = new Vector3(Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y), 0.01f);
            Gizmos.color = new Color(0f, 1f, 0f, 0.18f);
            Gizmos.DrawCube(center, size);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center, size);
        }

        if (playerSpawn != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(playerSpawn.position, 0.4f);
        }
    }

    private void DrawVerticalLine(float x, Color color, string label)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(new Vector3(x, -50f, 0f), new Vector3(x, 50f, 0f));
#if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.Label(new Vector3(x, 5f, 0f), label);
#endif
    }
}
