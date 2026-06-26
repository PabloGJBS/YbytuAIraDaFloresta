using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class TutorialScreenBuilder
{
    [MenuItem("Ybytu/UI/Build Tutorial Screen")]
    public static void Build()
    {
        var overlay = Object.FindObjectOfType<TutorialOverlayUI>(true);
        if (overlay == null)
        {
            Debug.LogError("[TutorialScreenBuilder] TutorialOverlayUI nao encontrado. Abra a cena MainMenu.");
            return;
        }

        var overlayRt = EnsureRect(overlay.gameObject);

        var rootChild = overlay.transform.Find("Root");
        RectTransform rootTr;
        if (rootChild != null)
        {
            rootTr = EnsureRect(rootChild.gameObject);
        }
        else
        {
            rootTr = NewChild("Root", overlay.transform);
            var soRoot = new SerializedObject(overlay);
            soRoot.FindProperty("root").objectReferenceValue = rootTr.gameObject;
            soRoot.ApplyModifiedPropertiesWithoutUndo();
        }

        Stretch(overlayRt);
        Stretch(rootTr);
        overlay.transform.SetAsLastSibling();

        for (int i = rootTr.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(rootTr.GetChild(i).gameObject);

        // Fundo preto cobrindo tudo
        var bg = rootTr.GetComponent<Image>();
        if (bg == null) bg = rootTr.gameObject.AddComponent<Image>();
        bg.color = Color.black;
        bg.raycastTarget = true;

        var content = NewChild("Content", rootTr);
        Stretch(content);
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 28f;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.padding = new RectOffset(40, 40, 40, 40);

        MakeLine(content, "Title", "ui.tutorial.title", "Controles", 64, FontStyles.Bold);
        MakeLine(content, "Move", "ui.tutorial.controls_move", "WASD para movimentar", 40, FontStyles.Normal);
        MakeLine(content, "Attack", "ui.tutorial.controls_attack", "JKL para golpes", 40, FontStyles.Normal);
        MakeLine(content, "Interact", "ui.tutorial.controls_interact", "Espaço para interagir com objetos", 40, FontStyles.Normal);

        var backBtn = MakeButton(content, "BackButton", "ui.tutorial.back", "Voltar");

        var so = new SerializedObject(overlay);
        so.FindProperty("closeButton").objectReferenceValue = backBtn;
        var pagesProp = so.FindProperty("pages");
        pagesProp.ClearArray();
        pagesProp.InsertArrayElementAtIndex(0);
        pagesProp.GetArrayElementAtIndex(0).objectReferenceValue = content.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();

        rootTr.gameObject.SetActive(false);

        EditorUtility.SetDirty(overlay);
        EditorSceneManager.MarkSceneDirty(overlay.gameObject.scene);
        Debug.Log("[TutorialScreenBuilder] Tela de tutorial construida com sucesso.");
    }

    private static RectTransform EnsureRect(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        return rt;
    }

    private static RectTransform NewChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.anchoredPosition = Vector2.zero;
    }

    private static void MakeLine(Transform parent, string name, string key, string fallback, float size, FontStyles style)
    {
        var rt = NewChild(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = fallback;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = style;
        tmp.enableWordWrapping = false;

        var loc = rt.gameObject.AddComponent<LocalizedText>();
        var so = new SerializedObject(loc);
        so.FindProperty("localizationKey").stringValue = key;
        so.ApplyModifiedPropertiesWithoutUndo();

        var le = rt.gameObject.AddComponent<LayoutElement>();
        le.minHeight = size + 18f;
    }

    private static Button MakeButton(Transform parent, string name, string key, string fallback)
    {
        var rt = NewChild(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(0.16f, 0.40f, 0.18f, 1f);
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;

        var le = rt.gameObject.AddComponent<LayoutElement>();
        le.minWidth = 280f;
        le.minHeight = 76f;
        le.preferredWidth = 280f;
        le.preferredHeight = 76f;

        var labelRt = NewChild("Label", rt);
        Stretch(labelRt);
        var tmp = labelRt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = fallback;
        tmp.fontSize = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        var loc = labelRt.gameObject.AddComponent<LocalizedText>();
        var so = new SerializedObject(loc);
        so.FindProperty("localizationKey").stringValue = key;
        so.ApplyModifiedPropertiesWithoutUndo();

        return btn;
    }
}
