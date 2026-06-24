using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Monta a luta final (CombatZone4) na cena aberta:
///  - remove os inimigos iniciais (Starting_*) da zona;
///  - instancia os 2 chefes (Chefe1 + Chefe1Black) posicionados, com EnemyController desligado;
///  - reconfigura a CombatZone4 pra ficar trancada (sem waves, testEmptyZoneDuration alto);
///  - cria/liga o FinalBossEncounter (chefes, spawn points, capangas crescentes, reuniao);
///  - cria/liga o BoarReunion (controller/sprite pegos de um BoarAlly da cena).
///
/// Rodar com a Stage1 ABERTA: menu Tools/Setup/Setup Final Boss Fight.
/// </summary>
public static class SetupFinalBossFight
{
    const string Chefe1 = "Assets/Prefabs/Enemies/EnemyChefe1_Enemy.prefab";
    const string Chefe1Black = "Assets/Prefabs/Enemies/EnemyChefe1Black_Enemy.prefab";
    const string Punk = "Assets/Prefabs/Enemies/EnemyPunk_Enemy.prefab";
    const string Gangster = "Assets/Prefabs/Enemies/EnemyGangster2_Enemy.prefab";
    const string Raider = "Assets/Prefabs/Enemies/EnemyRaider3_Enemy.prefab";
    const string Brawler = "Assets/Prefabs/Enemies/BrawlerGirl_Enemy.prefab";

    [MenuItem("Tools/Setup/Setup Final Boss Fight")]
    public static void Run()
    {
        var zoneGO = FindInScene("CombatZone4");
        if (zoneGO == null) { Debug.LogError("[FinalBoss] CombatZone4 nao encontrada na cena aberta."); return; }
        var zone = zoneGO.GetComponent<CombatZone>();
        if (zone == null) { Debug.LogError("[FinalBoss] CombatZone4 sem componente CombatZone."); return; }

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        Vector3 zoneCenter = zoneGO.transform.position;
        float groundY = -5f;

        // 1) Remover inimigos iniciais (Starting_*) e guardar posicoes
        var startPositions = new List<Vector3>();
        var toRemove = new List<GameObject>();
        foreach (Transform child in zoneGO.transform)
            if (child.name.StartsWith("Starting_") || child.name.StartsWith("Boss_"))
            {
                if (child.name.StartsWith("Starting_")) startPositions.Add(child.position);
                toRemove.Add(child.gameObject);
            }
        foreach (var go in toRemove) Object.DestroyImmediate(go);

        // posicoes default dos chefes se nao houver Starting_*
        Vector3 posA = startPositions.Count > 0 ? startPositions[0] : new Vector3(zoneCenter.x - 1.5f, groundY, 0f);
        Vector3 posB = startPositions.Count > 1 ? startPositions[1] : new Vector3(zoneCenter.x + 1.5f, groundY, 0f);

        // 2) Instanciar os 2 chefes (EnemyController desligado)
        var bosses = new List<EnemyController>();
        var b1 = InstantiateBoss(Chefe1, "Boss_Chefe1", zoneGO.transform, posA, enemyLayer);
        var b2 = InstantiateBoss(Chefe1Black, "Boss_Chefe1Black", zoneGO.transform, posB, enemyLayer);
        if (b1 != null) bosses.Add(b1);
        if (b2 != null) bosses.Add(b2);

        // 3) Reconfigurar a zona pra ficar trancada (sem waves; nunca auto-completa)
        var so = new SerializedObject(zone);
        var wavesProp = so.FindProperty("waves");
        if (wavesProp != null) wavesProp.ClearArray();
        var startProp = so.FindProperty("startingEnemies");
        if (startProp != null) startProp.ClearArray();
        var testDur = so.FindProperty("testEmptyZoneDuration");
        if (testDur != null) testDur.floatValue = 99999f;
        so.ApplyModifiedProperties();

        // 4) Spawn points dos capangas (reusa os SpawnA/SpawnB da zona)
        var spawnPts = new List<Transform>();
        foreach (var t in zoneGO.GetComponentsInChildren<Transform>(true))
            if (t != zoneGO.transform && t.name.ToLower().Contains("spawn")) spawnPts.Add(t);
        if (spawnPts.Count == 0) { spawnPts.Add(b1 != null ? b1.transform : zoneGO.transform); }

        // 5) BoarReunion (controller/sprite de um BoarAlly da cena)
        var reunionGO = FindInScene("FinalBossReunion") ?? new GameObject("FinalBossReunion");
        if (reunionGO.transform.parent == null) reunionGO.transform.SetParent(zoneGO.transform.parent, true);
        var reunion = reunionGO.GetComponent<BoarReunion>() ?? reunionGO.AddComponent<BoarReunion>();
        var anyBoar = Object.FindObjectsByType<BoarAlly>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (anyBoar != null)
        {
            var anim = anyBoar.GetComponentInChildren<Animator>();
            if (anim != null) reunion.boarController = anim.runtimeAnimatorController;
            var bsr = anyBoar.GetComponentInChildren<SpriteRenderer>();
            if (bsr != null) reunion.previewSprite = bsr.sprite;
        }
        reunion.groundY = groundY;
        EditorUtility.SetDirty(reunion);

        // 6) FinalBossEncounter (liga tudo)
        var encGO = FindInScene("FinalBossEncounter") ?? new GameObject("FinalBossEncounter");
        if (encGO.transform.parent == null) encGO.transform.SetParent(zoneGO.transform.parent, true);
        var enc = encGO.GetComponent<FinalBossEncounter>() ?? encGO.AddComponent<FinalBossEncounter>();
        enc.zone = zone;
        enc.bosses = bosses;
        enc.capangaSpawnPoints = spawnPts;
        enc.wave1 = LoadList(Punk, Punk);
        enc.wave2 = LoadList(Gangster, Gangster, Raider);
        enc.wave3 = LoadList(Raider, Raider, Brawler, Brawler);
        enc.reunion = reunion;
        EditorUtility.SetDirty(enc);

        EditorSceneManager.MarkSceneDirty(zoneGO.scene);
        EditorSceneManager.SaveScene(zoneGO.scene);
        Debug.Log($"[FinalBoss] OK. Chefes={bosses.Count} spawnPts={spawnPts.Count} capangas={enc.wave1.Count}/{enc.wave2.Count}/{enc.wave3.Count}. Cena salva.");
    }

    static EnemyController InstantiateBoss(string path, string name, Transform parent, Vector3 pos, int layer)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { Debug.LogError("[FinalBoss] Prefab nao encontrado: " + path); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = pos;
        if (layer >= 0) go.layer = layer;
        var ec = go.GetComponent<EnemyController>();
        if (ec != null) ec.enabled = false; // o FinalBossEncounter liga quando a zona ativa
        return ec;
    }

    static List<GameObject> LoadList(params string[] paths)
    {
        var list = new List<GameObject>();
        foreach (var p in paths)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (go != null) list.Add(go);
        }
        return list;
    }

    static GameObject FindInScene(string name)
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (go.name == name) return go;
        return null;
    }
}
