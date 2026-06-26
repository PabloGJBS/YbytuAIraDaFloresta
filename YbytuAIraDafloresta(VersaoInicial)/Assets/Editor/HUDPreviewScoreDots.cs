using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class HUDPreviewScoreDots
{
    [MenuItem("Tools/HUD/Preview Score 99.999.999")]
    public static void Preview()
    {
        var sc = GameObject.Find("ScoreContainer");
        if (sc == null) { Debug.LogError("ScoreContainer not found"); return; }

        var dotPath = "Assets/Sprites/UI/ComboLetters/LetraPonto.png";
        var dotSprite = AssetDatabase.LoadAssetAtPath<Sprite>(dotPath);
        if (dotSprite == null) { Debug.LogError("LetraPonto sprite not loaded"); return; }

        for (int j = 0; j < 2; j++)
        {
            int targetIndex = (j == 0) ? 2 : 6;
            var go = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(sc.transform, false);
            var img = go.GetComponent<Image>();
            img.sprite = dotSprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.localScale = Vector3.one;
            float h = 18f;
            float aspect = dotSprite.rect.width / dotSprite.rect.height;
            rt.sizeDelta = new Vector2(h * aspect, h);
            go.transform.SetSiblingIndex(targetIndex);
        }
        EditorUtility.SetDirty(sc);
        Debug.Log("[HUD] Score dots inserted at indices 2 and 6");
    }
}
