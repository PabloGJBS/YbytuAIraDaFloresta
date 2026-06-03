using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Configura o fluxo da Fase 1 de ponta a ponta:
/// 1) Cria/atualiza o StageData (Stage1 como cena de gameplay, OutroCutscene como fim).
/// 2) Pluga esse StageData no GameFlowManager.stages (na cena Disclaimer).
/// 3) Garante Stage1 e OutroCutscene no Build Settings.
/// Rodar DEPOIS de "Build OutroCutscene Scene".
/// Menu: Tools/Ybytu/Setup Fase 1 Flow
/// </summary>
public static class Fase1FlowSetup
{
    private const string StageAssetPath = "Assets/Data/Stages/Stage01.asset";
    private const string DisclaimerScene = "Assets/Scenes/Disclaimer.unity";

    [MenuItem("Tools/Ybytu/Setup Fase 1 Flow")]
    public static void Setup()
    {
        // 1) StageData da Fase 1
        if (!AssetDatabase.IsValidFolder("Assets/Data/Stages"))
            AssetDatabase.CreateFolder("Assets/Data", "Stages");

        var stage = AssetDatabase.LoadAssetAtPath<StageData>(StageAssetPath);
        if (stage == null)
        {
            stage = ScriptableObject.CreateInstance<StageData>();
            AssetDatabase.CreateAsset(stage, StageAssetPath);
        }
        stage.stageName = "A Entrada da Floresta";
        stage.stageIndex = 0;
        stage.description = "Fase 1";
        stage.gameplaySceneName = "Stage1";
        stage.introCutsceneId = "";   // o prologo global ja eh a intro
        stage.outroCutsceneId = "";   // fim de fase -> tela de score (StageScore), e o score leva a cutscene final
        stage.unlockedByDefault = true;
        EditorUtility.SetDirty(stage);
        AssetDatabase.SaveAssets();

        // 2) Plugar no GameFlowManager (cena Disclaimer = boot)
        var scene = EditorSceneManager.OpenScene(DisclaimerScene, OpenSceneMode.Single);
        GameFlowManager gfm = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            gfm = root.GetComponentInChildren<GameFlowManager>(true);
            if (gfm != null) break;
        }
        if (gfm == null)
        {
            Debug.LogError("[Fase1FlowSetup] GameFlowManager nao encontrado na Disclaimer.");
        }
        else
        {
            var so = new SerializedObject(gfm);
            var prop = so.FindProperty("stages");
            prop.arraySize = 1;
            prop.GetArrayElementAtIndex(0).objectReferenceValue = stage;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Fase1FlowSetup] GameFlowManager.stages = [Stage01].");
        }

        // 3) Build Settings
        var scenes = EditorBuildSettings.scenes.ToList();
        void Ensure(string path)
        {
            if (scenes.All(s => s.path != path))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                Debug.Log($"[Fase1FlowSetup] + build scene: {path}");
            }
        }
        Ensure("Assets/Scenes/Stage1.unity");
        Ensure("Assets/Scenes/StageScore.unity");
        Ensure("Assets/Scenes/OutroCutscene.unity");
        EditorBuildSettings.scenes = scenes.ToArray();

        Debug.Log("[Fase1FlowSetup] Fluxo da Fase 1 configurado.");
    }
}
