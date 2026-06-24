using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemySkin", menuName = "Game/Enemy Skin")]
public class EnemySkin : ScriptableObject
{
    public string skinName;
    public CharacterAnimationData animationData;
    public Color tintColor = Color.white;
    public Vector2 spriteScale = Vector2.one;
    [Tooltip("Se true, o sprite original olha pra direita (flipX=false=direita). Se false, sprite original olha pra esquerda.")]
    public bool defaultFacesRight = true;
    [Tooltip("Offset vertical aplicado ao alvo de chase pra alinhar visualmente os pes do inimigo com os do player. " +
             "Necessario quando sprites tem proporcoes diferentes no frame (Ybytu preenche todo o frame; inimigos so parte). " +
             "Negativo abaixa o inimigo; positivo eleva. Ajustar in-Play via EnemyTestZone.chaseYOverride.")]
    public float feetYOffset = 0f;
}
