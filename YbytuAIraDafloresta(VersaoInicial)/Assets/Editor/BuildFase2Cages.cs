using UnityEditor;
using UnityEngine;

/// <summary>
/// Converte as jaulas calibradas da caracterTest (Jaulas/JaulaCobra|JaulaOnca|JaulaJavali,
/// cada uma com 3 estagios Jaula1/2/3) em prefabs funcionais com AnimalCage + collider +
/// animais presos. Requer a caracterTest ABERTA. Menu Tools/Setup/Build Fase2 Cages From Test.
/// </summary>
public static class BuildFase2Cages
{
    private const string OutDir = "Assets/Prefabs/Animals/";

    [MenuItem("Tools/Setup/Build Fase2 Cages From Test")]
    public static void Run()
    {
        // Mapeamento por TAMANHO da jaula (os nomes dos containers na caracterTest ficaram
        // trocados): cobra = jaula menorzinha (0.35), javali = jaula media (0.75), onça = grande (2.0).
        Build("JaulaJavali", "CobraCage", new[] { "CobraAlly", "CobraAzulAlly", "CobraVerdeAlly" });
        Build("JaulaCobra", "JavaliCage", new[] { "JavaliAlly" });
        Build("JaulaOnca", "OncaCage", new[] { "OncaAlly" });
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BuildFase2Cages] OK. Prefabs CobraCage/JavaliCage/OncaCage em " + OutDir);
    }

    private static void Build(string srcName, string outName, string[] animalPrefabs)
    {
        var src = GameObject.Find(srcName);
        if (src == null) { Debug.LogError($"[BuildFase2Cages] '{srcName}' nao encontrado na cena (abra a caracterTest)."); return; }

        var copy = Object.Instantiate(src);
        copy.name = outName;
        copy.transform.position = Vector3.zero;

        var s1 = copy.transform.Find("Jaula1");
        var s2 = copy.transform.Find("Jaula2");
        var s3 = copy.transform.Find("Jaula3");
        if (s1 == null) { Debug.LogError($"[BuildFase2Cages] {srcName} sem Jaula1."); Object.DestroyImmediate(copy); return; }

        // Na caracterTest os 3 estagios ficavam ESPALHADOS so pra exibir/calibrar.
        // No jogo eles se SOBREPOEM (um ativo por vez), com base na origem da jaula.
        s1.localPosition = Vector3.zero;
        if (s2 != null) s2.localPosition = Vector3.zero;
        if (s3 != null) s3.localPosition = Vector3.zero;

        // collider (alvo do soco) a partir do estagio intacto
        var col = copy.GetComponent<BoxCollider2D>();
        if (col == null) col = copy.AddComponent<BoxCollider2D>();
        var sr = s1.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            var b = sr.bounds; // world; copy esta na origem
            col.size = b.size;
            col.offset = new Vector2(b.center.x, b.center.y);
        }

        // AnimalCage wirando os 3 estagios
        var cage = copy.GetComponent<AnimalCage>();
        if (cage == null) cage = copy.AddComponent<AnimalCage>();
        cage.intactStage = s1.gameObject;
        cage.damagedStage = s2 != null ? s2.gameObject : null;
        cage.brokenStage = s3 != null ? s3.gameObject : null;
        cage.hitsToDamage = 3;
        cage.hitsToBreak = 5;
        if (s2 != null) s2.gameObject.SetActive(false);
        if (s3 != null) s3.gameObject.SetActive(false);

        // Ordena por Y (igual props/personagens). Sem isso o chao da Stage2 (sortingOrder 3)
        // renderiza por cima da jaula e ela some.
        foreach (var s in new[] { s1, s2, s3 })
        {
            if (s == null) continue;
            // ordem base alta (visivel no editor, acima do chao=3); YSort refina em play.
            var ssr = s.GetComponent<SpriteRenderer>();
            if (ssr != null) ssr.sortingOrder = 100;
            if (s.GetComponent<YSortRenderer>() == null) s.gameObject.AddComponent<YSortRenderer>();
        }

        // animais presos como filhos (auto-detectados pela AnimalCage)
        int n = animalPrefabs.Length;
        for (int i = 0; i < n; i++)
        {
            var ap = AssetDatabase.LoadAssetAtPath<GameObject>(OutDir + animalPrefabs[i] + ".prefab");
            if (ap == null) { Debug.LogWarning("[BuildFase2Cages] prefab animal nao achado: " + animalPrefabs[i]); continue; }
            var a = (GameObject)PrefabUtility.InstantiatePrefab(ap, copy.transform);
            float x = n == 1 ? 0f : -0.55f + i * 0.55f;
            a.transform.localPosition = new Vector3(x, 0.1f, -0.01f);
        }

        PrefabUtility.SaveAsPrefabAsset(copy, OutDir + outName + ".prefab");
        Object.DestroyImmediate(copy);
        Debug.Log($"[BuildFase2Cages] {outName} ({n} animais) salvo.");
    }
}
