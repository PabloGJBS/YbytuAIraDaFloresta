using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controlador da tela de selecao de fases (mapa).
/// </summary>
public class StageSelectUI : MonoBehaviour
{
    [Header("Stage Buttons")]
    [SerializeField] private StageButtonUI[] stageButtons;

    [Header("Info Panel")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private TMP_Text stageNameText;
    [SerializeField] private TMP_Text stageDescriptionText;
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private TMP_Text bestRankText;
    [SerializeField] private Button playButton;

    [Header("Navegacao")]
    [SerializeField] private Button backButton;

    private StageData selectedStage;

    private void Start()
    {
        RefreshStages();
        HideInfoPanel();

        if (backButton != null)
            backButton.onClick.AddListener(OnBackButton);
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButton);
    }

    private void RefreshStages()
    {
        if (GameFlowManager.Instance == null) return;

        var stages = GameFlowManager.Instance.Stages;
        for (int i = 0; i < stageButtons.Length && i < stages.Length; i++)
        {
            bool unlocked = GameFlowManager.Instance.IsStageUnlocked(stages[i]);
            StageProgress progress = null;
            if (SaveManager.Instance != null)
                progress = SaveManager.Instance.GetStageProgress(stages[i].stageIndex);

            stageButtons[i].Setup(stages[i], unlocked, progress);
        }
    }

    public void OnStageSelected(StageData stage)
    {
        if (!GameFlowManager.Instance.IsStageUnlocked(stage)) return;

        selectedStage = stage;
        ShowInfoPanel(stage);
    }

    private void ShowInfoPanel(StageData stage)
    {
        if (infoPanel != null) infoPanel.SetActive(true);
        if (stageNameText != null) stageNameText.text = stage.stageName;
        if (stageDescriptionText != null) stageDescriptionText.text = stage.description;

        var progress = SaveManager.Instance?.GetStageProgress(stage.stageIndex);
        if (bestScoreText != null)
            bestScoreText.text = progress != null ? $"Melhor: {progress.bestScore}" : "Nao jogada";
        if (bestRankText != null)
            bestRankText.text = progress != null && !string.IsNullOrEmpty(progress.rankGrade) ? progress.rankGrade : "-";
    }

    private void HideInfoPanel()
    {
        if (infoPanel != null) infoPanel.SetActive(false);
    }

    private void OnPlayButton()
    {
        if (selectedStage != null && GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToStage(selectedStage);
    }

    private void OnBackButton()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToMainMenu();
    }
}
