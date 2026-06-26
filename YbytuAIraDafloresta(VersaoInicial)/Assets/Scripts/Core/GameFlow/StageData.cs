using UnityEngine;

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
    [Tooltip("Cutscene finalizadora que roda DEPOIS da tela de score. Ao terminar, vai pra proxima fase (ou Menu se for a ultima).")]
    public string finalizerCutsceneId;

    [Header("Desbloqueio")]
    public bool unlockedByDefault;
    public StageData requiredStage;

    [Header("Pontuacao")]
    public int maxScore = 10000;
    public float timeBonusMultiplier = 10f;
    public float healthBonusMultiplier = 100f;
}
