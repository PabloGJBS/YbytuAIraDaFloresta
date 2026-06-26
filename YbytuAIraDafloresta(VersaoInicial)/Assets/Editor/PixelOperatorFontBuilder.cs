using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class PixelOperatorFontBuilder
{
    private const string TtfPath = "Assets/Fonts/Pixel/PixelOperator-Bold.ttf";
    private const string OutPath = "Assets/Fonts/Pixel/PixelOperator-Bold SDF.asset";

    private const string CharSet =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;!?()[]-+/%\"'" +
        "áàâãéêíóôõúüçÁÀÂÃÉÊÍÓÔÕÚÜÇ";

    [MenuItem("Tools/Ybytu/Build PixelOperator TMP Font")]
    public static void Build()
    {
        var font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
        if (font == null) { Debug.LogError($"[PixelOperatorFontBuilder] TTF nao encontrado: {TtfPath}"); return; }

        var fa = TMP_FontAsset.CreateFontAsset(font, 64, 6, GlyphRenderMode.SDFAA, 512, 512,
                                               AtlasPopulationMode.Dynamic, enableMultiAtlasSupport: true);
        if (fa == null) { Debug.LogError("[PixelOperatorFontBuilder] Falha ao criar o TMP_FontAsset."); return; }
        fa.name = "PixelOperator-Bold SDF";

        fa.TryAddCharacters(CharSet);

        AssetDatabase.DeleteAsset(OutPath);
        AssetDatabase.CreateAsset(fa, OutPath);
        if (fa.atlasTextures != null)
        {
            foreach (var tex in fa.atlasTextures)
            {
                if (tex == null) continue;
                tex.name = fa.name + " Atlas";
                AssetDatabase.AddObjectToAsset(tex, fa);
            }
        }
        if (fa.material != null)
        {
            fa.material.name = fa.name + " Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
        }

        fa.atlasPopulationMode = AtlasPopulationMode.Static;
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(OutPath);
        Debug.Log($"[PixelOperatorFontBuilder] Fonte TMP criada: {OutPath}");
    }
}
