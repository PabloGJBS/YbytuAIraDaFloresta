using UnityEngine;

public class BossTrigger : MonoBehaviour
{
    [Tooltip("O chefe a iniciar quando o player cruzar esta linha.")]
    [SerializeField] private TractorBoss boss;

    private bool fired;
    private Transform player;

    private void Update()
    {
        if (fired || boss == null) return;
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go == null) return;
            player = go.transform;
        }
        if (player.position.x >= transform.position.x)
        {
            fired = true;
            boss.StartFight();
        }
    }

    private void OnDrawGizmos()
    {
        float x = transform.position.x;
        Gizmos.color = new Color(1f, 0.25f, 0.15f);
        Gizmos.DrawLine(new Vector3(x, -50f, 0f), new Vector3(x, 50f, 0f));
#if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(1f, 0.25f, 0.15f);
        UnityEditor.Handles.Label(new Vector3(x, 5.5f, 0f), "Boss Start");
#endif
    }
}
