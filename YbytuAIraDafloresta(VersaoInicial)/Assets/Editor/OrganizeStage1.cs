using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OrganizeStage1
{
    private const string ScenePath = "Assets/Scenes/Stage1.unity";

    [MenuItem("Tools/Ybytu/Organize Stage1 Hierarchy")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject keptGlobals = null;
        var toKill = new List<GameObject>();
        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.name != "CombatGlobals") continue;
            if (keptGlobals == null) keptGlobals = go;
            else toKill.Add(go);
        }
        foreach (var g in toKill) Object.DestroyImmediate(g);

        // 2) Grupos
        var env = NewGroup("Environment", scene, null);
        var sky = NewGroup("Sky", scene, env.transform);
        var tiles = NewGroup("Tilesets", scene, env.transform);
        var props = NewGroup("Props", scene, env.transform);
        var gameplay = NewGroup("Gameplay", scene, null);
        var czGroup = NewGroup("CombatZones", scene, gameplay.transform);
        var systems = NewGroup("Systems", scene, null);
        var groups = new HashSet<GameObject> { env, sky, tiles, props, gameplay, czGroup, systems };

        foreach (var go in scene.GetRootGameObjects())
        {
            if (groups.Contains(go)) continue;
            string n = go.name;
            if (n == "Main Camera" || n == "AudioManager" || n == "CombatGlobals") continue; // ficam no root

            Transform t = null;
            if (n.StartsWith("SkyBackground") || n.StartsWith("Background-") || n.StartsWith("background-")) t = sky.transform;
            else if (n.StartsWith("TileSet")) t = tiles.transform;
            else if (n.StartsWith("Props")) t = props.transform;
            else if (n == "Stage1") t = env.transform;
            else if (n == "Player" || n == "StageZone" || n == "Troncos") t = gameplay.transform;
            else if (n.StartsWith("CombatZone")) t = czGroup.transform;
            else if (n == "StageManager" || n == "SceneBgm" || n == "GameOverManager") t = systems.transform;

            if (t != null) go.transform.SetParent(t, true);
            else Debug.Log($"[OrganizeStage1] sem grupo (fica root): {n}");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[OrganizeStage1] Hierarquia organizada + CombatGlobals deduplicado.");
    }

    private static GameObject NewGroup(string name, Scene scene, Transform parent)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        else SceneManager.MoveGameObjectToScene(go, scene);
        return go;
    }
}
