using UnityEditor;
using UnityEngine;

public static class BuildFase2Cages
{
    private const string OutDir = "Assets/Prefabs/Animals/";

    [MenuItem("Tools/Setup/Build Fase2 Cages From Test")]
    public static void Run()
    {
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

        s1.localPosition = Vector3.zero;
        if (s2 != null) s2.localPosition = Vector3.zero;
        if (s3 != null) s3.localPosition = Vector3.zero;

        var col = copy.GetComponent<BoxCollider2D>();
        if (col == null) col = copy.AddComponent<BoxCollider2D>();
        var sr = s1.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            var b = sr.bounds; // world; copy esta na origem
            col.size = b.size;
            col.offset = new Vector2(b.center.x, b.center.y);
        }

        var cage = copy.GetComponent<AnimalCage>();
        if (cage == null) cage = copy.AddComponent<AnimalCage>();
        cage.intactStage = s1.gameObject;
        cage.damagedStage = s2 != null ? s2.gameObject : null;
        cage.brokenStage = s3 != null ? s3.gameObject : null;
        cage.hitsToDamage = 3;
        cage.hitsToBreak = 5;
        if (s2 != null) s2.gameObject.SetActive(false);
        if (s3 != null) s3.gameObject.SetActive(false);

        foreach (var s in new[] { s1, s2, s3 })
        {
            if (s == null) continue;
            var ssr = s.GetComponent<SpriteRenderer>();
            if (ssr != null) ssr.sortingOrder = 100;
            if (s.GetComponent<YSortRenderer>() == null) s.gameObject.AddComponent<YSortRenderer>();
        }

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
