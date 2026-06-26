using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class HudPauseSetup
{
    private const string HudPath = "Assets/Scenes/HUD.unity";
    private const string PauseSpritePath = "Assets/Sprites/UI/HUD/PauseButton.png";

    private const float ButtonSize = 68f;
    private const float CornerMargin = 20f;     // margem do canto
    private const float LivesPanelX = -105f;
    private const float ButtonY = -31f;

    [MenuItem("Tools/Ybytu/Setup Pause Button (HUD)")]
    public static void Setup()
    {
        var hud = SceneManager.GetSceneByName("HUD");
        bool openedHere = false;
        if (!hud.isLoaded)
        {
            hud = EditorSceneManager.OpenScene(HudPath, OpenSceneMode.Additive);
            openedHere = true;
        }

        var gameHud = FindInScene<GameHUD>(hud);
        if (gameHud == null) { Debug.LogError("[HudPauseSetup] GameHUD nao encontrado na cena HUD."); return; }

        var canvas = gameHud.GetComponentInParent<Canvas>();
        if (canvas == null) { Debug.LogError("[HudPauseSetup] Canvas da HUD nao encontrado."); return; }
        Transform canvasT = canvas.transform;

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PauseSpritePath);
        if (sprite == null) Debug.LogWarning($"[HudPauseSetup] Sprite nao encontrado: {PauseSpritePath} (o botao ficara sem imagem).");

        var livesPanel = canvasT.Find("TopRight_LivesScorePanel") as RectTransform;
        if (livesPanel != null)
            livesPanel.anchoredPosition = new Vector2(LivesPanelX, livesPanel.anchoredPosition.y);
        else
            Debug.LogWarning("[HudPauseSetup] TopRight_LivesScorePanel nao encontrado; vidas/score nao foram reposicionados.");

        var btnGO = FindOrCreate(canvasT, "PauseButton",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var brt = (RectTransform)btnGO.transform;
        brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(1f, 1f);
        brt.sizeDelta = new Vector2(ButtonSize, ButtonSize);
        brt.anchoredPosition = new Vector2(-CornerMargin, ButtonY);
        var img = btnGO.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = true;
        var btn = btnGO.GetComponent<Button>();

        if (AssetDatabase.LoadAssetAtPath<GameObject>(SettingsOverlayPrefab.PrefabPath) == null)
            SettingsOverlayPrefab.BuildPrefab();
        var overlay = SettingsOverlayPrefab.PlaceInstance(canvasT);

        var ctrlGO = FindOrCreate(canvasT, "PauseController",
            typeof(RectTransform), typeof(PauseMenuController));
        var ctrl = ctrlGO.GetComponent<PauseMenuController>();
        var so = new SerializedObject(ctrl);
        so.FindProperty("pauseButton").objectReferenceValue = btn;
        so.FindProperty("settingsOverlay").objectReferenceValue = overlay;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(hud);
        EditorSceneManager.SaveScene(hud);
        Debug.Log("[HudPauseSetup] Botao de pausa + tela de configuracoes montados na HUD.");

        if (openedHere)
            EditorSceneManager.CloseScene(hud, true);
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var c = root.GetComponentInChildren<T>(true);
            if (c != null) return c;
        }
        return null;
    }

    private static GameObject FindOrCreate(Transform parent, string name, params System.Type[] comps)
    {
        var t = parent.Find(name);
        if (t != null) return t.gameObject;
        var go = new GameObject(name, comps);
        go.transform.SetParent(parent, false);
        return go;
    }
}
