using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Stage1Populator
{
    private const string ScenePath = "Assets/Scenes/Stage1.unity";

    [MenuItem("Tools/Setup/Populate Stage1 Waves")]
    public static void Run()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        var punk = LoadPrefab("Assets/Prefabs/Enemies/EnemyPunk_Enemy.prefab");
        var brawler = LoadPrefab("Assets/Prefabs/Enemies/BrawlerGirl_Enemy.prefab");
        var forte1 = LoadPrefab("Assets/Prefabs/Enemies/EnemyGangster2_Enemy.prefab"); // Gangster2 = Forte 1 (rapido)
        var forte2 = LoadPrefab("Assets/Prefabs/Enemies/EnemyRaider3_Enemy.prefab");   // Raider3 = Forte 2 (forte)
        var chefe = LoadPrefab("Assets/Prefabs/Enemies/EnemyChefe1_Enemy.prefab");

        if (punk == null || brawler == null || forte1 == null || forte2 == null || chefe == null)
        {
            Debug.LogError("[Stage1Populator] Algum prefab nao encontrado. Abortando.");
            return;
        }

        var cz1 = GameObject.Find("CombatZone1")?.GetComponent<CombatZone>();
        if (cz1 != null)
        {
            ConfigureWaves(cz1, new[] {
                new WaveSpec("Wave 1 - 2x Punk", 1.0f, 0.4f, new[] {
                    new EntrySpec(punk, 2),
                }),
                new WaveSpec("Wave 2 - Punk + Brawler", 1.5f, 0.4f, new[] {
                    new EntrySpec(punk, 1),
                    new EntrySpec(brawler, 1),
                }),
            });
            Debug.Log("[Stage1Populator] CombatZone1 -> 2 waves (3 inimigos fracos)");
        }
        else Debug.LogWarning("[Stage1Populator] CombatZone1 nao encontrada.");

        var cz2 = GameObject.Find("CombatZone2")?.GetComponent<CombatZone>();
        if (cz2 != null)
        {
            ConfigureWaves(cz2, new[] {
                new WaveSpec("Wave 1 - Brawler + Punk", 1.0f, 0.4f, new[] {
                    new EntrySpec(brawler, 1),
                    new EntrySpec(punk, 1),
                }),
                new WaveSpec("Wave 2 - Forte 1 (Gangster2) + Punk", 1.5f, 0.4f, new[] {
                    new EntrySpec(forte1, 1),
                    new EntrySpec(punk, 1),
                }),
                new WaveSpec("Wave 3 - Forte 2 (Raider3) + Brawler", 1.5f, 0.4f, new[] {
                    new EntrySpec(forte2, 1),
                    new EntrySpec(brawler, 1),
                }),
            });
            Debug.Log("[Stage1Populator] CombatZone2 -> 3 waves (6 inimigos, introduz Forte 1 e Forte 2)");
        }
        else Debug.LogWarning("[Stage1Populator] CombatZone2 nao encontrada.");

        // --- CombatZone 3 (boss) ---
        var cz3 = GameObject.Find("CombatZone3")?.GetComponent<CombatZone>();
        if (cz3 != null)
        {
            ConfigureWaves(cz3, new[] {
                new WaveSpec("Wave 1 - 2x Forte 1", 1.2f, 0.4f, new[] {
                    new EntrySpec(forte1, 2),
                }),
                new WaveSpec("Wave 2 - Forte 2 + Forte 1", 1.5f, 0.5f, new[] {
                    new EntrySpec(forte2, 1),
                    new EntrySpec(forte1, 1),
                }),
                new WaveSpec("Wave 3 - BOSS Chefe1 + Punk", 2.0f, 0.6f, new[] {
                    new EntrySpec(chefe, 1),
                    new EntrySpec(punk, 1),
                }),
            });
            Debug.Log("[Stage1Populator] CombatZone3 -> 3 waves (boss Chefe1 + capangas)");
        }
        else Debug.LogWarning("[Stage1Populator] CombatZone3 nao encontrada.");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Stage1Populator] Stage1 salvo. Total: 3 zonas / 8 waves / 14 inimigos.");
    }

    private static GameObject LoadPrefab(string path)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) Debug.LogError("[Stage1Populator] Prefab nao encontrado: " + path);
        return prefab;
    }

    private struct EntrySpec
    {
        public GameObject Prefab;
        public int Count;
        public EntrySpec(GameObject p, int c) { Prefab = p; Count = c; }
    }

    private struct WaveSpec
    {
        public string Name;
        public float DelayBefore;
        public float SpawnInterval;
        public EntrySpec[] Entries;
        public WaveSpec(string n, float d, float i, EntrySpec[] e)
        { Name = n; DelayBefore = d; SpawnInterval = i; Entries = e; }
    }

    private static void ConfigureWaves(CombatZone zone, WaveSpec[] waves)
    {
        var so = new SerializedObject(zone);
        var wavesProp = so.FindProperty("waves");
        wavesProp.arraySize = waves.Length;

        for (int wi = 0; wi < waves.Length; wi++)
        {
            var waveProp = wavesProp.GetArrayElementAtIndex(wi);
            waveProp.FindPropertyRelative("waveName").stringValue = waves[wi].Name;
            waveProp.FindPropertyRelative("delayBeforeWave").floatValue = waves[wi].DelayBefore;
            waveProp.FindPropertyRelative("spawnInterval").floatValue = waves[wi].SpawnInterval;

            var entriesProp = waveProp.FindPropertyRelative("enemies");
            entriesProp.arraySize = waves[wi].Entries.Length;
            for (int ei = 0; ei < waves[wi].Entries.Length; ei++)
            {
                var entryProp = entriesProp.GetArrayElementAtIndex(ei);
                entryProp.FindPropertyRelative("enemyPrefab").objectReferenceValue = waves[wi].Entries[ei].Prefab;
                entryProp.FindPropertyRelative("count").intValue = waves[wi].Entries[ei].Count;
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(zone);
    }
}
