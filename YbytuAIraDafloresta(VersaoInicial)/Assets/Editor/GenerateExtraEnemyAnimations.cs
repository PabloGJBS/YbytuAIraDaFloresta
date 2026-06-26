using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class GenerateExtraEnemyAnimations
{
    private const string ExtraClipsDir = "Assets/Animations/Player/Clips/"; // mesmo dir dos existentes
    private const string BrawlerEnemyDir = "Assets/Animations/Enemy/BrawlerGirl/";

    [MenuItem("Tools/Setup/Generate Extra Enemy Animations")]
    public static void Run()
    {
        ConfigureImportersForRoot("Assets/Sprites_Temporarios/Sprites/Enemy-Gangster2");
        ConfigureImportersForRoot("Assets/Sprites_Temporarios/Sprites/Enemy-Raider3");
        ConfigureImportersForRoot("Assets/Sprites_Temporarios/Sprites/Enemy-Chefe1");
        ConfigureImportersForRoot("Assets/Sprites_Temporarios/Sprites/Brawler-Girl");

        GenerateForGangster2();
        GenerateForRaider3();
        GenerateForChefe1();
        GenerateForBrawlerEnemy();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GenerateExtraEnemyAnimations] OK. Clips extras gerados + Brawler-Enemy separado.");
    }

    private static void GenerateForGangster2()
    {
        const string root = "Assets/Sprites_Temporarios/Sprites/Enemy-Gangster2/";
        CreateClip(ExtraClipsDir + "EnemyGangster2_Attack2.anim", BuildFrames(root, "Attack2/attack2", 4), 15f, false, attackEventFrame: 2);
        CreateClip(ExtraClipsDir + "EnemyGangster2_Attack3.anim", BuildFrames(root, "Attack3/attack3", 6), 15f, false, attackEventFrame: 4);
        CreateClip(ExtraClipsDir + "EnemyGangster2_Dead.anim",    BuildFrames(root, "Dead/dead",       5), 10f, false);
        CreateClip(ExtraClipsDir + "EnemyGangster2_Idle2.anim",   BuildFrames(root, "Idle2/idle2",     13), 8f,  true);
        CreateClip(ExtraClipsDir + "EnemyGangster2_Jump.anim",    BuildFrames(root, "Jump/jump",       10), 12f, false);
        CreateClip(ExtraClipsDir + "EnemyGangster2_Run.anim",     BuildFrames(root, "Run/run",         10), 14f, true);
    }

    private static void GenerateForRaider3()
    {
        const string root = "Assets/Sprites_Temporarios/Sprites/Enemy-Raider3/";
        CreateClip(ExtraClipsDir + "EnemyRaider3_Attack2.anim",   BuildFrames(root, "Attack2/attack2", 5), 15f, false, attackEventFrame: 3);
        CreateClip(ExtraClipsDir + "EnemyRaider3_Attack3.anim",   BuildFrames(root, "Attack3/attack3", 4), 15f, false, attackEventFrame: 2);
        CreateClip(ExtraClipsDir + "EnemyRaider3_Dead.anim",      BuildFrames(root, "Dead/dead",       4), 10f, false);
        CreateClip(ExtraClipsDir + "EnemyRaider3_Idle2.anim",     BuildFrames(root, "Idle2/idle2",     5), 8f,  true);
        CreateClip(ExtraClipsDir + "EnemyRaider3_Jump.anim",      BuildFrames(root, "Jump/jump",       8), 12f, false);
        CreateClip(ExtraClipsDir + "EnemyRaider3_Run.anim",       BuildFrames(root, "Run/run",         8), 14f, true);
    }

    private static void GenerateForChefe1()
    {
        const string root = "Assets/Sprites_Temporarios/Sprites/Enemy-Chefe1/";
        CreateClip(ExtraClipsDir + "EnemyChefe1_Dead.anim",       BuildFrames(root, "Dead/dead",       5),  10f, false);
        CreateClip(ExtraClipsDir + "EnemyChefe1_Idle2.anim",      BuildFrames(root, "Idle2/idle2",     14), 8f,  true);
        CreateClip(ExtraClipsDir + "EnemyChefe1_Jump.anim",       BuildFrames(root, "Jump/jump",       10), 12f, false);
        CreateClip(ExtraClipsDir + "EnemyChefe1_Run.anim",        BuildFrames(root, "Run/run",         10), 14f, true);
        CreateClip(ExtraClipsDir + "EnemyChefe1_Shot.anim",       BuildFrames(root, "Shot/shot",       12), 15f, false, attackEventFrame: 8);
        CreateClip(ExtraClipsDir + "EnemyChefe1_Recharge.anim",   BuildFrames(root, "Recharge/recharge", 6), 12f, false);
    }

    private static void GenerateForBrawlerEnemy()
    {
        EnsureFolder(BrawlerEnemyDir);
        const string root = "Assets/Sprites_Temporarios/Sprites/Brawler-Girl/";

        var idle  = CreateClip(BrawlerEnemyDir + "BrawlerGirlEnemy_Idle.anim",  BuildFrames(root, "Idle/idle",   4),  8f, true);
        var walk  = CreateClip(BrawlerEnemyDir + "BrawlerGirlEnemy_Walk.anim",  BuildFrames(root, "Walk/walk",  10), 12f, true);
        var punch = CreateClip(BrawlerEnemyDir + "BrawlerGirlEnemy_Punch.anim", BuildFrames(root, "Punch/punch", 3), 15f, false, attackEventFrame: 1);
        var hurt  = CreateClip(BrawlerEnemyDir + "BrawlerGirlEnemy_Hurt.anim",  BuildFrames(root, "Hurt/hurt",   2), 12f, false);
        CreateClip(BrawlerEnemyDir + "BrawlerGirlEnemy_Jab.anim",  BuildFrames(root, "Jab/jab",   3), 15f, false, attackEventFrame: 1);
        CreateClip(BrawlerEnemyDir + "BrawlerGirlEnemy_Kick.anim", BuildFrames(root, "Kick/kick", 5), 15f, false, attackEventFrame: 3);

        var baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/Player/PlayerBase.controller");
        if (baseController == null)
        {
            Debug.LogError("[GenerateExtraEnemyAnimations] PlayerBase.controller nao encontrado.");
            return;
        }

        const string overridePath = BrawlerEnemyDir + "BrawlerGirlEnemy_Override.overrideController";
        var overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);
        if (overrideController == null)
        {
            overrideController = new AnimatorOverrideController(baseController) { name = "BrawlerGirlEnemy_Override" };
            AssetDatabase.CreateAsset(overrideController, overridePath);
        }
        else
        {
            overrideController.runtimeAnimatorController = baseController;
        }

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);
        var newOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        foreach (var pair in overrides)
        {
            string n = pair.Key.name;
            AnimationClip val;
            if (n.Contains("Idle")) val = idle;
            else if (n.Contains("Walk")) val = walk;
            else if (n.Contains("Punch")) val = punch;
            else if (n.Contains("Hurt")) val = hurt;
            else val = idle;
            newOverrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(pair.Key, val));
        }
        overrideController.ApplyOverrides(newOverrides);
        EditorUtility.SetDirty(overrideController);

        const string animDataPath = "Assets/Data/EnemySkins/BrawlerGirlEnemy_AnimData.asset";
        var animData = AssetDatabase.LoadAssetAtPath<CharacterAnimationData>(animDataPath);
        if (animData == null)
        {
            animData = ScriptableObject.CreateInstance<CharacterAnimationData>();
            AssetDatabase.CreateAsset(animData, animDataPath);
        }
        animData.animatorOverride = overrideController;
        animData.skinName = "Brawler Girl (Enemy)";
        animData.description = "Inimiga Brawler - clips separados do player Ybytu.";
        EditorUtility.SetDirty(animData);

        var enemySkin = AssetDatabase.LoadAssetAtPath<EnemySkin>("Assets/Data/EnemySkins/BrawlerGirl_EnemySkin.asset");
        if (enemySkin != null)
        {
            enemySkin.animationData = animData;
            EditorUtility.SetDirty(enemySkin);
            Debug.Log("[GenerateExtraEnemyAnimations] BrawlerGirl_EnemySkin agora usa AnimData proprio.");
        }
        else
        {
            Debug.LogWarning("[GenerateExtraEnemyAnimations] BrawlerGirl_EnemySkin.asset nao encontrado.");
        }
    }

    private static string[] BuildFrames(string rootPath, string prefixWithSlash, int count)
    {
        var arr = new string[count];
        for (int i = 0; i < count; i++) arr[i] = rootPath + prefixWithSlash + (i + 1) + ".png";
        return arr;
    }

    private static AnimationClip CreateClip(string clipPath, string[] spritePaths, float frameRate, bool loop, int attackEventFrame = -1)
    {
        var clip = new AnimationClip { frameRate = frameRate };
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };
        var keyframes = new ObjectReferenceKeyframe[spritePaths.Length];
        for (int i = 0; i < spritePaths.Length; i++)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePaths[i]);
            if (sprite == null)
            {
                var subs = AssetDatabase.LoadAllAssetsAtPath(spritePaths[i]);
                foreach (var s in subs) if (s is Sprite sp) { sprite = sp; break; }
            }
            if (sprite == null) Debug.LogWarning($"[GenerateExtraEnemyAnimations] Sprite nulo em {spritePaths[i]}");
            keyframes[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = sprite };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        if (attackEventFrame >= 0 && attackEventFrame < spritePaths.Length)
        {
            AnimationUtility.SetAnimationEvents(clip, new[] {
                new AnimationEvent { time = attackEventFrame / frameRate, functionName = "OnAttackHit" }
            });
        }

        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(clip, existing);
            return existing;
        }
        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static void ConfigureImportersForRoot(string root)
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

            if (dirty)
            {
                imp.SaveAndReimport();
                changed++;
            }
        }
        if (changed > 0)
            Debug.Log($"[GenerateExtraEnemyAnimations] Importers ajustados em {changed} PNGs sob {root}.");
    }

    private static void EnsureFolder(string folder)
    {
        var parts = folder.TrimEnd('/').Split('/');
        string current = parts[0]; // "Assets"
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
