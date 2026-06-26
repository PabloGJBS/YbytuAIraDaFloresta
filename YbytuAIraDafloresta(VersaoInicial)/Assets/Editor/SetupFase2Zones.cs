using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupFase2Zones
{
    private const string Pfx = "Assets/Prefabs/Enemies/";
    private const string PunkAlt = Pfx + "EnemyPunkAlt_Enemy.prefab";
    private const string GangAlt = Pfx + "EnemyGangster2Alt_Enemy.prefab";
    private const string RaidAlt = Pfx + "EnemyRaider3Alt_Enemy.prefab";
    private const string BrawAlt = Pfx + "BrawlerGirlAlt_Enemy.prefab";

    private const int ChefeHp = 150;
    private const int ChefeScore = 1500;

    [MenuItem("Tools/Setup/Setup Fase2 Combat Zones")]
    public static void Run()
    {
        string chefe1 = MakeReducedChefe(
            "Assets/Data/EnemyData/EnemyChefe1_EnemyData.asset",
            Pfx + "EnemyChefe1_Enemy.prefab", "EnemyChefe1Zone");
        string chefe1Black = MakeReducedChefe(
            "Assets/Data/EnemyData/EnemyChefe1Black_EnemyData.asset",
            Pfx + "EnemyChefe1Black_Enemy.prefab", "EnemyChefe1BlackZone");

        var z1 = FindZone("CombatZone1");
        var z2 = FindZone("CombatZone2");
        var z3 = FindZone("CombatZone3");
        if (z1 == null || z2 == null || z3 == null)
        {
            Debug.LogError("[Fase2Zones] CombatZone1/2/3 nao encontradas na cena ativa. Abra a Stage2.unity.");
            return;
        }

        Cleanup(z1);
        SetWaves(z1, new List<List<(string, int)>> {
            new List<(string, int)> { (PunkAlt, 2), (GangAlt, 1) },
        });
        WireMiniBoss(z1, chefe1, new List<string> { RaidAlt, BrawAlt, PunkAlt });

        Cleanup(z2);
        SetWaves(z2, new List<List<(string, int)>> {
            new List<(string, int)> { (RaidAlt, 2), (BrawAlt, 1) },
        });
        WireMiniBoss(z2, chefe1Black, new List<string> { GangAlt, PunkAlt, RaidAlt });

        Cleanup(z3);
        SetWaves(z3, new List<List<(string, int)>> {
            new List<(string, int)> { (PunkAlt, 2), (GangAlt, 1) },
            new List<(string, int)> { (RaidAlt, 2), (BrawAlt, 1) },
        });
        SetStartingEnemies(z3, new EnemyController[0]);

        EditorSceneManager.MarkSceneDirty(z1.gameObject.scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.Refresh();
        Debug.Log("[Fase2Zones] OK. Zonas 1/2 com mini-boss + reforco a 50%; zona 3 com comuns Alt.");
    }

    private static void WireMiniBoss(CombatZone cz, string chefePrefabPath, List<string> reinforcementPaths)
    {
        var spawnPoints = GetSpawnPoints(cz);

        var prefab = Load<GameObject>(chefePrefabPath);
        var chefe = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        chefe.transform.SetParent(cz.transform, true);
        var camRight = GetTransformField(cz, "cameraLimitRight");
        Vector3 pos;
        if (camRight != null) pos = new Vector3(camRight.position.x - 3.5f, camRight.position.y, 0f);
        else if (spawnPoints.Count > 0) pos = spawnPoints[0].position;
        else pos = new Vector3(cz.transform.position.x + 4f, -3f, 0f);
        chefe.transform.position = pos;
        chefe.name = "MiniBoss_" + prefab.name;
        var chefeEc = chefe.GetComponent<EnemyController>();
        if (chefeEc != null) chefeEc.enabled = false; // convencao de startingEnemy

        SetStartingEnemies(cz, new[] { chefeEc });

        // ZoneMiniBoss
        var zmbGo = new GameObject("ZoneMiniBoss");
        zmbGo.transform.SetParent(cz.transform, false);
        var zmb = zmbGo.AddComponent<ZoneMiniBoss>();
        zmb.zone = cz;
        zmb.boss = chefeEc;
        zmb.spawnPoints = new List<Transform>(spawnPoints);
        zmb.reinforcements = new List<GameObject>();
        foreach (var p in reinforcementPaths)
        {
            var go = Load<GameObject>(p);
            if (go != null) zmb.reinforcements.Add(go);
        }
        zmb.threshold = 0.5f;
        EditorUtility.SetDirty(zmb);
    }

    // ---- chefe rebaixado ----
    private static string MakeReducedChefe(string baseDataPath, string basePrefabPath, string newPrefix)
    {
        string newDataPath = "Assets/Data/EnemyData/" + newPrefix + "_EnemyData.asset";
        if (Load<EnemyData>(newDataPath) == null)
        {
            AssetDatabase.CopyAsset(baseDataPath, newDataPath);
            AssetDatabase.ImportAsset(newDataPath);
        }
        var data = Load<EnemyData>(newDataPath);
        var baseData = Load<EnemyData>(baseDataPath);
        data.maxHealth = ChefeHp;
        data.scoreValue = ChefeScore;
        data.enemyName = (baseData != null ? baseData.enemyName : newPrefix) + " (Capanga)";
        EditorUtility.SetDirty(data);

        string newPrefabPath = "Assets/Prefabs/Enemies/" + newPrefix + "_Enemy.prefab";
        if (Load<GameObject>(newPrefabPath) == null)
        {
            AssetDatabase.CopyAsset(basePrefabPath, newPrefabPath);
            AssetDatabase.ImportAsset(newPrefabPath);
        }
        var root = PrefabUtility.LoadPrefabContents(newPrefabPath);
        root.name = newPrefix + "_Enemy";
        foreach (var comp in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (comp == null) continue;
            var so = new SerializedObject(comp);
            var it = so.GetIterator();
            bool changed = false;
            while (it.NextVisible(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (it.objectReferenceValue is EnemyData) { it.objectReferenceValue = data; changed = true; }
            }
            if (changed) so.ApplyModifiedPropertiesWithoutUndo();
        }
        PrefabUtility.SaveAsPrefabAsset(root, newPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log($"[Fase2Zones] Chefe reduzido: {newPrefabPath} (HP {ChefeHp}).");
        return newPrefabPath;
    }

    private static void SetWaves(CombatZone cz, List<List<(string path, int count)>> waves)
    {
        var so = new SerializedObject(cz);
        var wp = so.FindProperty("waves");
        wp.arraySize = waves.Count;
        for (int i = 0; i < waves.Count; i++)
        {
            var wEl = wp.GetArrayElementAtIndex(i);
            wEl.FindPropertyRelative("waveName").stringValue = "Fase2 Wave " + (i + 1);
            wEl.FindPropertyRelative("delayBeforeWave").floatValue = i == 0 ? 0.4f : 1.5f;
            wEl.FindPropertyRelative("spawnInterval").floatValue = 0.5f;
            var ep = wEl.FindPropertyRelative("enemies");
            ep.arraySize = waves[i].Count;
            for (int j = 0; j < waves[i].Count; j++)
            {
                var enEl = ep.GetArrayElementAtIndex(j);
                enEl.FindPropertyRelative("enemyPrefab").objectReferenceValue = Load<GameObject>(waves[i][j].path);
                enEl.FindPropertyRelative("count").intValue = waves[i][j].count;
            }
        }
        so.ApplyModifiedProperties();
    }

    private static void SetStartingEnemies(CombatZone cz, EnemyController[] enemies)
    {
        var so = new SerializedObject(cz);
        var p = so.FindProperty("startingEnemies");
        p.arraySize = enemies.Length;
        for (int i = 0; i < enemies.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = enemies[i];
        var br = so.FindProperty("bossReinforcements");
        if (br != null) br.arraySize = 0;
        so.ApplyModifiedProperties();
    }

    private static Transform GetTransformField(CombatZone cz, string field)
    {
        var so = new SerializedObject(cz);
        var p = so.FindProperty(field);
        return p != null ? p.objectReferenceValue as Transform : null;
    }

    private static List<Transform> GetSpawnPoints(CombatZone cz)
    {
        var list = new List<Transform>();
        var so = new SerializedObject(cz);
        var p = so.FindProperty("spawnPoints");
        for (int i = 0; i < p.arraySize; i++)
            if (p.GetArrayElementAtIndex(i).objectReferenceValue is Transform t) list.Add(t);
        return list;
    }

    private static void Cleanup(CombatZone cz)
    {
        var toDelete = new List<GameObject>();
        foreach (Transform child in cz.transform)
            if (child.name.StartsWith("Starting_") || child.name.StartsWith("MiniBoss_") || child.name == "ZoneMiniBoss")
                toDelete.Add(child.gameObject);
        foreach (var go in toDelete) Object.DestroyImmediate(go);
    }

    private static CombatZone FindZone(string name)
    {
        foreach (var cz in Object.FindObjectsByType<CombatZone>(FindObjectsSortMode.None))
            if (cz.gameObject.name == name) return cz;
        return null;
    }

    private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
}
