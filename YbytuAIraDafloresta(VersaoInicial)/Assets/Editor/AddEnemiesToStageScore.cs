using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AddEnemiesToStageScore
{
    private const string ScenePath = "Assets/Scenes/StageScore.unity";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("Tools/Ybytu/Add Enemies to StageScore")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        var frame = GameObject.Find("Canvas/ScoreFrame");
        if (frame == null) { Debug.LogError("[AddEnemies] Canvas/ScoreFrame nao encontrado."); return; }

        Kill(frame.transform, "AbatidosCaption");
        Kill(frame.transform, "Abatidos");

        MakeText("AbatidosCaption", frame.transform, font, "Abatidos", 24, FontStyles.Bold,
            new Color(0.75f, 0.78f, 0.72f), new Vector2(0, 36), new Vector2(330, 44));
        var value = MakeText("Abatidos", frame.transform, font, "0", 44, FontStyles.Bold,
            new Color(0.95f, 0.7f, 0.55f), new Vector2(0, -10), new Vector2(330, 80));

        var manager = GameObject.Find("StageScore_Manager");
        if (manager == null) { Debug.LogError("[AddEnemies] StageScore_Manager nao encontrado."); return; }
        var ui = manager.GetComponent<StageScoreUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("enemiesText").objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[AddEnemies] Abatidos adicionado ao quadro da StageScore.");
    }

    private static void Kill(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t != null) Object.DestroyImmediate(t.gameObject);
    }

    private static TMP_Text MakeText(string name, Transform parent, TMP_FontAsset font, string text,
        float size, FontStyles style, Color color, Vector2 pos, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        var rt = tmp.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = pos;
        return tmp;
    }
}
