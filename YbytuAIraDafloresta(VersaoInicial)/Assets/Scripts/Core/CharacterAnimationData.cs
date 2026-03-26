using UnityEngine;

/// <summary>
/// Define os clips de animacao de um personagem.
/// Cada AnimatorOverrideController substitui os clips base mantendo a mesma state machine.
/// </summary>
[CreateAssetMenu(fileName = "NewAnimationData", menuName = "Game/Character Animation Data")]
public class CharacterAnimationData : ScriptableObject
{
    [Header("Animator")]
    public AnimatorOverrideController animatorOverride;

    [Header("Informacoes")]
    public string skinName;
    [TextArea] public string description;
}
