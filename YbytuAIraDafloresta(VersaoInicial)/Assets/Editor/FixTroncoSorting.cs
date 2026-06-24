using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FixTroncoSorting
{
    private const string ScenePath = "Assets/Scenes/Stage1.unity";

    [MenuItem("Tools/Ybytu/Fix Tronco Sorting")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        foreach (var name in new[] { "TroncoInterativo1", "TroncoInterativo2", "TroncoInterativo3" })
        {
            var go = GameObject.Find(name);
            if (go == null) { Debug.LogWarning("[FixTronco] nao achou: " + name); continue; }
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) { Debug.LogWarning("[FixTronco] sem SpriteRenderer: " + name); continue; }
            sr.sortingOrder = Mathf.RoundToInt(-go.transform.position.y * 100f);
            Debug.Log($"[FixTronco] {name}: sortingOrder={sr.sortingOrder}, sprite={(sr.sprite != null ? sr.sprite.name : "NULL")}");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[FixTronco] OK.");
    }
}
