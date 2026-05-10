#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public static class SaveSlotAnimSetup
{
    const string AnimDir = "Assets/Animations/UI/SaveSlot";
    const string FramesDir = "Assets/Sprites/UI/SaveSelect/SlotFrameAnim";
    const float FPS = 6f;

    [MenuItem("Tools/SaveSlot/Setup Burning Animator")]
    public static void Setup()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");
        if (!AssetDatabase.IsValidFolder("Assets/Animations/UI"))
            AssetDatabase.CreateFolder("Assets/Animations", "UI");
        if (!AssetDatabase.IsValidFolder(AnimDir))
            AssetDatabase.CreateFolder("Assets/Animations/UI", "SaveSlot");

        var sprites = new Sprite[6];
        for (int i = 0; i < 6; i++)
        {
            string path = $"{FramesDir}/SlotFrame_{(i + 1):D2}.png";
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprites[i] == null)
            {
                Debug.LogError($"Sprite not found: {path}");
                return;
            }
        }

        var binding = new EditorCurveBinding
        {
            type = typeof(Image),
            path = "",
            propertyName = "m_Sprite"
        };

        // Idle: frame 1 static
        var idle = MakeClip("SaveSlot_Idle", binding,
            new[] { new ObjectReferenceKeyframe { time = 0f, value = sprites[0] } },
            loop: false, stopTime: 1f / FPS);

        // Ignition: frames 1..6 once
        var igniteKeys = new ObjectReferenceKeyframe[6];
        for (int i = 0; i < 6; i++)
            igniteKeys[i] = new ObjectReferenceKeyframe { time = i / FPS, value = sprites[i] };
        var ignite = MakeClip("SaveSlot_BurningIgnite", binding, igniteKeys,
            loop: false, stopTime: 6f / FPS);

        // Loop: alterna frame 5 e 6 enquanto selecionado
        var loopKeys = new[]
        {
            new ObjectReferenceKeyframe { time = 0f,        value = sprites[4] },
            new ObjectReferenceKeyframe { time = 1f / FPS,  value = sprites[5] },
        };
        var loop = MakeClip("SaveSlot_BurningLoop", binding, loopKeys,
            loop: true, stopTime: 2f / FPS);

        string idlePath = $"{AnimDir}/SaveSlot_Idle.anim";
        string ignitePath = $"{AnimDir}/SaveSlot_BurningIgnite.anim";
        string loopPath = $"{AnimDir}/SaveSlot_BurningLoop.anim";
        SaveOrReplace(idle, idlePath);
        SaveOrReplace(ignite, ignitePath);
        SaveOrReplace(loop, loopPath);

        // Controller (recria do zero)
        string ctrlPath = $"{AnimDir}/SaveSlot.controller";
        AssetDatabase.DeleteAsset(ctrlPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        ctrl.AddParameter("isSelected", AnimatorControllerParameterType.Bool);

        var sm = ctrl.layers[0].stateMachine;
        var idleState = sm.AddState("Idle");
        idleState.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(idlePath);
        var igniteState = sm.AddState("BurningIgnite");
        igniteState.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(ignitePath);
        var loopState = sm.AddState("BurningLoop");
        loopState.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(loopPath);
        sm.defaultState = idleState;

        // Idle -> Ignite quando isSelected=true
        AddTransition(idleState, igniteState, hasExit: false, duration: 0.05f,
            cond: ("isSelected", AnimatorConditionMode.If));

        // Ignite -> Loop ao terminar (exit time = 1)
        var t = igniteState.AddTransition(loopState);
        t.hasExitTime = true;
        t.exitTime = 1f;
        t.duration = 0f;

        // Ignite -> Idle se desselecionar no meio
        AddTransition(igniteState, idleState, hasExit: false, duration: 0.05f,
            cond: ("isSelected", AnimatorConditionMode.IfNot));

        // Loop -> Idle quando isSelected=false
        AddTransition(loopState, idleState, hasExit: false, duration: 0.05f,
            cond: ("isSelected", AnimatorConditionMode.IfNot));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SaveSlotAnimSetup] OK @ {FPS} FPS. Idle/Ignite/Loop + controller.");
    }

    static AnimationClip MakeClip(string name, EditorCurveBinding binding, ObjectReferenceKeyframe[] keys,
        bool loop, float stopTime)
    {
        var clip = new AnimationClip { name = name, frameRate = FPS };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = loop;
        s.stopTime = stopTime;
        AnimationUtility.SetAnimationClipSettings(clip, s);
        return clip;
    }

    static void SaveOrReplace(AnimationClip clip, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(clip, existing);
        }
        else
        {
            AssetDatabase.CreateAsset(clip, path);
        }
    }

    static void AddTransition(AnimatorState from, AnimatorState to, bool hasExit, float duration,
        (string name, AnimatorConditionMode mode) cond)
    {
        var t = from.AddTransition(to);
        t.hasExitTime = hasExit;
        t.duration = duration;
        t.AddCondition(cond.mode, 0, cond.name);
    }
}
#endif
