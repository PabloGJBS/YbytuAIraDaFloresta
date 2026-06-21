using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SetupTractorBoss
{
    [MenuItem("Tools/Setup/Build Tractor + Explosions")]
    public static void Run()
    {
        CreatureAnimationBuilder.BuildCreature("Tractor");
        CreatureAnimationBuilder.BuildCreature("Explosions");

        foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Animations/Explosions" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) continue;
            var s = AnimationUtility.GetAnimationClipSettings(clip);
            s.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, s);
            EditorUtility.SetDirty(clip);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TractorBoss] Tractor + Explosions: clips/controllers gerados.");
    }
}
