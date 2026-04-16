using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Setup das variants cosmeticas (PunkDark + BrawlerGirlEnemyDark).
/// Reusa sprites recoloridos via Python (recolor_sprites.py).
/// Cria, pra cada variant: importers, clips, override controller, animData,
/// enemySkin, enemyData (stats copiados do original) e prefab duplicado.
///
/// Rodar via menu Tools/Setup/Setup Color Variants.
/// </summary>
public static class SetupColorVariants
{
    [MenuItem("Tools/Setup/Setup Color Variants")]
    public static void Run()
    {
        SetupPunkDark();
        SetupBrawlerDark();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SetupColorVariants] OK. PunkDark + BrawlerEnemyDark prontos.");
    }

    // ==================== PUNK DARK ====================
    private static void SetupPunkDark()
    {
        const string root = "Assets/Sprites_Temporarios/Sprites/Enemy-Punk-Dark/";
        ConfigureImporters(root);

        var idle  = CreateClip("Assets/Animations/Player/Clips/EnemyPunkDark_Idle.anim",  BuildFrames(root, "Idle/idle",  4),  8f, true);
        var walk  = CreateClip("Assets/Animations/Player/Clips/EnemyPunkDark_Walk.anim",  BuildFrames(root, "Walk/walk",  4), 12f, true);
        var punch = CreateClip("Assets/Animations/Player/Clips/EnemyPunkDark_Punch.anim", BuildFrames(root, "Punch/punch", 3), 15f, false, attackEventFrame: 1);
        // Hurt usa apenas 3 frames (igual ao Punk original) - hurt4 e o "voa pra longe", removido.
        var hurt  = CreateClip("Assets/Animations/Player/Clips/EnemyPunkDark_Hurt.anim",  BuildFrames(root, "Hurt/hurt",   3), 12f, false);

        var overrideController = CreateOverride(
            "Assets/Animations/Player/EnemyPunkDark_Override.overrideController",
            "EnemyPunkDark_Override",
            new Dictionary<string, AnimationClip> {
                { "Idle", idle }, { "Walk", walk }, { "Punch", punch }, { "Hurt", hurt }
            });

        var animData = CreateAnimData(
            "Assets/Data/EnemySkins/EnemyPunkDark_AnimData.asset",
            overrideController,
            "Enemy Punk Dark",
            "Variant cosmetica do Punk com paleta mais sombria (rosa/vermelho dessaturados).");

        var srcSkin = AssetDatabase.LoadAssetAtPath<EnemySkin>("Assets/Data/EnemySkins/EnemyPunk_EnemySkin.asset");
        CreateEnemySkin(
            "Assets/Data/EnemySkins/EnemyPunkDark_EnemySkin.asset",
            "Enemy Punk Dark",
            animData,
            srcSkin);

        var srcData = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/EnemyData/EnemyPunk_EnemyData.asset");
        var data = CreateEnemyData(
            "Assets/Data/EnemyData/EnemyPunkDark_EnemyData.asset",
            "Enemy Punk Dark",
            srcData);

        var enemySkin = AssetDatabase.LoadAssetAtPath<EnemySkin>("Assets/Data/EnemySkins/EnemyPunkDark_EnemySkin.asset");
        DuplicatePrefab(
            "Assets/Prefabs/Enemies/EnemyPunk_Enemy.prefab",
            "Assets/Prefabs/Enemies/EnemyPunkDark_Enemy.prefab",
            "EnemyPunkDark_Enemy",
            data, enemySkin);
    }

    // ==================== BRAWLER DARK ====================
    private static void SetupBrawlerDark()
    {
        const string root = "Assets/Sprites_Temporarios/Sprites/Brawler-Girl-Dark/";
        const string dir = "Assets/Animations/Enemy/BrawlerGirl/Dark/";
        ConfigureImporters(root);
        EnsureFolder(dir);

        var idle  = CreateClip(dir + "BrawlerGirlEnemyDark_Idle.anim",  BuildFrames(root, "Idle/idle",   4),  8f, true);
        var walk  = CreateClip(dir + "BrawlerGirlEnemyDark_Walk.anim",  BuildFrames(root, "Walk/walk",  10), 12f, true);
        var punch = CreateClip(dir + "BrawlerGirlEnemyDark_Punch.anim", BuildFrames(root, "Punch/punch", 3), 15f, false, attackEventFrame: 1);
        var hurt  = CreateClip(dir + "BrawlerGirlEnemyDark_Hurt.anim",  BuildFrames(root, "Hurt/hurt",   2), 12f, false);
        var jab   = CreateClip(dir + "BrawlerGirlEnemyDark_Jab.anim",   BuildFrames(root, "Jab/jab",     3), 15f, false, attackEventFrame: 1);
        var kick  = CreateClip(dir + "BrawlerGirlEnemyDark_Kick.anim",  BuildFrames(root, "Kick/kick",   5), 15f, false, attackEventFrame: 3);

        var overrideController = CreateOverride(
            dir + "BrawlerGirlEnemyDark_Override.overrideController",
            "BrawlerGirlEnemyDark_Override",
            new Dictionary<string, AnimationClip> {
                { "Idle", idle }, { "Walk", walk }, { "Punch", punch }, { "Hurt", hurt },
                { "Jab", jab }, { "Kick", kick }
            });

        var animData = CreateAnimData(
            "Assets/Data/EnemySkins/BrawlerGirlEnemyDark_AnimData.asset",
            overrideController,
            "Brawler Girl (Enemy Dark)",
            "Variant cosmetica da Brawler com camiseta vermelho->bordo.");

        var srcSkin = AssetDatabase.LoadAssetAtPath<EnemySkin>("Assets/Data/EnemySkins/BrawlerGirl_EnemySkin.asset");
        CreateEnemySkin(
            "Assets/Data/EnemySkins/BrawlerGirlEnemyDark_EnemySkin.asset",
            "Brawler Girl Dark",
            animData,
            srcSkin);

        var srcData = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/EnemyData/BrawlerGirl_EnemyData.asset");
        var data = CreateEnemyData(
            "Assets/Data/EnemyData/BrawlerGirlEnemyDark_EnemyData.asset",
            "Brawler Girl Dark",
            srcData);

        var enemySkin = AssetDatabase.LoadAssetAtPath<EnemySkin>("Assets/Data/EnemySkins/BrawlerGirlEnemyDark_EnemySkin.asset");
        DuplicatePrefab(
            "Assets/Prefabs/Enemies/BrawlerGirl_Enemy.prefab",
            "Assets/Prefabs/Enemies/BrawlerGirlEnemyDark_Enemy.prefab",
            "BrawlerGirlEnemyDark_Enemy",
            data, enemySkin);
    }

    // ==================== HELPERS ====================
    private static void ConfigureImporters(string root)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { root.TrimEnd('/') });
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
        if (changed > 0) Debug.Log($"[SetupColorVariants] Importers ajustados em {changed} PNGs sob {root}.");
    }

    private static string[] BuildFrames(string root, string prefix, int count)
    {
        var arr = new string[count];
        for (int i = 0; i < count; i++) arr[i] = root + prefix + (i + 1) + ".png";
        return arr;
    }

    private static AnimationClip CreateClip(string clipPath, string[] spritePaths, float frameRate, bool loop, int attackEventFrame = -1)
    {
        var clip = new AnimationClip { frameRate = frameRate };
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        var binding = new EditorCurveBinding { type = typeof(SpriteRenderer), path = "", propertyName = "m_Sprite" };
        var keyframes = new ObjectReferenceKeyframe[spritePaths.Length];
        for (int i = 0; i < spritePaths.Length; i++)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePaths[i]);
            if (sprite == null)
            {
                var subs = AssetDatabase.LoadAllAssetsAtPath(spritePaths[i]);
                foreach (var s in subs) if (s is Sprite sp) { sprite = sp; break; }
            }
            if (sprite == null) Debug.LogWarning($"[SetupColorVariants] Sprite nulo em {spritePaths[i]}");
            keyframes[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = sprite };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        if (attackEventFrame >= 0 && attackEventFrame < spritePaths.Length)
            AnimationUtility.SetAnimationEvents(clip, new[] {
                new AnimationEvent { time = attackEventFrame / frameRate, functionName = "OnAttackHit" }
            });

        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(clip, existing);
            return existing;
        }
        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static AnimatorOverrideController CreateOverride(string path, string name, Dictionary<string, AnimationClip> clipsBySlot)
    {
        var baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/Player/PlayerBase.controller");
        var overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
        if (overrideController == null)
        {
            overrideController = new AnimatorOverrideController(baseController) { name = name };
            AssetDatabase.CreateAsset(overrideController, path);
        }
        else
        {
            overrideController.runtimeAnimatorController = baseController;
        }

        var current = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(current);
        var newList = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        // Fallback: Idle se nada casar
        AnimationClip fallback = clipsBySlot.ContainsKey("Idle") ? clipsBySlot["Idle"] : null;
        foreach (var pair in current)
        {
            string n = pair.Key.name;
            AnimationClip val = fallback;
            foreach (var kv in clipsBySlot)
            {
                if (n.Contains(kv.Key)) { val = kv.Value; break; }
            }
            newList.Add(new KeyValuePair<AnimationClip, AnimationClip>(pair.Key, val));
        }
        overrideController.ApplyOverrides(newList);
        EditorUtility.SetDirty(overrideController);
        return overrideController;
    }

    private static CharacterAnimationData CreateAnimData(string path, AnimatorOverrideController overrideController, string skinName, string description)
    {
        var animData = AssetDatabase.LoadAssetAtPath<CharacterAnimationData>(path);
        if (animData == null)
        {
            animData = ScriptableObject.CreateInstance<CharacterAnimationData>();
            AssetDatabase.CreateAsset(animData, path);
        }
        animData.animatorOverride = overrideController;
        animData.skinName = skinName;
        animData.description = description;
        EditorUtility.SetDirty(animData);
        return animData;
    }

    private static EnemySkin CreateEnemySkin(string path, string skinName, CharacterAnimationData animData, EnemySkin srcSkin)
    {
        var skin = AssetDatabase.LoadAssetAtPath<EnemySkin>(path);
        if (skin == null)
        {
            skin = ScriptableObject.CreateInstance<EnemySkin>();
            AssetDatabase.CreateAsset(skin, path);
        }
        skin.skinName = skinName;
        skin.animationData = animData;
        if (srcSkin != null)
        {
            skin.tintColor = srcSkin.tintColor;
            skin.spriteScale = srcSkin.spriteScale;
            skin.defaultFacesRight = srcSkin.defaultFacesRight;
            skin.feetYOffset = srcSkin.feetYOffset;
        }
        EditorUtility.SetDirty(skin);
        return skin;
    }

    private static EnemyData CreateEnemyData(string path, string enemyName, EnemyData srcData)
    {
        var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<EnemyData>();
            AssetDatabase.CreateAsset(data, path);
        }
        if (srcData != null)
        {
            data.enemyName = enemyName;
            data.maxHealth = srcData.maxHealth;
            data.moveSpeed = srcData.moveSpeed;
            data.patrolRadius = srcData.patrolRadius;
            data.patrolWaitTime = srcData.patrolWaitTime;
            data.detectionRange = srcData.detectionRange;
            data.loseTargetRange = srcData.loseTargetRange;
            data.attackDamage = srcData.attackDamage;
            data.attackRange = srcData.attackRange;
            data.attackYTolerance = srcData.attackYTolerance;
            data.attackCooldown = srcData.attackCooldown;
            data.hurtRecoveryTime = srcData.hurtRecoveryTime;
            data.scoreValue = srcData.scoreValue;
            data.behaviorType = srcData.behaviorType;
            data.attackTriggers = srcData.attackTriggers;
        }
        EditorUtility.SetDirty(data);
        return data;
    }

    private static void DuplicatePrefab(string src, string dst, string newName, EnemyData enemyData, EnemySkin enemySkin)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(src) == null)
        {
            Debug.LogError("[SetupColorVariants] Prefab base nao encontrado: " + src);
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(dst) == null)
        {
            AssetDatabase.CopyAsset(src, dst);
            AssetDatabase.ImportAsset(dst);
        }

        var prefabRoot = PrefabUtility.LoadPrefabContents(dst);
        prefabRoot.name = newName;

        var components = prefabRoot.GetComponentsInChildren<MonoBehaviour>(true);
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
                if (obj is EnemyData)      { prop.objectReferenceValue = enemyData; changed = true; }
                else if (obj is EnemySkin) { prop.objectReferenceValue = enemySkin; changed = true; }
            }
            if (changed) so.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, dst);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        Debug.Log("[SetupColorVariants] Prefab criado em " + dst);
    }

    private static void EnsureFolder(string folder)
    {
        var parts = folder.TrimEnd('/').Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
