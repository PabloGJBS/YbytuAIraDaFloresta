using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class EnemyGangster2Setup
{
    private const string Root = "Assets/Sprites_Temporarios/Sprites/Enemy-Gangster2/";
    private const string IdLabel = "EnemyGangster2";
    private const string DisplayName = "Gangster 2";

    [MenuItem("Tools/Setup/Enemy Gangster2")]
    public static void Run()
    {
        ConfigureImporters();
        var (idleClip, walkClip, punchClip, hurtClip) = CreateClips();
        var overrideController = CreateOverride(idleClip, walkClip, punchClip, hurtClip);
        var animData = CreateAnimData(overrideController);
        var enemyData = CreateEnemyData();
        var enemySkin = CreateEnemySkin(animData);
        DuplicatePrefab(enemyData, enemySkin);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[EnemyGangster2Setup] OK: importers + 4 clips + override + animData + enemyData + skin + prefab.");
    }

    private static void ConfigureImporters()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Root.TrimEnd('/') });
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
            // FullRect mesh type
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
        Debug.Log($"[EnemyGangster2Setup] Importers ajustados em {changed} PNGs.");
    }

    // ---------- 2) Clips ----------
    private static (AnimationClip idle, AnimationClip walk, AnimationClip punch, AnimationClip hurt) CreateClips()
    {
        var idle = CreateClip(
            "Assets/Animations/Player/Clips/EnemyGangster2_Idle.anim",
            BuildFrames("Idle/idle", 7), 8f, true);

        var walk = CreateClip(
            "Assets/Animations/Player/Clips/EnemyGangster2_Walk.anim",
            BuildFrames("Walk/walk", 10), 12f, true);

        var punch = CreateClip(
            "Assets/Animations/Player/Clips/EnemyGangster2_Punch.anim",
            BuildFrames("Punch/punch", 6), 15f, false);

        var hurt = CreateClip(
            "Assets/Animations/Player/Clips/EnemyGangster2_Hurt.anim",
            BuildFrames("Hurt/hurt", 4), 12f, false);

        AnimationUtility.SetAnimationEvents(punch, new[] {
            new AnimationEvent { time = 4f / 15f, functionName = "OnAttackHit" }
        });

        return (idle, walk, punch, hurt);
    }

    private static string[] BuildFrames(string prefixWithSlash, int count)
    {
        var arr = new string[count];
        for (int i = 0; i < count; i++) arr[i] = Root + prefixWithSlash + (i + 1) + ".png";
        return arr;
    }

    private static AnimationClip CreateClip(string clipPath, string[] spritePaths, float frameRate, bool loop)
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
            if (sprite == null) Debug.LogWarning($"[EnemyGangster2Setup] Sprite nulo em {spritePaths[i]}");
            keyframes[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = sprite };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        // overwrite ou cria
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(clip, existing);
            return existing;
        }
        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static AnimatorOverrideController CreateOverride(AnimationClip idle, AnimationClip walk, AnimationClip punch, AnimationClip hurt)
    {
        var baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/Player/PlayerBase.controller");
        const string overridePath = "Assets/Animations/Player/EnemyGangster2_Override.overrideController";

        var overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);
        if (overrideController == null)
        {
            overrideController = new AnimatorOverrideController(baseController) { name = "EnemyGangster2_Override" };
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
        return overrideController;
    }

    private static CharacterAnimationData CreateAnimData(AnimatorOverrideController overrideController)
    {
        const string path = "Assets/Data/CharacterSkins/EnemyGangster2_AnimData.asset";
        var animData = AssetDatabase.LoadAssetAtPath<CharacterAnimationData>(path);
        if (animData == null)
        {
            animData = ScriptableObject.CreateInstance<CharacterAnimationData>();
            AssetDatabase.CreateAsset(animData, path);
        }
        animData.animatorOverride = overrideController;
        animData.skinName = DisplayName;
        animData.description = "Sprites Gangsters_2 (Craftpix) - inimigo forte 1";
        EditorUtility.SetDirty(animData);
        return animData;
    }

    private static EnemyData CreateEnemyData()
    {
        const string path = "Assets/Data/EnemyData/EnemyGangster2_EnemyData.asset";
        var data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<EnemyData>();
            AssetDatabase.CreateAsset(data, path);
        }
        data.enemyName = DisplayName;
        data.maxHealth = 45;
        data.moveSpeed = 2.3f;
        data.patrolRadius = 3f;
        data.patrolWaitTime = 1f;
        data.detectionRange = 6.5f;
        data.loseTargetRange = 9.5f;
        data.attackDamage = 14;
        data.attackRange = 1.3f;
        data.attackCooldown = 1.3f;
        data.scoreValue = 2200;
        EditorUtility.SetDirty(data);
        return data;
    }

    private static EnemySkin CreateEnemySkin(CharacterAnimationData animData)
    {
        const string path = "Assets/Data/EnemySkins/EnemyGangster2_EnemySkin.asset";
        var skin = AssetDatabase.LoadAssetAtPath<EnemySkin>(path);
        if (skin == null)
        {
            skin = ScriptableObject.CreateInstance<EnemySkin>();
            AssetDatabase.CreateAsset(skin, path);
        }
        skin.skinName = DisplayName;
        skin.animationData = animData;
        skin.tintColor = Color.white;
        skin.spriteScale = new Vector2(0.6f, 0.6f);
        skin.defaultFacesRight = false;
        EditorUtility.SetDirty(skin);
        return skin;
    }

    private static void DuplicatePrefab(EnemyData enemyData, EnemySkin enemySkin)
    {
        const string src = "Assets/Prefabs/Enemies/EnemyPunk_Enemy.prefab";
        const string dst = "Assets/Prefabs/Enemies/EnemyGangster2_Enemy.prefab";

        if (!AssetDatabase.LoadAssetAtPath<GameObject>(src))
        {
            Debug.LogError("[EnemyGangster2Setup] Prefab base EnemyPunk_Enemy.prefab nao encontrado em " + src);
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(dst) == null)
        {
            AssetDatabase.CopyAsset(src, dst);
            AssetDatabase.ImportAsset(dst);
        }

        var prefabRoot = PrefabUtility.LoadPrefabContents(dst);
        prefabRoot.name = "EnemyGangster2_Enemy";

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
                if (obj is EnemyData)
                {
                    prop.objectReferenceValue = enemyData;
                    changed = true;
                }
                else if (obj is EnemySkin)
                {
                    prop.objectReferenceValue = enemySkin;
                    changed = true;
                }
            }
            if (changed) so.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, dst);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        Debug.Log("[EnemyGangster2Setup] Prefab criado em " + dst);
    }
}
