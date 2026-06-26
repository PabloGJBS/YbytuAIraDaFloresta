using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterSkin", menuName = "Game/Character Skin")]
public class CharacterSkin : ScriptableObject
{
    [Header("Identidade")]
    public string characterName;
    public Sprite portrait;

    [Header("Animacoes")]
    public CharacterAnimationData animationData;

    [Header("Configuracoes")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Visual")]
    public Color tintColor = Color.white;
    public Vector2 spriteScale = Vector2.one;
}
