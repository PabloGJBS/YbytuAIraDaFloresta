using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Atualiza os AnimatorOverrideController dos inimigos pra mapear os slots
/// Jab/Kick (originalmente usados pelo player) pros clips de ataque variante:
///   Gangster2: Jab -> EnemyGangster2_Attack2,  Kick -> EnemyGangster2_Attack3
///   Raider3:   Jab -> EnemyRaider3_Attack2,    Kick -> EnemyRaider3_Attack3
///   Chefe1:    Jab -> EnemyChefe1_Shot,        Kick -> EnemyChefe1_Recharge
/// Demais slots ja existentes (Idle/Walk/Punch/Hurt) preservados.
///
/// Pareados com EnemyData.attackTriggers (lista de triggers a randomizar
/// em PerformAttack) e os triggers Jab/Kick ja existem em PlayerBase.controller.
/// </summary>
public static class WireAttackVariants
{
    [MenuItem("Tools/Setup/Wire Attack Variants")]
    public static void Run()
    {
        ApplyOverrides("Assets/Animations/Player/EnemyGangster2_Override.overrideController", new Dictionary<string, string>
        {
            { "BrawlerGirl_Jab",  "Assets/Animations/Player/Clips/EnemyGangster2_Attack2.anim" },
            { "BrawlerGirl_Kick", "Assets/Animations/Player/Clips/EnemyGangster2_Attack3.anim" },
        });

        ApplyOverrides("Assets/Animations/Player/EnemyRaider3_Override.overrideController", new Dictionary<string, string>
        {
            { "BrawlerGirl_Jab",  "Assets/Animations/Player/Clips/EnemyRaider3_Attack2.anim" },
            { "BrawlerGirl_Kick", "Assets/Animations/Player/Clips/EnemyRaider3_Attack3.anim" },
        });

        ApplyOverrides("Assets/Animations/Player/EnemyChefe1_Override.overrideController", new Dictionary<string, string>
        {
            { "BrawlerGirl_Jab",  "Assets/Animations/Player/Clips/EnemyChefe1_Shot.anim" },
            { "BrawlerGirl_Kick", "Assets/Animations/Player/Clips/EnemyChefe1_Recharge.anim" },
        });

        ApplyOverrides("Assets/Animations/Enemy/BrawlerGirl/BrawlerGirlEnemy_Override.overrideController", new Dictionary<string, string>
        {
            { "BrawlerGirl_Jab",  "Assets/Animations/Enemy/BrawlerGirl/BrawlerGirlEnemy_Jab.anim" },
            { "BrawlerGirl_Kick", "Assets/Animations/Enemy/BrawlerGirl/BrawlerGirlEnemy_Kick.anim" },
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[WireAttackVariants] OK. Jab/Kick mapeados em Gangster2/Raider3/Chefe1/BrawlerEnemy.");
    }

    /// <summary>
    /// Carrega o override controller, e pra cada chave (nome do slot do base) substitui
    /// o clip override pelo asset em assetPath. Slots nao listados ficam intocados.
    /// </summary>
    private static void ApplyOverrides(string overridePath, Dictionary<string, string> nameToClipPath)
    {
        var overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);
        if (overrideController == null)
        {
            Debug.LogWarning($"[WireAttackVariants] Override controller nao encontrado: {overridePath}");
            return;
        }

        var current = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(current);

        var newList = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        int replaced = 0;
        foreach (var pair in current)
        {
            string baseName = pair.Key.name;
            AnimationClip newVal = pair.Value;
            if (nameToClipPath.TryGetValue(baseName, out var path))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null)
                {
                    newVal = clip;
                    replaced++;
                }
                else
                {
                    Debug.LogWarning($"[WireAttackVariants] Clip nao encontrado: {path}");
                }
            }
            newList.Add(new KeyValuePair<AnimationClip, AnimationClip>(pair.Key, newVal));
        }
        overrideController.ApplyOverrides(newList);
        EditorUtility.SetDirty(overrideController);
        Debug.Log($"[WireAttackVariants] {System.IO.Path.GetFileNameWithoutExtension(overridePath)}: {replaced} slots mapeados.");
    }
}
