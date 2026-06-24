using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class JavaliArenaTest : MonoBehaviour
{
    [Header("Inimigos (1 quadrado por prefab)")]
    public List<GameObject> enemyPrefabs = new List<GameObject>();

    [Header("Javali")]
    public RuntimeAnimatorController javaliController;
    public Sprite javaliSprite;
    public int javaliMaxHealth = 100;
    public float javaliScale = 1f;

    [Header("Layout")]
    public Vector2 startPos = new Vector2(-14f, 0f);
    public float spacingX = 8f;     // distancia entre os quadrados
    public float pairGap = 2.4f;

    private void Start()
    {
        StartCoroutine(Build());
    }

    private IEnumerator Build()
    {
        for (int i = 0; i < enemyPrefabs.Count; i++)
        {
            if (enemyPrefabs[i] == null) continue;
            float cx = startPos.x + i * spacingX;
            float cy = startPos.y;

            var javali = BuildJavali(new Vector3(cx - pairGap * 0.5f, cy, 0f));

            var enemyGo = Instantiate(enemyPrefabs[i], new Vector3(cx + pairGap * 0.5f, cy, 0f), Quaternion.identity);
            enemyGo.name = "Inimigo_" + enemyPrefabs[i].name;
            enemyGo.layer = LayerMask.NameToLayer("Enemy");
            var enemy = enemyGo.GetComponent<EnemyController>();
            if (enemy != null) enemy.enabled = true;

            yield return null;

            if (javali != null) javali.Activate();
            if (enemy != null && javali != null)
                enemy.ForceAggroAlly(javali.GetComponent<HealthSystem>());
        }
    }

    private BoarAlly BuildJavali(Vector3 pos)
    {
        var go = new GameObject("Javali");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * javaliScale;

        var sr = go.AddComponent<SpriteRenderer>();
        if (javaliSprite != null) sr.sprite = javaliSprite;
        sr.sortingOrder = 50;

        var an = go.AddComponent<Animator>();
        if (javaliController != null) an.runtimeAnimatorController = javaliController;
        an.applyRootMotion = false;

        go.AddComponent<YSortRenderer>();

        var boar = go.AddComponent<BoarAlly>();
        boar.maxHealth = javaliMaxHealth;
        return boar;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.8f);
        for (int i = 0; i < enemyPrefabs.Count; i++)
        {
            Vector3 c = new Vector3(startPos.x + i * spacingX, startPos.y, 0f);
            Gizmos.DrawWireCube(c, new Vector3(spacingX * 0.85f, 4f, 0.1f));
        }
    }
}
