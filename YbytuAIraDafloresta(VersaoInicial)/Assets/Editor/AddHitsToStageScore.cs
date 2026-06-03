using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Adiciona o contador de HITS ao quadro da StageScore (sem rebuildar a cena,
/// preservando os ajustes manuais). Insere HitsCaption + Hits abaixo do RANK e
/// religa StageScoreUI.hitsText. Idempotente. Menu: Tools/Ybytu/Add Hits to StageScore
/// </summary>
public static class AddHitsToStageScore
{
    private const string ScenePath = "Assets/Scenes/StageScore.unity";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("Tools/Ybytu/Add Hits to StageScore")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        var frame = GameObject.Find("Canvas/ScoreFrame");
        if (frame == null) { Debug.LogError("[AddHits] Canvas/ScoreFrame nao encontrado."); return; }

        Kill(frame.transform, "HitsCaption");
        Kill(frame.transform, "Hits");

        MakeText("HitsCaption", frame.transform, font, "HITS", 24, FontStyles.Bold,
            new Color(0.75f, 0.78f, 0.72f), new Vector2(0, -300), new Vector2(330, 44));
        var value = MakeText("Hits", frame.transform, font, "0", 44, FontStyles.Bold,
            new Color(0.85f, 0.95f, 0.9f), new Vector2(0, -346), new Vector2(330, 80));

        var manager = GameObject.Find("StageScore_Manager");
        if (manager == null) { Debug.LogError("[AddHits] StageScore_Manager nao encontrado."); return; }
        var ui = manager.GetComponent<StageScoreUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("hitsText").objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[AddHits] HITS adicionado ao quadro da StageScore.");
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
