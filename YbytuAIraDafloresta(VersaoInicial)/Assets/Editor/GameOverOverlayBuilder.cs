using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameOverOverlayBuilder
{
    private const string StagePath = "Assets/Scenes/Stage1.unity";
    private const string Dir = "Assets/Sprites/UI/GameOver";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("Tools/Ybytu/Build GameOver Overlay (Stage1)")]
    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(StagePath, OpenSceneMode.Single);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        // Idempotente: remove o anterior
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == "GameOverManager") { Object.DestroyImmediate(root); break; }

        var manager = new GameObject("GameOverManager");
        SceneManager.MoveGameObjectToScene(manager, scene);
        var controller = manager.AddComponent<GameOverController>();

        var canvasGo = new GameObject("GameOverCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(manager.transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Fundo escurecido (modal)
        var dark = NewImage("Darken", canvasGo.transform, null);
        dark.color = new Color(0f, 0f, 0f, 0.78f);
        dark.raycastTarget = true;
        Stretch(dark.rectTransform);

        // Titulo GAME OVER
        var title = NewImage("Title", canvasGo.transform, LoadSprite("GameOverTitle"));
        title.preserveAspect = true;
        title.raycastTarget = false;
        Place(title.rectTransform, new Vector2(0, 230), new Vector2(1000, 340));

        // Contador
        var countdown = NewText("Countdown", canvasGo.transform, font, "10", 90,
            new Color(1f, 0.85f, 0.4f));
        Place(countdown.rectTransform, new Vector2(0, 20), new Vector2(300, 130));

        // Botoes
        var continueBtn = NewButton("ContinueButton", canvasGo.transform, LoadSprite("ButtonContinuar"));
        Place(((Image)continueBtn.targetGraphic).rectTransform, new Vector2(0, -180), new Vector2(470, 130));

        var desistirBtn = NewButton("DesistirButton", canvasGo.transform, LoadSprite("ButtonDesistir"));
        Place(((Image)desistirBtn.targetGraphic).rectTransform, new Vector2(0, -340), new Vector2(470, 140));

        // Wire controller
        SetField(controller, "root", canvasGo);
        SetField(controller, "continueButton", continueBtn);
        SetField(controller, "desistirButton", desistirBtn);
        SetField(controller, "countdownText", countdown);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[GameOverOverlayBuilder] Overlay de Game Over adicionado a Stage1.");
    }

    private static Sprite LoadSprite(string name)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{Dir}/{name}.png");
        if (s == null) Debug.LogWarning($"[GameOverOverlayBuilder] sprite nao encontrado: {Dir}/{name}.png");
        return s;
    }

    private static Image NewImage(string name, Transform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        if (sprite != null) img.color = Color.white;
        return img;
    }

    private static Button NewButton(string name, Transform parent, Sprite sprite)
    {
        var img = NewImage(name, parent, sprite);
        img.preserveAspect = true;
        img.raycastTarget = true;
        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        return btn;
    }

    private static TMP_Text NewText(string name, Transform parent, TMP_FontAsset font, string text, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
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
        if (f == null) { Debug.LogError($"[GameOverOverlayBuilder] field nao encontrado: {fieldName}"); return; }
        f.SetValue(target, value);
    }
}
