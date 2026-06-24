using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StageScoreSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/StageScore.unity";
    private const string MusicPath = "Assets/Audio/Musicas/FinalizacaoDeFase.mp3";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string BgPath = "Assets/Sprites/UI/StageScore/BackGroundFinalFase1.png";
    private const string FramePath = "Assets/Sprites/UI/SaveSelect/SlotFrameAnim/SlotFrame_06.png";

    [MenuItem("Tools/Ybytu/Build StageScore Scene")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.03f, 0.04f, 0.05f);
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

        // Background
        var bgGo = new GameObject("BG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgImg = bgGo.GetComponent<Image>();
        var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BgPath);
        if (bgSprite != null) { bgImg.sprite = bgSprite; bgImg.color = Color.white; }
        else bgImg.color = new Color(0.02f, 0.05f, 0.03f, 1f);
        bgImg.raycastTarget = false;
        Stretch(bgGo.GetComponent<RectTransform>());

        var scrim = new GameObject("Scrim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        scrim.transform.SetParent(canvasGo.transform, false);
        var scrimImg = scrim.GetComponent<Image>();
        scrimImg.color = new Color(0f, 0f, 0f, 0.42f);
        scrimImg.raycastTarget = false;
        Stretch(scrim.GetComponent<RectTransform>());

        // Titulo
        var title = MakeText("Title", canvasGo.transform, font, "FASE CONCLUÍDA", 74,
            FontStyles.Bold, new Color(0.55f, 0.9f, 0.45f));
        Place(title.rectTransform, new Vector2(0, 430), new Vector2(1500, 110));

        var frameGo = new GameObject("ScoreFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        frameGo.transform.SetParent(canvasGo.transform, false);
        var frameImg = frameGo.GetComponent<Image>();
        frameImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FramePath);
        frameImg.preserveAspect = false;
        frameImg.raycastTarget = false;
        Place(frameGo.GetComponent<RectTransform>(), new Vector2(0, -10), new Vector2(540, 800));

        var frameT = frameGo.transform;
        var stageName = MakeText("StageName", frameT, font, "", 26, FontStyles.Italic, new Color(0.88f, 0.92f, 0.85f));
        Place(stageName.rectTransform, new Vector2(0, 288), new Vector2(330, 60));
        var scoreCaption = MakeText("ScoreCaption", frameT, font, "PONTUAÇÃO", 28, FontStyles.Bold, new Color(0.8f, 0.82f, 0.75f));
        Place(scoreCaption.rectTransform, new Vector2(0, 158), new Vector2(330, 50));
        var score = MakeText("Score", frameT, font, "0", 74, FontStyles.Bold, new Color(1f, 0.92f, 0.6f));
        Place(score.rectTransform, new Vector2(0, 62), new Vector2(360, 130));
        var timeCaption = MakeText("TimeCaption", frameT, font, "TEMPO", 24, FontStyles.Bold, new Color(0.75f, 0.78f, 0.72f));
        Place(timeCaption.rectTransform, new Vector2(0, -78), new Vector2(330, 44));
        var time = MakeText("Time", frameT, font, "00:00", 42, FontStyles.Bold, new Color(0.93f, 0.95f, 0.9f));
        Place(time.rectTransform, new Vector2(0, -124), new Vector2(330, 70));
        var rankCaption = MakeText("RankCaption", frameT, font, "RANK", 24, FontStyles.Bold, new Color(0.75f, 0.78f, 0.72f));
        Place(rankCaption.rectTransform, new Vector2(0, -210), new Vector2(330, 44));
        var rank = MakeText("Rank", frameT, font, "-", 54, FontStyles.Bold, new Color(1f, 0.85f, 0.4f));
        Place(rank.rectTransform, new Vector2(0, -262), new Vector2(330, 90));

        // Prompt
        var promptGo = MakeText("ContinuePrompt", canvasGo.transform, font, "Espaço para continuar  ▼", 28,
            FontStyles.Bold, new Color(1f, 1f, 1f, 0.8f));
        Place(promptGo.rectTransform, new Vector2(0, -470), new Vector2(900, 50));
        promptGo.gameObject.SetActive(false);

        var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        SceneManager.MoveGameObjectToScene(eventSystemGo, scene);

        var musicGo = new GameObject("Music", typeof(AudioSource));
        SceneManager.MoveGameObjectToScene(musicGo, scene);
        var src = musicGo.GetComponent<AudioSource>();
        src.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
        src.playOnAwake = false;
        src.loop = false;

        var managerGo = new GameObject("StageScore_Manager");
        SceneManager.MoveGameObjectToScene(managerGo, scene);
        var ui = managerGo.AddComponent<StageScoreUI>();
        SetField(ui, "stageNameText", stageName);
        SetField(ui, "scoreText", score);
        SetField(ui, "timeText", time);
        SetField(ui, "rankText", rank);
        SetField(ui, "continuePrompt", promptGo.gameObject);
        SetField(ui, "music", src);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("[StageScoreSceneBuilder] StageScore.unity construida (com moldura + chamas).");
    }

    private static TMP_Text MakeText(string name, Transform parent, TMP_FontAsset font, string text,
        float size, FontStyles style, Color color)
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
        return tmp;
    }

    private static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        var f = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (f == null) { Debug.LogError($"[StageScoreSceneBuilder] field nao encontrado: {fieldName}"); return; }
        f.SetValue(target, value);
    }
}
