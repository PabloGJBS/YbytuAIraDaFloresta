using UnityEngine;

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
