using UnityEditor;
using UnityEngine;

public static class SetupFase2Sprites
{
    private const string Root = "Assets/Sprites/Stage2";
    private const float PPU = 256f;

    [MenuItem("Tools/Setup/Setup Fase2 Sprites")]
    public static void Run()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Root });
        int changed = 0;
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png")) continue;
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;

            bool dirty = false;
            if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; dirty = true; }
            if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            if (imp.spritePixelsPerUnit != PPU) { imp.spritePixelsPerUnit = PPU; dirty = true; }
            if (imp.filterMode != FilterMode.Point) { imp.filterMode = FilterMode.Point; dirty = true; }
            if (imp.textureCompression != TextureImporterCompression.Uncompressed) { imp.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
            if (imp.mipmapEnabled) { imp.mipmapEnabled = false; dirty = true; }
            if (imp.wrapMode != TextureWrapMode.Clamp) { imp.wrapMode = TextureWrapMode.Clamp; dirty = true; }

            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            if (settings.spriteMeshType != SpriteMeshType.FullRect) { settings.spriteMeshType = SpriteMeshType.FullRect; imp.SetTextureSettings(settings); dirty = true; }
            if (settings.spriteAlignment != (int)SpriteAlignment.Center) { settings.spriteAlignment = (int)SpriteAlignment.Center; imp.SetTextureSettings(settings); dirty = true; }

            if (dirty) { imp.SaveAndReimport(); changed++; }
        }
        Debug.Log($"[SetupFase2Sprites] Importers ajustados em {changed} de {guids.Length} texturas (PPU {PPU}, Point, FullRect, Uncompressed).");
    }
}
