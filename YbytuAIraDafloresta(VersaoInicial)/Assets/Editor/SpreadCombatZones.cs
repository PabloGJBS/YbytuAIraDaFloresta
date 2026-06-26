using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SpreadCombatZones
{
    private const string ScenePath = "Assets/Scenes/Stage1.unity";

    [MenuItem("Tools/Ybytu/Spread Combat Zones")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var cz1 = GameObject.Find("CombatZone1");
        var cz2 = GameObject.Find("CombatZone2");
        var cz3 = GameObject.Find("CombatZone3");
        if (cz1 == null || cz2 == null || cz3 == null)
        {
            Debug.LogError("[Spread] CombatZone1/2/3 nao encontradas."); return;
        }

        var cz4 = GameObject.Find("CombatZone4");
        if (cz4 == null)
        {
            cz4 = Object.Instantiate(cz2, cz2.transform.parent);
            cz4.name = "CombatZone4";
        }

        cz1.transform.position = new Vector3(2f, -3f, 0f);
        cz2.transform.position = new Vector3(33f, -3f, 0f);
        cz4.transform.position = new Vector3(62f, -3f, 0f);
        cz3.transform.position = new Vector3(90f, -3f, 0f);

        var smGo = GameObject.Find("StageManager");
        var sm = smGo != null ? smGo.GetComponent<StageManager>() : null;
        if (sm != null)
        {
            var cz4Comp = cz4.GetComponent<CombatZone>();
            var so = new SerializedObject(sm);
            var arr = so.FindProperty("combatZones");
            bool has = false;
            for (int i = 0; i < arr.arraySize; i++)
                if (arr.GetArrayElementAtIndex(i).objectReferenceValue == cz4Comp) has = true;
            if (!has)
            {
                arr.arraySize++;
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = cz4Comp;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[Spread] CZ4 registrada no StageManager.");
            }
        }
        else Debug.LogWarning("[Spread] StageManager nao encontrado.");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Spread] Zonas: CZ1=2, CZ2=33, CZ4=62, CZ3(boss)=90.");
    }
}
