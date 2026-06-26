using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SetupFase2Variants
{
    private const string SpritesBase = "Assets/Sprites_Temporarios/Sprites/";
    private const string ClipsRoot = "Assets/Animations/Enemy/Fase2/";

    private class Variant
    {
        public string key;
        public string skinPath, dataPath, prefabPath;
        public string baseFolder, altFolder; // nomes de pasta sob SpritesBase
        public string altPrefix;             // ex.: EnemyPunkAlt
    }

    private static readonly Variant[] Variants =
    {
        new Variant { key="Punk",     skinPath="Assets/Data/EnemySkins/EnemyPunk_EnemySkin.asset",      dataPath="Assets/Data/EnemyData/EnemyPunk_EnemyData.asset",      prefabPath="Assets/Prefabs/Enemies/EnemyPunk_Enemy.prefab",      baseFolder="Enemy-Punk",      altFolder="Enemy-Punk-Alt",      altPrefix="EnemyPunkAlt" },
        new Variant { key="Gangster", skinPath="Assets/Data/EnemySkins/EnemyGangster2_EnemySkin.asset", dataPath="Assets/Data/EnemyData/EnemyGangster2_EnemyData.asset", prefabPath="Assets/Prefabs/Enemies/EnemyGangster2_Enemy.prefab", baseFolder="Enemy-Gangster2", altFolder="Enemy-Gangster2-Alt", altPrefix="EnemyGangster2Alt" },
        new Variant { key="Raider",   skinPath="Assets/Data/EnemySkins/EnemyRaider3_EnemySkin.asset",   dataPath="Assets/Data/EnemyData/EnemyRaider3_EnemyData.asset",   prefabPath="Assets/Prefabs/Enemies/EnemyRaider3_Enemy.prefab",   baseFolder="Enemy-Raider3",   altFolder="Enemy-Raider3-Alt",   altPrefix="EnemyRaider3Alt" },
        new Variant { key="Brawler",  skinPath="Assets/Data/EnemySkins/BrawlerGirl_EnemySkin.asset",    dataPath="Assets/Data/EnemyData/BrawlerGirl_EnemyData.asset",    prefabPath="Assets/Prefabs/Enemies/BrawlerGirl_Enemy.prefab",    baseFolder="Brawler-Girl",    altFolder="Brawler-Girl-Alt",    altPrefix="BrawlerGirlAlt" },
    };

    [MenuItem("Tools/Setup/Setup Fase2 Enemy Variants")]
    public static void Run()
    {
        AssetDatabase.Refresh();
        foreach (var v in Variants)
        {
            try { Build(v); }
            catch (System.Exception e) { Debug.LogError($"[Fase2Variants] {v.key} FALHOU: {e}"); }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Fase2Variants] OK. Variantes Fase2 prontas (Punk/Gangster/Raider/Brawler).");
    }

    private static void Build(Variant v)
    {
        string altRoot = SpritesBase + v.altFolder;
        ConfigureImporters(altRoot);

        var baseSkin = AssetDatabase.LoadAssetAtPath<EnemySkin>(v.skinPath);
        if (baseSkin == null) { Debug.LogError($"[Fase2Variants] EnemySkin nao encontrado: {v.skinPath}"); return; }
        var baseAnimData = baseSkin.animationData;
        if (baseAnimData == null || baseAnimData.animatorOverride == null) { Debug.LogError($"[Fase2Variants] {v.key}: animData/override base nulo."); return; }
        var baseOverride = baseAnimData.animatorOverride;
        var baseController = baseOverride.runtimeAnimatorController;

        string clipDir = ClipsRoot + v.key + "/";
        EnsureFolder(clipDir);
        var overridesList = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        baseOverride.GetOverrides(overridesList);

        var newList = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        foreach (var pair in overridesList)
        {
            AnimationClip baseClip = pair.Value;
            if (baseClip == null) { newList.Add(new KeyValuePair<AnimationClip, AnimationClip>(pair.Key, null)); continue; }
            string clipPath = clipDir + v.altPrefix + "_" + Sanitize(baseClip.name) + ".anim";
            var altClip = CloneClipRemap(baseClip, clipPath, v.baseFolder, v.altFolder);
            newList.Add(new KeyValuePair<AnimationClip, AnimationClip>(pair.Key, altClip));
        }

        // 2) Override controller alt
        string ovPath = clipDir + v.altPrefix + "_Override.overrideController";
        var altOverride = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ovPath);
        if (altOverride == null)
        {
            altOverride = new AnimatorOverrideController(baseController) { name = v.altPrefix + "_Override" };
            AssetDatabase.CreateAsset(altOverride, ovPath);
        }
        else { altOverride.runtimeAnimatorController = baseController; }
        altOverride.ApplyOverrides(newList);
        EditorUtility.SetDirty(altOverride);

        string baseAnimPath = AssetDatabase.GetAssetPath(baseAnimData);
        string altAnimPath = "Assets/Data/EnemySkins/" + v.altPrefix + "_AnimData.asset";
        var altAnimData = CopyAndLoad<CharacterAnimationData>(baseAnimPath, altAnimPath);
        altAnimData.animatorOverride = altOverride;
        altAnimData.skinName = baseAnimData.skinName + " (Fase2)";
        EditorUtility.SetDirty(altAnimData);

        string altSkinPath = "Assets/Data/EnemySkins/" + v.altPrefix + "_EnemySkin.asset";
        var altSkin = CopyAndLoad<EnemySkin>(v.skinPath, altSkinPath);
        altSkin.animationData = altAnimData;
        altSkin.skinName = baseSkin.skinName + " Fase2";
        EditorUtility.SetDirty(altSkin);

        string altDataPath = "Assets/Data/EnemyData/" + v.altPrefix + "_EnemyData.asset";
        var baseData = AssetDatabase.LoadAssetAtPath<EnemyData>(v.dataPath);
        var altData = CopyAndLoad<EnemyData>(v.dataPath, altDataPath);
        altData.enemyName = baseData != null ? baseData.enemyName + " Fase2" : v.altPrefix;
        EditorUtility.SetDirty(altData);

        string altPrefabPath = "Assets/Prefabs/Enemies/" + v.altPrefix + "_Enemy.prefab";
        DuplicatePrefab(v.prefabPath, altPrefabPath, v.altPrefix + "_Enemy", altData, altSkin);

        Debug.Log($"[Fase2Variants] {v.key}: {newList.Count} clips, prefab {altPrefabPath}");
    }

    private static AnimationClip CloneClipRemap(AnimationClip baseClip, string clipPath, string baseFolder, string altFolder)
    {
        var clip = new AnimationClip { frameRate = baseClip.frameRate };
        var settings = AnimationUtility.GetAnimationClipSettings(baseClip);
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string from = "/" + baseFolder + "/";
        string to = "/" + altFolder + "/";

        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(baseClip))
        {
            var keys = AnimationUtility.GetObjectReferenceCurve(baseClip, binding);
            var newKeys = new ObjectReferenceKeyframe[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                var sp = keys[i].value as Sprite;
                Object val = keys[i].value;
                if (sp != null)
                {
                    string p = AssetDatabase.GetAssetPath(sp);
                    string ap = p.Replace(from, to);
                    var alt = AssetDatabase.LoadAssetAtPath<Sprite>(ap);
                    if (alt == null)
                    {
                        var subs = AssetDatabase.LoadAllAssetsAtPath(ap);
                        foreach (var s in subs) if (s is Sprite spr) { alt = spr; break; }
                    }
                    if (alt != null) val = alt;
                    else Debug.LogWarning($"[Fase2Variants] sprite alt nao encontrado: {ap}");
                }
                newKeys[i] = new ObjectReferenceKeyframe { time = keys[i].time, value = val };
            }
            AnimationUtility.SetObjectReferenceCurve(clip, binding, newKeys);
        }

        var evts = AnimationUtility.GetAnimationEvents(baseClip);
        if (evts != null && evts.Length > 0) AnimationUtility.SetAnimationEvents(clip, evts);

        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (existing != null) { EditorUtility.CopySerialized(clip, existing); return existing; }
        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static T CopyAndLoad<T>(string srcPath, string dstPath) where T : Object
    {
        if (AssetDatabase.LoadAssetAtPath<T>(dstPath) == null)
        {
            AssetDatabase.CopyAsset(srcPath, dstPath);
            AssetDatabase.ImportAsset(dstPath);
        }
        return AssetDatabase.LoadAssetAtPath<T>(dstPath);
    }

    private static void DuplicatePrefab(string src, string dst, string newName, EnemyData enemyData, EnemySkin enemySkin)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(src) == null) { Debug.LogError("[Fase2Variants] Prefab base nao encontrado: " + src); return; }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(dst) == null) { AssetDatabase.CopyAsset(src, dst); AssetDatabase.ImportAsset(dst); }

        var root = PrefabUtility.LoadPrefabContents(dst);
        root.name = newName;
        var components = root.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var comp in components)
        {
            if (comp == null) continue;
            var so = new SerializedObject(comp);
            var prop = so.GetIterator();
            bool changed = false;
            while (prop.NextVisible(true))
            {
                if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                var obj = prop.objectReferenceValue;
                if (obj is EnemyData) { prop.objectReferenceValue = enemyData; changed = true; }
                else if (obj is EnemySkin) { prop.objectReferenceValue = enemySkin; changed = true; }
            }
            if (changed) so.ApplyModifiedPropertiesWithoutUndo();
        }
        PrefabUtility.SaveAsPrefabAsset(root, dst);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void ConfigureImporters(string root)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { root });
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
            if (imp.spritePixelsPerUnit != 32f) { imp.spritePixelsPerUnit = 32f; dirty = true; }
            if (imp.filterMode != FilterMode.Point) { imp.filterMode = FilterMode.Point; dirty = true; }
            if (imp.textureCompression != TextureImporterCompression.Uncompressed) { imp.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
            if (imp.mipmapEnabled) { imp.mipmapEnabled = false; dirty = true; }
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            if (settings.spriteMeshType != SpriteMeshType.FullRect) { settings.spriteMeshType = SpriteMeshType.FullRect; imp.SetTextureSettings(settings); dirty = true; }
            if (settings.spriteAlignment != (int)SpriteAlignment.Center) { settings.spriteAlignment = (int)SpriteAlignment.Center; imp.SetTextureSettings(settings); dirty = true; }
            if (dirty) { imp.SaveAndReimport(); changed++; }
        }
        if (changed > 0) Debug.Log($"[Fase2Variants] Importers ajustados em {changed} PNGs sob {root}.");
    }

    private static string Sanitize(string name)
    {
        int i = name.LastIndexOf('_');
        return i >= 0 && i < name.Length - 1 ? name.Substring(i + 1) : name;
    }

    private static void EnsureFolder(string folder)
    {
        var parts = folder.TrimEnd('/').Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
