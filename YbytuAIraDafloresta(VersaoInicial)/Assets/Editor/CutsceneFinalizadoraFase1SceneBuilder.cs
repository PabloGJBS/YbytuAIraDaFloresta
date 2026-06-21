using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CutsceneFinalizadoraFase1SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/CutsceneFinalizadoraFase1.unity";
    private const string PtBrJsonPath = "Assets/Localization/pt-BR.json";
    private const string EnUsJsonPath = "Assets/Localization/en-US.json";
    private const string Dir = "Assets/Sprites/UI/OutroCutscene";
    private const string MusicPath = "Assets/Audio/Musicas/MusicaCalmaria.mp3";

    [MenuItem("Tools/Ybytu/Build CutsceneFinalizadoraFase1 Scene")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        camGo.tag = "MainCamera";
        camGo.AddComponent<AudioListener>();
        SceneManager.MoveGameObjectToScene(camGo, scene);

        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        SceneManager.MoveGameObjectToScene(canvasGo, scene);

        const float topBarHeight = 0f;
        const float bottomBarHeight = 150f;  // barra de texto enxuta

        var bgGo = new GameObject("NewsImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(0f, bottomBarHeight);
        bgRect.offsetMax = new Vector2(0f, -topBarHeight);
        var bgImage = bgGo.GetComponent<Image>();
        bgImage.color = Color.black;
        bgImage.preserveAspect = true;
        bgImage.raycastTarget = false;

        var topBarGo = new GameObject("TopBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        topBarGo.transform.SetParent(canvasGo.transform, false);
        var topRect = topBarGo.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0f, 1f);
        topRect.anchorMax = new Vector2(1f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = Vector2.zero;
        topRect.sizeDelta = new Vector2(0f, topBarHeight);
        topBarGo.GetComponent<Image>().color = Color.black;
        topBarGo.GetComponent<Image>().raycastTarget = false;

        var bottomBarGo = new GameObject("BottomBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bottomBarGo.transform.SetParent(canvasGo.transform, false);
        var bottomRect = bottomBarGo.GetComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0f, 0f);
        bottomRect.anchorMax = new Vector2(1f, 0f);
        bottomRect.pivot = new Vector2(0.5f, 0f);
        bottomRect.anchoredPosition = Vector2.zero;
        bottomRect.sizeDelta = new Vector2(0f, bottomBarHeight);
        bottomBarGo.GetComponent<Image>().color = Color.black;
        bottomBarGo.GetComponent<Image>().raycastTarget = false;

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

        var managerGo = new GameObject("CutsceneFinalizadoraFase1_Manager");
        SceneManager.MoveGameObjectToScene(managerGo, scene);

        var locManager = managerGo.AddComponent<LocalizationManager>();
        var ptAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(PtBrJsonPath);
        var enAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(EnUsJsonPath);
        var langField = typeof(LocalizationManager).GetField("availableLanguages",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (langField != null)
            langField.SetValue(locManager, new[] { ptAsset, enAsset });

        var textPlayer = managerGo.AddComponent<CutsceneTextPlayer>();
        SetField(textPlayer, "displayText", dialogText);
        SetField(textPlayer, "continuePrompt", promptGo);
        SetField(textPlayer, "playOnStart", false);
        SetField(textPlayer, "waitForInput", true);
        SetField(textPlayer, "charDelay", 0.03f);

        var controller = managerGo.AddComponent<PrologueCutsceneController>();
        SetField(controller, "backgroundImage", bgImage);
        SetField(controller, "textPlayer", textPlayer);
        SetField(controller, "playOnStart", true);
        SetField(controller, "delayBetweenScenes", 0.6f);

        var scenes = new[]
        {
            SceneSpec("cutscene.outro.opening", null, 1.2f),
            SceneSpec("cutscene.outro.news_01", LoadSprite("Noticia_02"), 1.0f),
            SceneSpec("cutscene.outro.news_02", LoadSprite("Noticia_03"), 1.0f),
            SceneSpec("cutscene.outro.news_03", LoadSprite("Noticia_04"), 1.0f),
            SceneSpec("cutscene.outro.news_04", LoadSprite("Noticia_05"), 1.0f),
            SceneSpec("cutscene.outro.news_05", LoadSprite("Noticia_01"), 1.0f),
            SceneSpec("cutscene.outro.reflection", null, 1.5f),
        };
        SetField(controller, "scenes", scenes);

        var bridge = managerGo.AddComponent<OutroCutsceneBridge>();
        SetField(bridge, "cutscene", controller);

        var bgmGo = new GameObject("SceneBgm");
        SceneManager.MoveGameObjectToScene(bgmGo, scene);
        var bgm = bgmGo.AddComponent<SceneBgm>();
        SetField(bgm, "clip", AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath));
        SetField(bgm, "loop", true);
        SetField(bgm, "autoPlay", true);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("[CutsceneFinalizadoraFase1SceneBuilder] CutsceneFinalizadoraFase1.unity construida.");
    }

    private static Sprite LoadSprite(string name)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{Dir}/{name}.png");
        if (s == null) Debug.LogWarning($"[CutsceneFinalizadoraFase1SceneBuilder] sprite nao encontrado: {Dir}/{name}.png");
        return s;
    }

    private static PrologueCutsceneController.CutsceneScene SceneSpec(string key, Sprite sprite, float fade)
    {
        return new PrologueCutsceneController.CutsceneScene
        {
            sectionKey = key,
            backgroundSprite = sprite,
            backgroundColor = Color.white,
            fadeDuration = fade,
        };
    }

    private static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (field == null)
        {
            Debug.LogError($"[CutsceneFinalizadoraFase1SceneBuilder] field nao encontrado: {target.GetType().Name}.{fieldName}");
            return;
        }
        field.SetValue(target, value);
    }
}
