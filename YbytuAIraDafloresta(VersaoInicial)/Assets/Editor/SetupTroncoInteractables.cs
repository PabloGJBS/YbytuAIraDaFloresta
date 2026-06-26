using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupTroncoInteractables
{
    private const string ScenePath = "Assets/Scenes/Stage1.unity";

    [MenuItem("Tools/Ybytu/Setup Tronco Interactables")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        foreach (var name in new[] { "TroncoInterativo1", "TroncoInterativo2", "TroncoInterativo3" })
        {
            var go = GameObject.Find(name);
            if (go == null) { Debug.LogWarning("[SetupTronco] nao achou: " + name); continue; }

            if (go.GetComponent<InteractableHighlight>() == null)
            {
                go.AddComponent<InteractableHighlight>();
                EditorUtility.SetDirty(go);
            }

            var marker = go.GetComponentInChildren<EducationalMarker>(true);
            if (marker != null)
            {
                marker.useRandomFromPool = true;
                marker.requireInteraction = true;
                EditorUtility.SetDirty(marker);
            }
            else Debug.LogWarning("[SetupTronco] sem EducationalMarker em " + name);

            Debug.Log($"[SetupTronco] {name}: highlight + pool aleatorio OK (pos {go.transform.position})");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SetupTronco] OK.");
    }
}
