using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

/// <summary>
/// Setup do EnemyPunk: cria clips, override controller, AnimData e atualiza EnemySkin.
/// Rodar uma vez via menu "Tools/Setup/Enemy Punk".
/// </summary>
public static class EnemyPunkSetup
{
    [MenuItem("Tools/Setup/Enemy Punk")]
    public static void Run()
    {
        const string punk = "Assets/Sprites_Temporarios/Sprites/Enemy-Punk/";

        var idleClip = CreateClip("Assets/Animations/Player/Clips/EnemyPunk_Idle.anim",
            new[] { punk + "Idle/idle1.png", punk + "Idle/idle2.png", punk + "Idle/idle3.png", punk + "Idle/idle4.png" }, 8f, true);
        var walkClip = CreateClip("Assets/Animations/Player/Clips/EnemyPunk_Walk.anim",
            new[] { punk + "Walk/walk1.png", punk + "Walk/walk2.png", punk + "Walk/walk3.png", punk + "Walk/walk4.png" }, 12f, true);
        var punchClip = CreateClip("Assets/Animations/Player/Clips/EnemyPunk_Punch.anim",
            new[] { punk + "Punch/punch1.png", punk + "Punch/punch2.png", punk + "Punch/punch3.png" }, 15f, false);
        var hurtClip = CreateClip("Assets/Animations/Player/Clips/EnemyPunk_Hurt.anim",
            new[] { punk + "Hurt/hurt1.png", punk + "Hurt/hurt2.png", punk + "Hurt/hurt3.png", punk + "Hurt/hurt4.png" }, 12f, false);

        // OnAttackHit no penultimo frame do Punch (frame 1 de 3, time = 0.0667)
        AnimationUtility.SetAnimationEvents(punchClip, new[] {
            new AnimationEvent { time = 1f / 15f, functionName = "OnAttackHit" }
        });

        var baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/Player/PlayerBase.controller");
        var overrideController = new AnimatorOverrideController(baseController);
        overrideController.name = "EnemyPunk_Override";
        AssetDatabase.CreateAsset(overrideController, "Assets/Animations/Player/EnemyPunk_Override.overrideController");

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);
        var newOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        foreach (var pair in overrides)
        {
            string n = pair.Key.name;
            AnimationClip val;
            if (n.Contains("Idle")) val = idleClip;
            else if (n.Contains("Walk")) val = walkClip;
            else if (n.Contains("Punch")) val = punchClip;
            else if (n.Contains("Hurt")) val = hurtClip;
            else val = idleClip;
            newOverrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(pair.Key, val));
        }
        overrideController.ApplyOverrides(newOverrides);
        EditorUtility.SetDirty(overrideController);

        var animData = ScriptableObject.CreateInstance<CharacterAnimationData>();
        animData.animatorOverride = overrideController;
        animData.skinName = "Enemy Punk";
        animData.description = "Sprites Enemy Punk - Streets of Fight";
        AssetDatabase.CreateAsset(animData, "Assets/Data/CharacterSkins/EnemyPunk_AnimData.asset");

        var enemySkin = AssetDatabase.LoadAssetAtPath<EnemySkin>("Assets/Data/EnemySkins/EnemyPunk_EnemySkin.asset");
        enemySkin.animationData = animData;
        enemySkin.skinName = "Enemy Punk";
        enemySkin.tintColor = Color.white;
        EditorUtility.SetDirty(enemySkin);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[EnemyPunkSetup] OK: 4 clips + override + animData + skin.");
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
            keyframes[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = sprite };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }
}
