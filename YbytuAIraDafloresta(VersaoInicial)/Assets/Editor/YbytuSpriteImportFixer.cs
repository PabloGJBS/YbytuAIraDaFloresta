using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class YbytuSpriteImportFixer
{
    [MenuItem("Tools/Ybytu/Apply Sprite Import Settings")]
    public static void Apply()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites/Ybytu" });
        int total = 0, changed = 0;
        var changedPaths = new List<string>();
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;
            total++;
            bool dirty = false;
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            if (s.textureType != TextureImporterType.Sprite) { s.textureType = TextureImporterType.Sprite; dirty = true; }
            if (s.spriteMode != (int)SpriteImportMode.Single) { s.spriteMode = (int)SpriteImportMode.Single; dirty = true; }
            if (s.spritePixelsPerUnit != 32f) { s.spritePixelsPerUnit = 32f; dirty = true; }
            if (s.spriteMeshType != SpriteMeshType.FullRect) { s.spriteMeshType = SpriteMeshType.FullRect; dirty = true; }
            if (s.filterMode != FilterMode.Point) { s.filterMode = FilterMode.Point; dirty = true; }
            if (s.mipmapEnabled) { s.mipmapEnabled = false; dirty = true; }
            if (!s.alphaIsTransparency) { s.alphaIsTransparency = true; dirty = true; }
            if (dirty) imp.SetTextureSettings(s);
            if (imp.textureCompression != TextureImporterCompression.Uncompressed) { imp.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
            if (dirty)
            {
                imp.SaveAndReimport();
                changed++;
                changedPaths.Add(path);
            }
        }
        Debug.Log($"[YbytuSpriteImportFixer] total={total} changed={changed}");
        foreach (var p in changedPaths) Debug.Log($"[YbytuSpriteImportFixer] {p}");
    }
}
