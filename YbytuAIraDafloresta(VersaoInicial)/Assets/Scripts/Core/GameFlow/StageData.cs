using UnityEngine;

/// <summary>
/// Dados de uma fase do jogo.
/// Cada fase tem sua cena, cutscenes e configuracoes.
/// </summary>
[CreateAssetMenu(fileName = "NewStage", menuName = "Game/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Identidade")]
    public string stageName;
    public int stageIndex;
    [TextArea] public string description;
    public Sprite stageIcon;

    [Header("Cenas")]
    public string gameplaySceneName;
    public string introCutsceneId;
    public string outroCutsceneId;

    [Header("Desbloqueio")]
    public bool unlockedByDefault;
    public StageData requiredStage;

    [Header("Pontuacao")]
    public int maxScore = 10000;
    public float timeBonusMultiplier = 10f;
    public float healthBonusMultiplier = 100f;
}
