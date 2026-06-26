using UnityEngine;
using UnityEditor;

public static class HUDFixSiblingOrder
{
    [MenuItem("Tools/HUD/Fix HealthPanel Sibling Order")]
    public static void Fix()
    {
        var panel = GameObject.Find("TopLeft_HealthPanel");
        if (panel == null) { Debug.LogError("TopLeft_HealthPanel not found"); return; }
        var fill = panel.transform.Find("HealthBar_Fill");
        var frame = panel.transform.Find("HealthBar_Frame");
        var portrait = panel.transform.Find("Portrait");
        if (frame != null) frame.SetSiblingIndex(0);
        if (fill != null) fill.SetSiblingIndex(1);
        if (portrait != null) portrait.SetSiblingIndex(2);
        EditorUtility.SetDirty(panel);
        Debug.Log("[HUD] Sibling order fixed: Frame < Fill < Portrait");

        var lives = GameObject.Find("LivesContainer");
        if (lives != null)
        {
            var icon = lives.transform.Find("LifeIcon");
            if (icon != null) icon.SetSiblingIndex(0);
            EditorUtility.SetDirty(lives);
            Debug.Log("[HUD] LivesContainer: LifeIcon moved to index 0");
        }

        var combo = GameObject.Find("ComboBar");
        if (combo != null)
        {
            var cFrame = combo.transform.Find("ComboBar_Frame");
            var cFill = combo.transform.Find("ComboBar_Fill");
            if (cFrame != null) cFrame.SetSiblingIndex(0);
            if (cFill != null) cFill.SetSiblingIndex(1);
            EditorUtility.SetDirty(combo);
            Debug.Log("[HUD] Combo sibling order fixed: Frame < Fill");
        }
    }
}
