using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

/// <summary>
/// Constroi TMP_FontAsset bitmap a partir de um atlas PNG + descritor JSON
/// gerado pelos scripts Python (Ferramentas/PixelFont). Uso: menu Tools/Pixel Fonts.
/// </summary>
public static class PixelFontBuilder
{
    [System.Serializable] class GlyphRec { public int src, x, y, w, h, bearingX, bearingY, advance; }
    [System.Serializable] class CharRec { public int u, g; }
    [System.Serializable] class Descriptor
    {
        public string name;
        public int pointSize, atlasWidth, atlasHeight, padding, baseline, ascentLine, descentLine, capLine, meanLine, lineHeight;
        public GlyphRec[] glyphs;
        public CharRec[] chars;
    }

    const string Dir = "Assets/Fonts/Pixel/";

    [MenuItem("Tools/Pixel Fonts/Build Tiny01")]
    public static void BuildTiny01() => Build(Dir + "Tiny01_Atlas.png", Dir + "Tiny01_Atlas.json", Dir + "Tiny01.asset");

    [MenuItem("Tools/Pixel Fonts/Build Bold02")]
    public static void BuildBold02() => Build(Dir + "Bold02_Atlas.png", Dir + "Bold02_Atlas.json", Dir + "Bold02.asset");

    // Reaponta o material da fonte ja existente pro shader colorido (preserva o GUID do asset).
    [MenuItem("Tools/Pixel Fonts/Apply Color Shader (Bold02 + Tiny01)")]
    public static void ApplyColorShader()
    {
        var shader = Shader.Find("TMP/PixelColorBitmap");
        if (shader == null) { Debug.LogError("[PixelFont] Shader TMP/PixelColorBitmap nao encontrado."); return; }
        foreach (var path in new[] { Dir + "Bold02.asset", Dir + "Tiny01.asset" })
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null || font.material == null) continue;
            font.material.shader = shader;
            font.material.SetTexture("_MainTex", font.atlasTexture);
            EditorUtility.SetDirty(font.material);
            Debug.Log("[PixelFont] shader colorido aplicado: " + path);
        }
        AssetDatabase.SaveAssets();
    }

    static void Build(string pngPath, string jsonPath, string assetPath)
    {
        if (!File.Exists(jsonPath)) { Debug.LogError("[PixelFont] JSON nao encontrado: " + jsonPath); return; }

        // 1) Import settings do atlas
        var ti = (TextureImporter)AssetImporter.GetAtPath(pngPath);
        if (ti == null) { Debug.LogError("[PixelFont] PNG nao encontrado/importado: " + pngPath); return; }
        ti.textureType = TextureImporterType.Default;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.isReadable = true;
        ti.mipmapEnabled = false;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.alphaSource = TextureImporterAlphaSource.FromInput;
        ti.alphaIsTransparency = true;
        ti.sRGBTexture = true;
        ti.SaveAndReimport();

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
        var desc = JsonUtility.FromJson<Descriptor>(File.ReadAllText(jsonPath));

        // 2) Glyph + Character tables
        var glyphTable = new List<Glyph>();
        for (int i = 0; i < desc.glyphs.Length; i++)
        {
            var g = desc.glyphs[i];
            var metrics = new GlyphMetrics(g.w, g.h, g.bearingX, g.bearingY, g.advance);
            var rect = new GlyphRect(g.x, g.y, g.w, g.h);
            glyphTable.Add(new Glyph((uint)i, metrics, rect, 1f, 0));
        }
        var charTable = new List<TMP_Character>();
        foreach (var c in desc.chars)
        {
            if (c.g < 0 || c.g >= glyphTable.Count) continue;
            charTable.Add(new TMP_Character((uint)c.u, glyphTable[c.g]));
        }

        // 3) FaceInfo (via reflection - struct com setters internos)
        object face = new FaceInfo();
        SetFI(ref face, "m_FamilyName", desc.name);
        SetFI(ref face, "m_StyleName", "Regular");
        SetFI(ref face, "m_PointSize", desc.pointSize);
        SetFI(ref face, "m_Scale", 1f);
        SetFI(ref face, "m_LineHeight", (float)desc.lineHeight);
        SetFI(ref face, "m_AscentLine", (float)desc.ascentLine);
        SetFI(ref face, "m_CapLine", (float)desc.capLine);
        SetFI(ref face, "m_MeanLine", (float)desc.meanLine);
        SetFI(ref face, "m_Baseline", (float)desc.baseline);
        SetFI(ref face, "m_DescentLine", (float)desc.descentLine);
        SetFI(ref face, "m_SuperscriptOffset", (float)desc.pointSize);
        SetFI(ref face, "m_SuperscriptSize", 0.5f);
        SetFI(ref face, "m_SubscriptOffset", -(float)desc.pointSize * 0.25f);
        SetFI(ref face, "m_SubscriptSize", 0.5f);
        SetFI(ref face, "m_UnderlineOffset", -1f);
        SetFI(ref face, "m_UnderlineThickness", 1f);
        SetFI(ref face, "m_StrikethroughOffset", (float)desc.capLine * 0.5f);
        SetFI(ref face, "m_StrikethroughThickness", 1f);
        SetFI(ref face, "m_TabWidth", (float)(desc.pointSize * 2));

        // 4) FontAsset
        var font = ScriptableObject.CreateInstance<TMP_FontAsset>();
        font.name = desc.name;
        SetField(font, "m_Version", "1.1.0");
        SetField(font, "m_FaceInfo", face);
        SetField(font, "m_GlyphTable", glyphTable);
        SetField(font, "m_CharacterTable", charTable);
        SetField(font, "m_AtlasTextures", new Texture2D[] { tex });
        SetField(font, "m_AtlasTextureIndex", 0);
        SetField(font, "m_AtlasWidth", desc.atlasWidth);
        SetField(font, "m_AtlasHeight", desc.atlasHeight);
        SetField(font, "m_AtlasPadding", desc.padding);
        SetField(font, "m_AtlasRenderMode", GlyphRenderMode.RASTER);
        font.atlasPopulationMode = AtlasPopulationMode.Static;
        font.isMultiAtlasTexturesEnabled = false;

        // 5) Material: shader colorido usa o RGB do atlas (cores ja desenhadas na fonte)
        var shader = Shader.Find("TMP/PixelColorBitmap");
        if (shader == null) shader = Shader.Find("TextMeshPro/Bitmap");
        var mat = new Material(shader);
        mat.name = desc.name + " Material";
        mat.SetTexture("_MainTex", tex);
        SetField(font, "m_Material", mat);
        font.material = mat;

        font.ReadFontAssetDefinition();

        // 6) Salvar
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null) AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.CreateAsset(font, assetPath);
        mat.name = desc.name + " Material";
        AssetDatabase.AddObjectToAsset(mat, font);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        Debug.Log($"[PixelFont] OK: {assetPath} | glyphs={glyphTable.Count} chars={charTable.Count} atlas={desc.atlasWidth}x{desc.atlasHeight}");
    }

    static void SetField(object obj, string name, object value)
    {
        var f = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (f == null) { Debug.LogWarning("[PixelFont] campo nao encontrado: " + name); return; }
        f.SetValue(obj, value);
    }

    static void SetFI(ref object face, string name, object value)
    {
        var f = typeof(FaceInfo).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (f == null) { Debug.LogWarning("[PixelFont] FaceInfo campo nao encontrado: " + name); return; }
        f.SetValue(face, value);
    }
}
