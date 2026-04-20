using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class HUDApplyOutline
{
    [MenuItem("Tools/HUD/Apply Outline to Number/Letter Sprites")]
    public static void Apply()
    {
        string[] containers = { "LivesContainer", "ScoreContainer", "HitCountContainer", "ComboRankImage" };
        Color color = new Color(1f, 0.85f, 0.35f, 0.6f);
        Vector2 dist = new Vector2(2f, -2f);
        int applied = 0;

        foreach (var containerName in containers)
        {
            var root = GameObject.Find(containerName);
            if (root == null) continue;

            var images = root.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.GetComponent<Outline>() != null) continue;
                var o = img.gameObject.AddComponent<Outline>();
                o.effectColor = color;
                o.effectDistance = dist;
                EditorUtility.SetDirty(img.gameObject);
                applied++;
            }
        }
        Debug.Log($"[HUD] Outline applied to {applied} sprite children.");
    }
}
