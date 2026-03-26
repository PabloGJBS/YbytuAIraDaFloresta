using UnityEngine;

/// <summary>
/// ScriptableObject para trocar o visual do cenario (stage).
/// Centraliza backgrounds, foregrounds e tilesets.
/// </summary>
[CreateAssetMenu(fileName = "NewStageSkin", menuName = "Game/Stage Skin")]
public class StageSkin : ScriptableObject
{
    [Header("Identidade")]
    public string stageName;

    [Header("Layers")]
    public Sprite background;
    public Sprite foreground;
    public Sprite tileset;

    [Header("Props")]
    public Sprite[] props;
}
