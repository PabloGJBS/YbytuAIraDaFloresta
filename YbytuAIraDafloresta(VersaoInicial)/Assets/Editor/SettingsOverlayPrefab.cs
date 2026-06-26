using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SettingsOverlayPrefab
{
    public const string PrefabPath = "Assets/Prefabs/UI/SettingsOverlay.prefab";
    private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    private const string HudPath = "Assets/Scenes/HUD.unity";

    [MenuItem("Tools/Ybytu/Setup Settings Overlay (Prefab)")]
    public static void SetupAll()
    {
        BuildPrefab();
        WireMainMenu();
        WireHud();
        Debug.Log("[SettingsOverlayPrefab] Prefab criado e aplicado em MainMenu + HUD.");
    }

    public static GameObject BuildPrefab()
    {
        var dir = Path.GetDirectoryName(PrefabPath);
        if (!AssetDatabase.IsValidFolder(dir))
        {
            Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }

        var go = new GameObject("SettingsOverlay", typeof(RectTransform), typeof(SettingsOverlayUI));
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        SettingsPanelBuilder.Populate(go.GetComponent<SettingsOverlayUI>());

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    public static SettingsOverlayUI PlaceInstance(Transform canvasT)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { Debug.LogError($"[SettingsOverlayPrefab] Prefab nao encontrado: {PrefabPath}"); return null; }

        var existing = canvasT.Find("SettingsOverlay");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        inst.name = "SettingsOverlay";
        var rt = inst.GetComponent<RectTransform>();
        rt.SetParent(canvasT, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        inst.transform.SetAsLastSibling();
        return inst.GetComponent<SettingsOverlayUI>();
    }

    private static void WireMainMenu()
    {
        var scene = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);

        var menu = Object.FindFirstObjectByType<MainMenuUI>(FindObjectsInactive.Include);
        if (menu == null) { Debug.LogError("[SettingsOverlayPrefab] MainMenuUI nao encontrado."); return; }
        var canvas = menu.GetComponentInParent<Canvas>() ?? Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null) { Debug.LogError("[SettingsOverlayPrefab] Canvas da MainMenu nao encontrado."); return; }

        var overlay = PlaceInstance(canvas.transform);
        SetPrivateField(menu, "settingsOverlay", overlay);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void WireHud()
    {
        var hud = SceneManager.GetSceneByName("HUD");
        bool openedHere = false;
        if (!hud.isLoaded) { hud = EditorSceneManager.OpenScene(HudPath, OpenSceneMode.Additive); openedHere = true; }

        PauseMenuController ctrl = null;
        Canvas canvas = null;
        foreach (var root in hud.GetRootGameObjects())
        {
            ctrl = root.GetComponentInChildren<PauseMenuController>(true);
            if (ctrl != null) { canvas = ctrl.GetComponentInParent<Canvas>(); break; }
        }
        if (ctrl == null || canvas == null)
        {
            Debug.LogError("[SettingsOverlayPrefab] PauseMenuController/Canvas nao encontrado na HUD. Rode antes 'Setup Pause Button (HUD)'.");
            if (openedHere) EditorSceneManager.CloseScene(hud, true);
            return;
        }

        var overlay = PlaceInstance(canvas.transform);
        SetPrivateField(ctrl, "settingsOverlay", overlay);

        EditorSceneManager.MarkSceneDirty(hud);
        EditorSceneManager.SaveScene(hud);
        if (openedHere) EditorSceneManager.CloseScene(hud, true);
    }

    private static void SetPrivateField(object target, string field, object value)
    {
        var f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (f == null) { Debug.LogError($"[SettingsOverlayPrefab] campo nao encontrado: {field}"); return; }
        f.SetValue(target, value);
    }
}
