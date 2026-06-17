using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SettingsPanelBuilder
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string PixelFontPath = "Assets/Fonts/Pixel/PixelOperator-Bold SDF.asset";
    private const string MusicIconPath = "Assets/Sprites/UI/HUD/MusicNoteIcon.png";
    private const string SfxIconPath = "Assets/Sprites/UI/HUD/SpeakerIcon.png";
    private const string FramePath = "Assets/Sprites/UI/SaveSelect/SlotFrameAnim/SlotFrame_01.png";

    [MenuItem("Tools/Ybytu/Build Settings Panel (MainMenu)")]
    public static void Build()
    {
        SettingsOverlayPrefab.SetupAll();
    }

    public static void Populate(SettingsOverlayUI ui)
    {
        var rootField = typeof(SettingsOverlayUI).GetField("root", BindingFlags.Instance | BindingFlags.NonPublic);
        var root = rootField != null ? rootField.GetValue(ui) as GameObject : null;
        if (root == null)
        {
            root = new GameObject("Root", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(ui.transform, false);
            rootField?.SetValue(ui, root);
        }

        var rootImg = root.GetComponent<Image>() ?? root.AddComponent<Image>();
        rootImg.color = new Color(0f, 0f, 0f, 0.88f);
        rootImg.raycastTarget = true;
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // Limpa conteudo antigo.
        for (int i = root.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(850, 790);
        var panelImg = panel.GetComponent<Image>();
        var frame = AssetDatabase.LoadAssetAtPath<Sprite>(FramePath);
        if (frame != null)
        {
            panelImg.sprite = frame;
            panelImg.type = Image.Type.Sliced;
            panelImg.color = Color.white;
            panelImg.pixelsPerUnitMultiplier = 1f;
        }
        else
        {
            panelImg.color = new Color(0.10f, 0.12f, 0.10f, 0.97f);
        }

        var pixelFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PixelFontPath);

        var title = MakeText(panel.transform, "Title", "CONFIGURAÇÕES", 46, FontStyles.Bold,
                 TextAlignmentOptions.Center, new Vector2(0, 175), new Vector2(560, 60), pixelFont);
        title.color = new Color(1f, 0.86f, 0.55f);

        MakeIcon(panel.transform, "MusicIcon", MusicIconPath, new Vector2(-205, 80), new Vector2(50, 50));
        var musicSlider = MakeSlider(panel.transform, "MusicSlider", new Vector2(35, 80), new Vector2(360, 28));

        MakeIcon(panel.transform, "SfxIcon", SfxIconPath, new Vector2(-205, 10), new Vector2(56, 50));
        var sfxSlider = MakeSlider(panel.transform, "SfxSlider", new Vector2(35, 10), new Vector2(360, 28));

        var langBtn = MakeButton(panel.transform, "LanguageButton", "Idioma: Português",
                                 new Vector2(0, -85), new Vector2(400, 70), pixelFont);
        var langLabel = langBtn.transform.Find("Text").GetComponent<TextMeshProUGUI>();

        var closeBtn = MakeButton(panel.transform, "CloseButton", "Voltar", new Vector2(0, -180), new Vector2(260, 66), pixelFont);

        SetField(ui, "musicSlider", musicSlider);
        SetField(ui, "sfxSlider", sfxSlider);
        SetField(ui, "closeButton", closeBtn);
        SetField(ui, "languageButton", langBtn);
        SetField(ui, "languageLabel", langLabel);
    }

    private static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size,
        FontStyles style, TextAlignmentOptions align, Vector2 pos, Vector2 sd, TMP_FontAsset font = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sd;
        var t = go.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    private static Image MakeIcon(Transform parent, string name, string spritePath, Vector2 pos, Vector2 sd)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sd;
        var img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    private static Slider MakeSlider(Transform parent, string name, Vector2 pos, Vector2 sd)
    {
        var go = DefaultControls.CreateSlider(new DefaultControls.Resources());
        go.name = name;
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sd;

        var slider = go.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        Tint(go.transform, "Background", new Color(0.16f, 0.12f, 0.08f, 1f));
        Tint(go.transform, "Fill Area/Fill", new Color(1f, 0.68f, 0.16f, 1f));   // ambar dourado
        Tint(go.transform, "Handle Slide Area/Handle", new Color(1f, 0.92f, 0.72f, 1f)); // creme
        return slider;
    }

    private static void Tint(Transform root, string path, Color c)
    {
        var tr = root.Find(path);
        if (tr == null) return;
        var img = tr.GetComponent<Image>();
        if (img != null) img.color = c;
    }

    private static Button MakeButton(Transform parent, string name, string label, Vector2 pos, Vector2 sd, TMP_FontAsset font = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = sd;
        go.GetComponent<Image>().color = new Color(0.32f, 0.21f, 0.12f, 1f); // madeira escura

        var txt = MakeText(go.transform, "Text", label, 34, FontStyles.Bold, TextAlignmentOptions.Center,
                           Vector2.zero, sd, font);
        txt.color = new Color(1f, 0.92f, 0.78f); // creme
        var txtRect = txt.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero; txtRect.offsetMax = Vector2.zero;
        return go.GetComponent<Button>();
    }

    private static void SetField(object target, string field, object value)
    {
        var f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (f == null) { Debug.LogError($"[SettingsPanelBuilder] campo nao encontrado: {field}"); return; }
        f.SetValue(target, value);
    }
}
