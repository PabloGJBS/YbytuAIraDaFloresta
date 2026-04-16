using System.Collections;
using UnityEngine;

/// <summary>
/// Zona de teste de inimigo: spawna 1 inimigo do prefab, escuta a morte dele,
/// e respawna depois de respawnDelay. Aplica EnemyZoneClamp no spawnado
/// pra ele nao sair do retangulo da zona. Usado na EnemyTest pra calibrar
/// movimento/golpes individualmente sem interferencia de outros inimigos.
/// </summary>
public class EnemyTestZone : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Vector2 spawnOffset = Vector2.zero;
    [SerializeField] private float respawnDelay = 1.5f;
    [SerializeField] private bool spawnOnStart = true;

    [Header("Confinamento (relativo ao transform da zona)")]
    [SerializeField] private Vector2 halfSize = new Vector2(15f, 3.5f);

    [Header("Visual")]
    [SerializeField] private Color gizmoColor = new Color(1f, 0.9f, 0.2f, 0.25f);

    [Header("Live Size Override (testes)")]
    [Tooltip("Se ambos componentes > 0, sobrescreve transform.localScale do inimigo spawnado em tempo real. " +
             "Deixar 0,0 cai no skin default. Encontrou um valor bom? Click direito no componente -> 'Apply Override to EnemySkin' pra salvar.")]
    [SerializeField] private Vector2 sizeOverride = Vector2.zero;

    [Header("Live Feet Y Offset (testes)")]
    [Tooltip("Toggle para sobrescrever o offset vertical que alinha os pes do inimigo com os do player. " +
             "Negativo abaixa, positivo eleva. ApplyToSkin salva no EnemySkin.")]
    [SerializeField] private bool useFeetYOverride = false;
    [SerializeField] private float feetYOverride = 0f;

    private EnemyController currentEnemy;
    private Coroutine respawnRoutine;

    public Vector2 Center => transform.position;
    public float MinX => transform.position.x - halfSize.x;
    public float MaxX => transform.position.x + halfSize.x;
    public float MinY => transform.position.y - halfSize.y;
    public float MaxY => transform.position.y + halfSize.y;

    private void Start()
    {
        if (spawnOnStart) Spawn();
    }

    private void LateUpdate()
    {
        if (currentEnemy == null) return;

        // Aplica override de tamanho em tempo real. LateUpdate pra rodar depois do
        // ApplySkin do EnemyController (Awake) sem disputa de ordem.
        if (sizeOverride.x > 0f && sizeOverride.y > 0f)
        {
            var t = currentEnemy.transform;
            if (Mathf.Abs(t.localScale.x - sizeOverride.x) > 0.0001f
                || Mathf.Abs(t.localScale.y - sizeOverride.y) > 0.0001f)
                t.localScale = new Vector3(sizeOverride.x, sizeOverride.y, t.localScale.z);
        }

        if (useFeetYOverride && currentEnemy.runtimeYOffset != feetYOverride)
            currentEnemy.runtimeYOffset = feetYOverride;
    }

#if UNITY_EDITOR
    [ContextMenu("Apply Override to EnemySkin")]
    private void ApplyOverrideToSkin()
    {
        var skin = ResolveSkinFromPrefab();
        if (skin == null) return;

        bool changed = false;
        if (sizeOverride.x > 0f && sizeOverride.y > 0f)
        {
            skin.spriteScale = sizeOverride;
            Debug.Log($"[EnemyTestZone] {name}: spriteScale={sizeOverride} salvo em {skin.name}.");
            changed = true;
        }
        if (useFeetYOverride)
        {
            skin.feetYOffset = feetYOverride;
            Debug.Log($"[EnemyTestZone] {name}: feetYOffset={feetYOverride} salvo em {skin.name}.");
            changed = true;
        }

        if (changed)
        {
            UnityEditor.EditorUtility.SetDirty(skin);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(skin);
        }
        else
        {
            Debug.LogWarning($"[EnemyTestZone] {name}: nenhum override ativo pra salvar.");
        }
    }

    private EnemySkin ResolveSkinFromPrefab()
    {
        if (enemyPrefab == null) { Debug.LogWarning($"[EnemyTestZone] {name}: enemyPrefab nulo."); return null; }
        var ec = enemyPrefab.GetComponent<EnemyController>();
        if (ec == null) { Debug.LogWarning($"[EnemyTestZone] {name}: prefab sem EnemyController."); return null; }
        var so = new UnityEditor.SerializedObject(ec);
        var skinProp = so.FindProperty("skin");
        var skin = skinProp != null ? skinProp.objectReferenceValue as EnemySkin : null;
        if (skin == null) Debug.LogWarning($"[EnemyTestZone] {name}: skin nulo no prefab.");
        return skin;
    }
#endif

    private void OnDisable()
    {
        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
        }
        if (currentEnemy != null)
            currentEnemy.OnEnemyDied -= HandleEnemyDied;
    }

    private void Spawn()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning($"[EnemyTestZone] {name}: enemyPrefab nao atribuido.");
            return;
        }

        Vector3 pos = (Vector2)transform.position + spawnOffset;
        GameObject go = Instantiate(enemyPrefab, pos, Quaternion.identity, transform);
        go.name = enemyPrefab.name + "_Instance";

        var clamp = go.AddComponent<EnemyZoneClamp>();
        clamp.MinX = MinX;
        clamp.MaxX = MaxX;
        clamp.MinY = MinY;
        clamp.MaxY = MaxY;

        currentEnemy = go.GetComponent<EnemyController>();
        if (currentEnemy != null)
            currentEnemy.OnEnemyDied += HandleEnemyDied;
    }

    private void HandleEnemyDied(int _)
    {
        if (currentEnemy != null)
            currentEnemy.OnEnemyDied -= HandleEnemyDied;
        currentEnemy = null;
        if (gameObject.activeInHierarchy && enabled)
            respawnRoutine = StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        respawnRoutine = null;
        Spawn();
    }

    private void OnDrawGizmos()
    {
        Vector3 center = transform.position;
        Vector3 size = new Vector3(halfSize.x * 2f, halfSize.y * 2f, 0.01f);

        Gizmos.color = gizmoColor;
        Gizmos.DrawCube(center, size);
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireCube(center, size);

        Vector3 spawnPos = (Vector2)transform.position + spawnOffset;
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(spawnPos, 0.25f);
    }
}
