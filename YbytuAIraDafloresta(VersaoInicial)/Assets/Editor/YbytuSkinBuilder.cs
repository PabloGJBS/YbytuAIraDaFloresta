using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class YbytuSkinBuilder
{
    private const string SpritesRoot = "Assets/Sprites/Ybytu";
    private const string ClipsFolder = "Assets/Animations/Player/Clips";
    private const string OverrideAssetPath = "Assets/Animations/Player/Ybytu_Override.overrideController";
    private const string AnimDataAssetPath = "Assets/Data/CharacterSkins/Ybytu_AnimData.asset";
    private const string SkinAssetPath = "Assets/Data/CharacterSkins/Ybytu_Skin.asset";
    private const string PlayerBaseControllerPath = "Assets/Animations/Player/PlayerBase.controller";
    private const string BrawlerGirlSkinPath = "Assets/Data/CharacterSkins/BrawlerGirl_Skin.asset";

    private struct AnimSpec
    {
        public string clipName;
        public string spriteFolder;
        public string spritePrefix;
        public int frameCount;
        public int sampleRate;
        public bool loop;
        public string baseClipName;
    }

    private static readonly AnimSpec[] Specs = new[]
    {
        new AnimSpec { clipName = "Ybytu_Idle",      spriteFolder = "IDLE",      spritePrefix = "IDLE",      frameCount = 4,  sampleRate = 8,  loop = true,  baseClipName = "BrawlerGirl_Idle" },
        new AnimSpec { clipName = "Ybytu_Walk",      spriteFolder = "WALK",      spritePrefix = "WALK",      frameCount = 10, sampleRate = 12, loop = true,  baseClipName = "BrawlerGirl_Walk" },
        new AnimSpec { clipName = "Ybytu_Jump",      spriteFolder = "JUMP",      spritePrefix = "JUMP",      frameCount = 4,  sampleRate = 10, loop = false, baseClipName = "BrawlerGirl_Jump" },
        new AnimSpec { clipName = "Ybytu_Punch",     spriteFolder = "PUNCH",     spritePrefix = "PUNCH",     frameCount = 3,  sampleRate = 15, loop = false, baseClipName = "BrawlerGirl_Punch" },
        new AnimSpec { clipName = "Ybytu_Kick",      spriteFolder = "KICK",      spritePrefix = "KICK",      frameCount = 5,  sampleRate = 12, loop = false, baseClipName = "BrawlerGirl_Kick" },
        new AnimSpec { clipName = "Ybytu_Jab",       spriteFolder = "JAB",       spritePrefix = "JAB",       frameCount = 3,  sampleRate = 15, loop = false, baseClipName = "BrawlerGirl_Jab" },
        new AnimSpec { clipName = "Ybytu_Hurt",      spriteFolder = "HURT",      spritePrefix = "HURT",      frameCount = 2,  sampleRate = 8,  loop = false, baseClipName = "BrawlerGirl_Hurt" },
        new AnimSpec { clipName = "Ybytu_Jump_kick", spriteFolder = "JUMP_KICK", spritePrefix = "JUMP_KICK", frameCount = 3,  sampleRate = 12, loop = false, baseClipName = "BrawlerGirl_Jump_kick" },
        new AnimSpec { clipName = "Ybytu_Dive_kick", spriteFolder = "DIVE_KICK", spritePrefix = "DIVE_KICK", frameCount = 5,  sampleRate = 12, loop = false, baseClipName = "BrawlerGirl_Dive_kick" },
    };

    [MenuItem("Tools/Ybytu/Compare Sprite Dimensions")]
    public static void CompareDimensions()
    {
        var ybytuIdle = AssetDatabase.LoadAssetAtPath<Texture2D>($"{SpritesRoot}/IDLE/IDLE_1.png");
        var bgIdle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites_Temporarios/Sprites/Brawler-Girl/Idle/idle1.png");
        if (ybytuIdle != null) Debug.Log($"[YbytuSkinBuilder] Ybytu IDLE_1 PNG: {ybytuIdle.width} x {ybytuIdle.height} px");
        if (bgIdle != null) Debug.Log($"[YbytuSkinBuilder] Brawler Girl idle1 sprite rect: {bgIdle.rect.width} x {bgIdle.rect.height} px");

        var ybytuBounds = MeasureAlphaBounds($"{SpritesRoot}/IDLE/IDLE_1.png");
        if (ybytuBounds.HasValue)
        {
            var b = ybytuBounds.Value;
            Debug.Log($"[YbytuSkinBuilder] Ybytu IDLE_1 alpha bounds: {b.width}x{b.height} px (recortado do padding transparente)");
            if (bgIdle != null)
            {
                float bgH = bgIdle.rect.height;
                float yH = b.height;
                float scaleSuggested = bgH / yH;
                Debug.Log($"[YbytuSkinBuilder] Para igualar altura ao BG ({bgH}px -> {yH}px), spriteScale = {scaleSuggested:F3}");
                Debug.Log($"[YbytuSkinBuilder] Em world units (PPU 32 + scale 1): Ybytu personagem ~{(yH / 32f):F2} unidades de altura, BG ~{(bgH / 32f):F2} unidades");
            }
        }
    }

    private struct PixelRect { public int x, y, width, height; }

    private static PixelRect? MeasureAlphaBounds(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return null;
        bool wasReadable = imp.isReadable;
        if (!wasReadable) { imp.isReadable = true; imp.SaveAndReimport(); }
        try
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) return null;
            var pixels = tex.GetPixels32();
            int w = tex.width, h = tex.height;
            int minX = w, minY = h, maxX = -1, maxY = -1;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (pixels[y * w + x].a > 0)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
            if (maxX < 0) return null;
            return new PixelRect { x = minX, y = minY, width = maxX - minX + 1, height = maxY - minY + 1 };
        }
        finally
        {
            if (!wasReadable) { imp.isReadable = false; imp.SaveAndReimport(); }
        }
    }

    [MenuItem("Tools/Ybytu/Build Ybytu Skin (clips + override + skin)")]
    public static void BuildAll()
    {
        EnsureFolder("Assets/Data/CharacterSkins");
        EnsureFolder(ClipsFolder);

        var clipMap = new Dictionary<string, AnimationClip>();
        foreach (var spec in Specs)
        {
            var clip = BuildClip(spec);
            clipMap[spec.baseClipName] = clip;
        }

        var baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerBaseControllerPath);
        if (baseController == null) { Debug.LogError($"[YbytuSkinBuilder] PlayerBase.controller nao encontrado em {PlayerBaseControllerPath}"); return; }

        var existingOverride = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(OverrideAssetPath);
        AnimatorOverrideController over;
        if (existingOverride == null)
        {
            over = new AnimatorOverrideController(baseController);
            over.name = "Ybytu_Override";
            AssetDatabase.CreateAsset(over, OverrideAssetPath);
        }
        else
        {
            over = existingOverride;
            over.runtimeAnimatorController = baseController;
        }

        var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        over.GetOverrides(pairs);
        var newPairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        foreach (var p in pairs)
        {
            AnimationClip overrideClip = null;
            if (p.Key != null && clipMap.TryGetValue(p.Key.name, out var mapped))
                overrideClip = mapped;
            newPairs.Add(new KeyValuePair<AnimationClip, AnimationClip>(p.Key, overrideClip));
            Debug.Log($"[YbytuSkinBuilder] override {p.Key?.name} -> {(overrideClip != null ? overrideClip.name : "<none>")}");
        }
        over.ApplyOverrides(newPairs);
        EditorUtility.SetDirty(over);

        var animData = AssetDatabase.LoadAssetAtPath<CharacterAnimationData>(AnimDataAssetPath);
        if (animData == null)
        {
            animData = ScriptableObject.CreateInstance<CharacterAnimationData>();
            AssetDatabase.CreateAsset(animData, AnimDataAssetPath);
        }
        animData.animatorOverride = over;
        animData.skinName = "Ybytu";
        animData.description = "Sprites finais do Ybytu - protagonista da floresta amazonica.";
        EditorUtility.SetDirty(animData);

        var bgSkin = AssetDatabase.LoadAssetAtPath<CharacterSkin>(BrawlerGirlSkinPath);
        var skin = AssetDatabase.LoadAssetAtPath<CharacterSkin>(SkinAssetPath);
        if (skin == null)
        {
            skin = ScriptableObject.CreateInstance<CharacterSkin>();
            AssetDatabase.CreateAsset(skin, SkinAssetPath);
        }
        skin.characterName = "Ybytu";
        skin.animationData = animData;
        skin.moveSpeed = bgSkin != null ? bgSkin.moveSpeed : 5f;
        skin.jumpForce = bgSkin != null ? bgSkin.jumpForce : 10f;
        skin.tintColor = bgSkin != null ? bgSkin.tintColor : Color.white;
        skin.spriteScale = new Vector2(0.367f, 0.367f);
        EditorUtility.SetDirty(skin);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[YbytuSkinBuilder] Done. Override: {OverrideAssetPath} | AnimData: {AnimDataAssetPath} | Skin: {SkinAssetPath}");
    }

    private static AnimationClip BuildClip(AnimSpec spec)
    {
        string clipPath = $"{ClipsFolder}/{spec.clipName}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, clipPath);
        }
        clip.frameRate = spec.sampleRate;
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = spec.loop;
        settings.keepOriginalPositionY = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        var keyframes = new ObjectReferenceKeyframe[spec.frameCount];
        float dt = 1f / spec.sampleRate;
        for (int i = 0; i < spec.frameCount; i++)
        {
            string spritePath = $"{SpritesRoot}/{spec.spriteFolder}/{spec.spritePrefix}_{i + 1}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null) Debug.LogError($"[YbytuSkinBuilder] sprite nao encontrado: {spritePath}");
            keyframes[i] = new ObjectReferenceKeyframe { time = i * dt, value = sprite };
        }
        var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        EditorUtility.SetDirty(clip);
        Debug.Log($"[YbytuSkinBuilder] clip {spec.clipName}: {spec.frameCount} frames @ {spec.sampleRate} fps, loop={spec.loop}");
        return clip;
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath)) return;
        var parent = Path.GetDirectoryName(assetPath).Replace('\\', '/');
        var leaf = Path.GetFileName(assetPath);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
