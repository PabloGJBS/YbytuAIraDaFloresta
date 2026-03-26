using UnityEngine;

/// <summary>
/// Aplica um CharacterSkin ao personagem em runtime.
/// Troca animator, escala e tint de forma centralizada.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class CharacterSkinController : MonoBehaviour
{
    [SerializeField] private CharacterSkin currentSkin;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    public CharacterSkin CurrentSkin => currentSkin;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (currentSkin != null)
            ApplySkin(currentSkin);
    }

    public void ApplySkin(CharacterSkin skin)
    {
        currentSkin = skin;

        if (skin.animationData != null && skin.animationData.animatorOverride != null)
            animator.runtimeAnimatorController = skin.animationData.animatorOverride;

        spriteRenderer.color = skin.tintColor;
        transform.localScale = new Vector3(skin.spriteScale.x, skin.spriteScale.y, 1f);
    }

    public float GetMoveSpeed() => currentSkin != null ? currentSkin.moveSpeed : 5f;
    public float GetJumpForce() => currentSkin != null ? currentSkin.jumpForce : 10f;
}
