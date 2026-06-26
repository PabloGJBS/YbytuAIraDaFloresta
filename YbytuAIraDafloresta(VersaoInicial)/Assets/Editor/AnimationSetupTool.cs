using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class AnimationSetupTool
{
    private const string SpritesPath = "Assets/Sprites_Temporarios/Sprites/Brawler-Girl";
    private const string AnimationsBasePath = "Assets/Animations";
    private const string PlayerAnimPath = "Assets/Animations/Player";
    private const string DataPath = "Assets/Data/CharacterSkins";

    private static readonly Dictionary<string, float> FrameRates = new Dictionary<string, float>
    {
        { "Idle", 8f },
        { "Walk", 12f },
        { "Jump", 10f },
        { "Jab", 15f },
        { "Kick", 12f },
        { "Punch", 15f },
        { "Jump_kick", 12f },
        { "Dive_kick", 12f },
        { "Hurt", 8f }
    };

    private static readonly HashSet<string> LoopingAnims = new HashSet<string>
    {
        "Idle", "Walk"
    };

    [MenuItem("Tools/Setup Animacoes Brawler-Girl")]
    public static void SetupAll()
    {
        CreateFolders();
        var clips = CreateAnimationClips();
        var baseController = CreateBaseAnimatorController(clips);
        var overrideController = CreateOverrideController(baseController, clips);
        CreateCharacterSkinAssets(overrideController);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[AnimationSetup] Setup completo! Animator Controller, Override e CharacterSkin criados.");
    }

    private static void CreateFolders()
    {
        string[] folders = { PlayerAnimPath, PlayerAnimPath + "/Clips", DataPath };
        foreach (var folder in folders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                var parent = Path.GetDirectoryName(folder).Replace("\\", "/");
                var name = Path.GetFileName(folder);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }

    private static Dictionary<string, AnimationClip> CreateAnimationClips()
    {
        var clips = new Dictionary<string, AnimationClip>();
        var animFolders = AssetDatabase.GetSubFolders(SpritesPath);

        foreach (var folderPath in animFolders)
        {
            var animName = Path.GetFileName(folderPath);
            var spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });

            var sprites = spriteGuids
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p)
                .Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p))
                .Where(s => s != null)
                .ToArray();

            if (sprites.Length == 0) continue;

            float frameRate = FrameRates.ContainsKey(animName) ? FrameRates[animName] : 10f;
            bool loop = LoopingAnims.Contains(animName);

            var clip = CreateClip(animName, sprites, frameRate, loop);
            var clipPath = $"{PlayerAnimPath}/Clips/BrawlerGirl_{animName}.anim";
            AssetDatabase.CreateAsset(clip, clipPath);
            clips[animName] = clip;

            Debug.Log($"[AnimationSetup] Clip criado: {animName} ({sprites.Length} frames, {frameRate}fps, loop={loop})");
        }

        return clips;
    }

    private static AnimationClip CreateClip(string name, Sprite[] sprites, float frameRate, bool loop)
    {
        var clip = new AnimationClip();
        clip.name = name;
        clip.frameRate = frameRate;

        var keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = sprites[i]
            };
        }

        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        return clip;
    }

    private static AnimatorController CreateBaseAnimatorController(Dictionary<string, AnimationClip> clips)
    {
        var controllerPath = $"{PlayerAnimPath}/PlayerBase.controller";

        // Remove existente para recriar
        if (File.Exists(controllerPath))
            AssetDatabase.DeleteAsset(controllerPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Kick", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Jab", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("DiveKick", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("JumpKick", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AttackType", AnimatorControllerParameterType.Int);

        var rootStateMachine = controller.layers[0].stateMachine;

        // Criar states
        var idleState = AddState(rootStateMachine, "Idle", clips, true);
        var walkState = AddState(rootStateMachine, "Walk", clips, false);
        var jumpState = AddState(rootStateMachine, "Jump", clips, false);
        var punchState = AddState(rootStateMachine, "Punch", clips, false);
        var kickState = AddState(rootStateMachine, "Kick", clips, false);
        var jabState = AddState(rootStateMachine, "Jab", clips, false);
        var hurtState = AddState(rootStateMachine, "Hurt", clips, false);
        var jumpKickState = AddState(rootStateMachine, "Jump_kick", clips, false);
        var diveKickState = AddState(rootStateMachine, "Dive_kick", clips, false);

        rootStateMachine.defaultState = idleState;

        // --- Transitions ---

        // Idle -> Walk (Speed > 0.1)
        var t = idleState.AddTransition(walkState);
        t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        t.hasExitTime = false;
        t.duration = 0.1f;

        // Walk -> Idle (Speed < 0.1)
        t = walkState.AddTransition(idleState);
        t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        t.hasExitTime = false;
        t.duration = 0.1f;

        // Any -> Jump
        t = rootStateMachine.AddAnyStateTransition(jumpState);
        t.AddCondition(AnimatorConditionMode.If, 0, "Jump");
        t.hasExitTime = false;
        t.duration = 0.05f;

        // Jump -> Idle (exit time)
        t = jumpState.AddTransition(idleState);
        t.hasExitTime = true;
        t.exitTime = 1f;
        t.duration = 0.1f;

        // Any -> Punch
        t = rootStateMachine.AddAnyStateTransition(punchState);
        t.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        t.hasExitTime = false;
        t.duration = 0.05f;

        // Punch -> Idle (exit time)
        t = punchState.AddTransition(idleState);
        t.hasExitTime = true;
        t.exitTime = 1f;
        t.duration = 0.1f;

        // Any -> Kick
        t = rootStateMachine.AddAnyStateTransition(kickState);
        t.AddCondition(AnimatorConditionMode.If, 0, "Kick");
        t.hasExitTime = false;
        t.duration = 0.05f;

        // Kick -> Idle
        t = kickState.AddTransition(idleState);
        t.hasExitTime = true;
        t.exitTime = 1f;
        t.duration = 0.1f;

        // Any -> Jab
        t = rootStateMachine.AddAnyStateTransition(jabState);
        t.AddCondition(AnimatorConditionMode.If, 0, "Jab");
        t.hasExitTime = false;
        t.duration = 0.05f;

        // Jab -> Idle
        t = jabState.AddTransition(idleState);
        t.hasExitTime = true;
        t.exitTime = 1f;
        t.duration = 0.1f;

        // Any -> Hurt
        t = rootStateMachine.AddAnyStateTransition(hurtState);
        t.AddCondition(AnimatorConditionMode.If, 0, "Hurt");
        t.hasExitTime = false;
        t.duration = 0.05f;

        // Hurt -> Idle
        t = hurtState.AddTransition(idleState);
        t.hasExitTime = true;
        t.exitTime = 1f;
        t.duration = 0.1f;

        // Any -> JumpKick
        t = rootStateMachine.AddAnyStateTransition(jumpKickState);
        t.AddCondition(AnimatorConditionMode.If, 0, "JumpKick");
        t.hasExitTime = false;
        t.duration = 0.05f;

        // JumpKick -> Idle
        t = jumpKickState.AddTransition(idleState);
        t.hasExitTime = true;
        t.exitTime = 1f;
        t.duration = 0.1f;

        // Any -> DiveKick
        t = rootStateMachine.AddAnyStateTransition(diveKickState);
        t.AddCondition(AnimatorConditionMode.If, 0, "DiveKick");
        t.hasExitTime = false;
        t.duration = 0.05f;

        // DiveKick -> Idle
        t = diveKickState.AddTransition(idleState);
        t.hasExitTime = true;
        t.exitTime = 1f;
        t.duration = 0.1f;

        Debug.Log("[AnimationSetup] Animator Controller base criado com todos os states e transitions.");
        return controller;
    }

    private static AnimatorState AddState(AnimatorStateMachine sm, string animName, Dictionary<string, AnimationClip> clips, bool isDefault)
    {
        var state = sm.AddState(animName.Replace("_", " "));
        if (clips.ContainsKey(animName))
            state.motion = clips[animName];
        return state;
    }

    private static AnimatorOverrideController CreateOverrideController(
        AnimatorController baseController, Dictionary<string, AnimationClip> clips)
    {
        var overridePath = $"{PlayerAnimPath}/BrawlerGirl_Override.overrideController";

        var ov = new AnimatorOverrideController(baseController);
        AssetDatabase.CreateAsset(ov, overridePath);

        Debug.Log("[AnimationSetup] Override Controller criado: BrawlerGirl_Override");
        return ov;
    }

    private static void CreateCharacterSkinAssets(AnimatorOverrideController overrideController)
    {
        // CharacterAnimationData
        var animData = ScriptableObject.CreateInstance<CharacterAnimationData>();
        animData.skinName = "Brawler Girl (Temporario)";
        animData.description = "Sprites temporarios da Brawler Girl - Streets of Fight pack. Substituir pelo Ybytu no futuro.";
        animData.animatorOverride = overrideController;
        AssetDatabase.CreateAsset(animData, $"{DataPath}/BrawlerGirl_AnimData.asset");

        // CharacterSkin
        var skin = ScriptableObject.CreateInstance<CharacterSkin>();
        skin.characterName = "Brawler Girl (Temporario)";
        skin.animationData = animData;
        skin.moveSpeed = 5f;
        skin.jumpForce = 10f;
        skin.tintColor = Color.white;
        skin.spriteScale = Vector2.one;
        AssetDatabase.CreateAsset(skin, $"{DataPath}/BrawlerGirl_Skin.asset");

        Debug.Log("[AnimationSetup] CharacterSkin e AnimationData criados em Data/CharacterSkins/");
    }
}
