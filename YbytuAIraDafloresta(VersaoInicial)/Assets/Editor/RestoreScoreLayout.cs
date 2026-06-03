using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Restaura o layout do quadro da StageScore para o arranjo final do usuario
/// (RANK no topo, TEMPO|Golpes em linha, Abatidos no meio, PONTUACAO embaixo),
/// usando as posicoes capturadas. One-shot. Menu: Tools/Ybytu/Restore Score Layout
/// </summary>
public static class RestoreScoreLayout
{
    private const string ScenePath = "Assets/Scenes/StageScore.unity";

    [MenuItem("Tools/Ybytu/Restore Score Layout")]
    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        SetPos("Canvas/ScoreFrame", new Vector2(21, -21));
        SetPos("Canvas/ScoreFrame/StageName", new Vector2(0, 288));
        SetPos("Canvas/ScoreFrame/RankCaption", new Vector2(0, 270));
        SetPos("Canvas/ScoreFrame/Rank", new Vector2(0, 227));
        SetPos("Canvas/ScoreFrame/TimeCaption", new Vector2(-69, 168));
        SetPos("Canvas/ScoreFrame/Time", new Vector2(-73, 121));
        SetPos("Canvas/ScoreFrame/HitsCaption", new Vector2(103, 170));
        SetPos("Canvas/ScoreFrame/Hits", new Vector2(106, 115));
        SetPos("Canvas/ScoreFrame/AbatidosCaption", new Vector2(0, 36));
        SetPos("Canvas/ScoreFrame/Abatidos", new Vector2(0, -10));
        SetPos("Canvas/ScoreFrame/ScoreCaption", new Vector2(-2, -93));
        SetPos("Canvas/ScoreFrame/Score", new Vector2(-7, -170));

        SetText("Canvas/ScoreFrame/HitsCaption", "Golpes");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[RestoreScoreLayout] Layout restaurado.");
    }

    private static void SetPos(string path, Vector2 pos)
    {
        var go = GameObject.Find(path);
        if (go == null) { Debug.LogWarning("[Restore] nao achou: " + path); return; }
        ((RectTransform)go.transform).anchoredPosition = pos;
    }

    private static void SetText(string path, string text)
    {
        var go = GameObject.Find(path);
        if (go == null) return;
        var t = go.GetComponent<TMP_Text>();
        if (t != null) t.text = text;
    }
}
