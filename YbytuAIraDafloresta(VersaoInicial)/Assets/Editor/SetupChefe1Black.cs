using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SetupChefe1Black
{
    private const string Root = "Assets/Sprites_Temporarios/Sprites/Enemy-Chefe1-BlackHair/";
    private const string ClipsDir = "Assets/Animations/Player/Clips/";
    private const string AnimDataPath = "Assets/Data/EnemySkins/EnemyChefe1Black_AnimData.asset";
    private const string EnemySkinPath = "Assets/Data/EnemySkins/EnemyChefe1Black_EnemySkin.asset";
    private const string EnemyDataPath = "Assets/Data/EnemyData/EnemyChefe1Black_EnemyData.asset";
    private const string OverridePath = "Assets/Animations/Player/EnemyChefe1Black_Override.overrideController";
    private const string PrefabSrc = "Assets/Prefabs/Enemies/EnemyChefe1_Enemy.prefab";
    private const string PrefabDst = "Assets/Prefabs/Enemies/EnemyChefe1Black_Enemy.prefab";

    [MenuItem("Tools/Setup/Setup Chefe1 Black Variant")]
    public static void Run()
    {
        ConfigureImporters();
        var clips = CreateClips();
        var overrideController = CreateOverride(clips);
        var animData = CreateAnimData(overrideController);
        var enemySkin = CreateEnemySkin(animData);
        var enemyData = CreateEnemyData();
        DuplicatePrefab(enemyData, enemySkin);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SetupChefe1Black] OK. Variant pronto. Prefab em " + PrefabDst);
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
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            if (settings.spriteMeshType != SpriteMeshType.FullRect) { settings.spriteMeshType = SpriteMeshType.FullRect; imp.SetTextureSettings(settings); dirty = true; }
            if (settings.spriteAlignment != (int)SpriteAlignment.Center) { settings.spriteAlignment = (int)SpriteAlignment.Center; imp.SetTextureSettings(settings); dirty = true; }

            if (dirty) { imp.SaveAndReimport(); changed++; }
        }
        if (changed > 0) Debug.Log($"[SetupChefe1Black] Importers ajustados em {changed} PNGs.");
    }

    // ---------- 2) Clips ----------
    private static Dictionary<string, AnimationClip> CreateClips()
    {
        var clips = new Dictionary<string, AnimationClip>();
        clips["Idle"]     = CreateClip(ClipsDir + "EnemyChefe1Black_Idle.anim",     BuildFrames("Idle/idle",         7),  8f, true);
        clips["Idle2"]    = CreateClip(ClipsDir + "EnemyChefe1Black_Idle2.anim",    BuildFrames("Idle2/idle2",      14),  8f, true);
        clips["Walk"]     = CreateClip(ClipsDir + "EnemyChefe1Black_Walk.anim",     BuildFrames("Walk/walk",        10), 12f, true);
        clips["Run"]      = CreateClip(ClipsDir + "EnemyChefe1Black_Run.anim",      BuildFrames("Run/run",          10), 14f, true);
        clips["Punch"]    = CreateClip(ClipsDir + "EnemyChefe1Black_Punch.anim",    BuildFrames("Punch/punch",       5), 12f, false, attackEventFrame: 3);
        clips["Hurt"]     = CreateClip(ClipsDir + "EnemyChefe1Black_Hurt.anim",     BuildFrames("Hurt/hurt",         4), 12f, false);
        clips["Dead"]     = CreateClip(ClipsDir + "EnemyChefe1Black_Dead.anim",     BuildFrames("Dead/dead",         5), 10f, false);
        clips["Jump"]     = CreateClip(ClipsDir + "EnemyChefe1Black_Jump.anim",     BuildFrames("Jump/jump",        10), 12f, false);
        clips["Shot"]     = CreateClip(ClipsDir + "EnemyChefe1Black_Shot.anim",     BuildFrames("Shot/shot",        12), 15f, false, attackEventFrame: 8);
        clips["Recharge"] = CreateClip(ClipsDir + "EnemyChefe1Black_Recharge.anim", BuildFrames("Recharge/recharge", 6), 12f, false);
        return clips;
    }

    private static string[] BuildFrames(string prefixWithSlash, int count)
    {
        var arr = new string[count];
        for (int i = 0; i < count; i++) arr[i] = Root + prefixWithSlash + (i + 1) + ".png";
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
            if (sprite == null) Debug.LogWarning($"[SetupChefe1Black] Sprite nulo em {spritePaths[i]}");
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

    private static AnimatorOverrideController CreateOverride(Dictionary<string, AnimationClip> clips)
    {
        var baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/Player/PlayerBase.controller");
        var overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(OverridePath);
        if (overrideController == null)
        {
            overrideController = new AnimatorOverrideController(baseController) { name = "EnemyChefe1Black_Override" };
            AssetDatabase.CreateAsset(overrideController, OverridePath);
        }
        else
        {
            overrideController.runtimeAnimatorController = baseController;
        }

        var current = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(current);
        var newList = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        foreach (var pair in current)
        {
            string n = pair.Key.name;
            AnimationClip val;
            if (n.Contains("Idle"))      val = clips["Idle"];
            else if (n.Contains("Walk")) val = clips["Walk"];
            else if (n.Contains("Punch"))val = clips["Punch"];
            else if (n.Contains("Hurt")) val = clips["Hurt"];
            else if (n.Contains("Jab"))  val = clips["Shot"];
            else if (n.Contains("Kick")) val = clips["Recharge"];
            else if (n.Contains("Jump")) val = clips["Jump"];
            else val = clips["Idle"];
            newList.Add(new KeyValuePair<AnimationClip, AnimationClip>(pair.Key, val));
        }
        overrideController.ApplyOverrides(newList);
        EditorUtility.SetDirty(overrideController);
        return overrideController;
    }

    private static CharacterAnimationData CreateAnimData(AnimatorOverrideController overrideController)
    {
        var animData = AssetDatabase.LoadAssetAtPath<CharacterAnimationData>(AnimDataPath);
        if (animData == null)
        {
            animData = ScriptableObject.CreateInstance<CharacterAnimationData>();
            AssetDatabase.CreateAsset(animData, AnimDataPath);
        }
        animData.animatorOverride = overrideController;
        animData.skinName = "Chefe 1 (Black Hair)";
        animData.description = "Variant cosmetica do Chefe1 com cabelo preto. Mesma logica de combate.";
        EditorUtility.SetDirty(animData);
        return animData;
    }

    private static EnemySkin CreateEnemySkin(CharacterAnimationData animData)
    {
        var srcSkin = AssetDatabase.LoadAssetAtPath<EnemySkin>("Assets/Data/EnemySkins/EnemyChefe1_EnemySkin.asset");
        var skin = AssetDatabase.LoadAssetAtPath<EnemySkin>(EnemySkinPath);
        if (skin == null)
        {
            skin = ScriptableObject.CreateInstance<EnemySkin>();
            AssetDatabase.CreateAsset(skin, EnemySkinPath);
        }
        skin.skinName = "Chefe 1 Black";
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

    private static EnemyData CreateEnemyData()
    {
        var src = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/EnemyData/EnemyChefe1_EnemyData.asset");
        var data = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyDataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<EnemyData>();
            AssetDatabase.CreateAsset(data, EnemyDataPath);
        }
        if (src != null)
        {
            data.enemyName = "Chefe 1 Black";
            data.maxHealth = src.maxHealth;
            data.moveSpeed = src.moveSpeed;
            data.patrolRadius = src.patrolRadius;
            data.patrolWaitTime = src.patrolWaitTime;
            data.detectionRange = src.detectionRange;
            data.loseTargetRange = src.loseTargetRange;
            data.attackDamage = src.attackDamage;
            data.attackRange = src.attackRange;
            data.attackYTolerance = src.attackYTolerance;
            data.attackCooldown = src.attackCooldown;
            data.hurtRecoveryTime = src.hurtRecoveryTime;
            data.scoreValue = src.scoreValue;
            data.behaviorType = src.behaviorType;
            data.attackTriggers = src.attackTriggers;
        }
        EditorUtility.SetDirty(data);
        return data;
    }

    private static void DuplicatePrefab(EnemyData enemyData, EnemySkin enemySkin)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabSrc) == null)
        {
            Debug.LogError("[SetupChefe1Black] Prefab base nao encontrado: " + PrefabSrc);
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDst) == null)
        {
            AssetDatabase.CopyAsset(PrefabSrc, PrefabDst);
            AssetDatabase.ImportAsset(PrefabDst);
        }

        var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabDst);
        prefabRoot.name = "EnemyChefe1Black_Enemy";

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

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabDst);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        Debug.Log("[SetupChefe1Black] Prefab criado em " + PrefabDst);
    }
}
