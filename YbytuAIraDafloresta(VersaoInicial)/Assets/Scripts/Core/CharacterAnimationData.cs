using UnityEngine;

[CreateAssetMenu(fileName = "NewAnimationData", menuName = "Game/Character Animation Data")]
public class CharacterAnimationData : ScriptableObject
{
    [Header("Animator")]
    public AnimatorOverrideController animatorOverride;

    [Header("Informacoes")]
    public string skinName;
    [TextArea] public string description;
}
