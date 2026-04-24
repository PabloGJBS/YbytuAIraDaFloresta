using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class IntroCutsceneSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/IntroCutscene.unity";
    private const string PtBrJsonPath = "Assets/Localization/pt-BR.json";
    private const string EnUsJsonPath = "Assets/Localization/en-US.json";
    private const string CutsceneSpritesDir = "Assets/Sprites/UI/IntroCutscene";
    private const string Scene01Sprite = CutsceneSpritesDir + "/Cutscene_01_AldeiaEmPaz.png";
    private const string Scene02Sprite = CutsceneSpritesDir + "/Cutscene_02_Invasao.png";
    private const string Scene03Sprite = CutsceneSpritesDir + "/Cutscene_03_Ressurreicao.png";

    [MenuItem("Tools/Ybytu/Build IntroCutscene Scene")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponent<Camera>() != null) continue;
            Object.DestroyImmediate(root);
        }

        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        camGo.AddComponent<AudioListener>();
        SceneManager.MoveGameObjectToScene(camGo, scene);

        var existingCam = GameObject.Find("Main Camera");
        if (existingCam != null && existingCam != camGo) Object.DestroyImmediate(existingCam);

        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        SceneManager.MoveGameObjectToScene(canvasGo, scene);

        var bgGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImage = bgGo.GetComponent<Image>();
        bgImage.color = Color.black;
        bgImage.preserveAspect = false;
        bgImage.raycastTarget = false;

        const float topBarHeight = 100f;
        const float bottomBarHeight = 220f;

        var topBarGo = new GameObject("TopBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        topBarGo.transform.SetParent(canvasGo.transform, false);
        var topRect = topBarGo.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0f, 1f);
        topRect.anchorMax = new Vector2(1f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = Vector2.zero;
        topRect.sizeDelta = new Vector2(0f, topBarHeight);
        var topBarImage = topBarGo.GetComponent<Image>();
        topBarImage.color = Color.black;
        topBarImage.raycastTarget = false;

        var bottomBarGo = new GameObject("BottomBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bottomBarGo.transform.SetParent(canvasGo.transform, false);
        var bottomRect = bottomBarGo.GetComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0f, 0f);
        bottomRect.anchorMax = new Vector2(1f, 0f);
        bottomRect.pivot = new Vector2(0.5f, 0f);
        bottomRect.anchoredPosition = Vector2.zero;
        bottomRect.sizeDelta = new Vector2(0f, bottomBarHeight);
        var bottomBarImage = bottomBarGo.GetComponent<Image>();
        bottomBarImage.color = Color.black;
        bottomBarImage.raycastTarget = false;

        var dialogTextGo = new GameObject("DialogText", typeof(RectTransform));
        dialogTextGo.transform.SetParent(bottomBarGo.transform, false);
        var textRect = dialogTextGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(120f, 30f);
        textRect.offsetMax = new Vector2(-120f, -30f);
        var dialogText = dialogTextGo.AddComponent<TextMeshProUGUI>();
        dialogText.fontSize = 30f;
        dialogText.color = Color.white;
        dialogText.alignment = TextAlignmentOptions.MidlineLeft;
        dialogText.enableWordWrapping = true;
        dialogText.text = "";
        dialogText.raycastTarget = false;

        var promptGo = new GameObject("ContinuePrompt", typeof(RectTransform));
        promptGo.transform.SetParent(bottomBarGo.transform, false);
        var promptRect = promptGo.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(1f, 0f);
        promptRect.anchorMax = new Vector2(1f, 0f);
        promptRect.pivot = new Vector2(1f, 0f);
        promptRect.anchoredPosition = new Vector2(-30f, 16f);
        promptRect.sizeDelta = new Vector2(280f, 32f);
        var promptText = promptGo.AddComponent<TextMeshProUGUI>();
        promptText.fontSize = 20f;
        promptText.color = new Color(1f, 1f, 1f, 0.7f);
        promptText.alignment = TextAlignmentOptions.MidlineRight;
        promptText.text = "Espaço / A  ▼";
        promptText.raycastTarget = false;
        promptGo.SetActive(false);

        var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        SceneManager.MoveGameObjectToScene(eventSystemGo, scene);

        var managerGo = new GameObject("IntroCutscene_Manager");
        SceneManager.MoveGameObjectToScene(managerGo, scene);

        var locManager = managerGo.AddComponent<LocalizationManager>();
        var ptAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(PtBrJsonPath);
        var enAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(EnUsJsonPath);
        var locType = typeof(LocalizationManager);
        var langField = locType.GetField("availableLanguages", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (langField != null)
            langField.SetValue(locManager, new[] { ptAsset, enAsset });

        var textPlayer = managerGo.AddComponent<CutsceneTextPlayer>();
        SetField(textPlayer, "displayText", dialogText);
        SetField(textPlayer, "continuePrompt", promptGo);
        SetField(textPlayer, "playOnStart", false);
        SetField(textPlayer, "waitForInput", true);
        SetField(textPlayer, "charDelay", 0.03f);

        var prologue = managerGo.AddComponent<PrologueCutsceneController>();
        SetField(prologue, "backgroundImage", bgImage);
        SetField(prologue, "textPlayer", textPlayer);
        SetField(prologue, "playOnStart", true);
        SetField(prologue, "delayBetweenScenes", 0.6f);

        var sprite01 = AssetDatabase.LoadAssetAtPath<Sprite>(Scene01Sprite);
        var sprite02 = AssetDatabase.LoadAssetAtPath<Sprite>(Scene02Sprite);
        var sprite03 = AssetDatabase.LoadAssetAtPath<Sprite>(Scene03Sprite);
        if (sprite01 == null) Debug.LogWarning($"[IntroCutsceneSceneBuilder] sprite nao encontrado: {Scene01Sprite}");
        if (sprite02 == null) Debug.LogWarning($"[IntroCutsceneSceneBuilder] sprite nao encontrado: {Scene02Sprite}");
        if (sprite03 == null) Debug.LogWarning($"[IntroCutsceneSceneBuilder] sprite nao encontrado: {Scene03Sprite}");

        var sceneSpecs = new[]
        {
            BuildSceneSpec("cutscene.prologue.scene_01", sprite01, Color.white, 1.5f),
            BuildSceneSpec("cutscene.prologue.scene_02", sprite02, Color.white, 1.5f),
            BuildSceneSpec("cutscene.prologue.scene_03", sprite03, Color.white, 1.5f),
        };
        SetField(prologue, "scenes", sceneSpecs);

        var bridge = managerGo.AddComponent<PrologueCutsceneBridge>();
        SetField(bridge, "prologue", prologue);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[IntroCutsceneSceneBuilder] IntroCutscene.unity reconstruida.");
    }

    private static PrologueCutsceneController.CutsceneScene BuildSceneSpec(string key, Sprite sprite, Color color, float fade)
    {
        return new PrologueCutsceneController.CutsceneScene
        {
            sectionKey = key,
            backgroundSprite = sprite,
            backgroundColor = color,
            fadeDuration = fade,
        };
    }

    private static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (field == null)
        {
            Debug.LogError($"[IntroCutsceneSceneBuilder] field nao encontrado: {target.GetType().Name}.{fieldName}");
            return;
        }
        field.SetValue(target, value);
    }
}
