using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Reorganiza as zonas de combate da Stage1: CZ4 vira a zona FINAL com os chefes
/// (x=90) e a CZ3 vira a pre-boss (x=62) com inimigos variados/escuros. Os chefes
/// passam a vir SO na zona final. Opera na CENA ATIVA (sem OpenScene) pra nao
/// reverter posicoes nao salvas (troncos). Menu: Tools/Ybytu/Reorganize Combat Zones (Boss in CZ4)
/// </summary>
public static class ReorganizeCombatZones
{
    private const string PrefabDir = "Assets/Prefabs/Enemies";

    private struct W
    {
        public string name;
        public float delay;
        public float interval;
        public (GameObject prefab, int count)[] enemies;
    }

    [MenuItem("Tools/Ybytu/Reorganize Combat Zones (Boss in CZ4)")]
    public static void Run()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "Stage1")
        {
            Debug.LogError($"[ReorgCZ] Cena ativa e '{scene.name}'. Abra a Stage1 primeiro. Abortado (nada alterado).");
            return;
        }

        var cz3 = GameObject.Find("CombatZone3");
        var cz4 = GameObject.Find("CombatZone4");
        if (cz3 == null || cz4 == null) { Debug.LogError("[ReorgCZ] CombatZone3/4 nao encontradas."); return; }

        // Swap de posicoes: CZ4 = final (x90), CZ3 = pre-boss (x62)
        SetX(cz3, 62f);
        SetX(cz4, 90f);

        var punk = Load("EnemyPunk_Enemy");
        var punkDark = Load("EnemyPunkDark_Enemy");
        var gangster = Load("EnemyGangster2_Enemy");
        var raider = Load("EnemyRaider3_Enemy");
        var brawler = Load("BrawlerGirl_Enemy");
        var brawlerDark = Load("BrawlerGirlEnemyDark_Enemy");
        var chefe1 = Load("EnemyChefe1_Enemy");
        var chefe1Black = Load("EnemyChefe1Black_Enemy");

        // CZ3 = pre-boss (variados, variacoes escuras, SEM chefe)
        SetWaves(cz3, new List<W>
        {
            new W { name = "Wave 1", delay = 1.2f, interval = 0.4f, enemies = new[] { (punkDark, 1), (gangster, 1) } },
            new W { name = "Wave 2", delay = 1.5f, interval = 0.5f, enemies = new[] { (brawlerDark, 1), (raider, 1) } },
        });

        // CZ4 = FINAL / BOSS
        SetWaves(cz4, new List<W>
        {
            new W { name = "Wave 1", delay = 1.0f, interval = 0.4f, enemies = new[] { (raider, 1), (gangster, 1) } },
            new W { name = "Wave 2", delay = 1.5f, interval = 0.5f, enemies = new[] { (brawler, 1), (raider, 1) } },
            new W { name = "Wave 3 - Chefes", delay = 2.0f, interval = 0.6f, enemies = new[] { (chefe1, 1), (chefe1Black, 1) } },
        });

        // Reforco: quando os 2 chefes chegam a meia-vida somada, chamam 1 de cada capanga
        SetBossReinforcements(cz4, 0.5f, new[] { punk, gangster, raider, brawler });

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ReorgCZ] OK. CZ4 = final/boss (x90), CZ3 = pre-boss escuros (x62). Chefes so na CZ4. Troncos preservados.");
    }

    private static GameObject Load(string name)
    {
        var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{name}.prefab");
        if (p == null) Debug.LogError($"[ReorgCZ] prefab nao encontrado: {name}");
        return p;
    }

    private static void SetX(GameObject go, float x)
    {
        var pos = go.transform.position; pos.x = x; go.transform.position = pos;
    }

    private static void SetBossReinforcements(GameObject zoneGo, float fraction, GameObject[] prefabs)
    {
        var cz = zoneGo.GetComponent<CombatZone>();
        if (cz == null) { Debug.LogError("[ReorgCZ] sem CombatZone em " + zoneGo.name); return; }

        var so = new SerializedObject(cz);
        so.FindProperty("bossReinforcementFraction").floatValue = fraction;
        var arr = so.FindProperty("bossReinforcements");
        arr.arraySize = prefabs.Length;
        for (int i = 0; i < prefabs.Length; i++)
            arr.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(cz);
    }

    private static void SetWaves(GameObject zoneGo, List<W> waves)
    {
        var cz = zoneGo.GetComponent<CombatZone>();
        if (cz == null) { Debug.LogError("[ReorgCZ] sem CombatZone em " + zoneGo.name); return; }

        var so = new SerializedObject(cz);
        var arr = so.FindProperty("waves");
        arr.arraySize = waves.Count;
        for (int i = 0; i < waves.Count; i++)
        {
            var w = waves[i];
            var elem = arr.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("waveName").stringValue = w.name;
            elem.FindPropertyRelative("delayBeforeWave").floatValue = w.delay;
            elem.FindPropertyRelative("spawnInterval").floatValue = w.interval;

            var en = elem.FindPropertyRelative("enemies");
            en.arraySize = w.enemies.Length;
            for (int j = 0; j < w.enemies.Length; j++)
            {
                var ee = en.GetArrayElementAtIndex(j);
                ee.FindPropertyRelative("enemyPrefab").objectReferenceValue = w.enemies[j].prefab;
                ee.FindPropertyRelative("count").intValue = w.enemies[j].count;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(cz);
    }
}
