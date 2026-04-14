using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controlador da tela de pontuacao ao final de uma fase.
/// </summary>
public class StageScoreUI : MonoBehaviour
{
    [Header("Textos")]
    [SerializeField] private TMP_Text stageNameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text bestScoreText;

    [Header("Botoes")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button retryButton;

    private void Start()
    {
        if (GameFlowManager.Instance == null) return;

        var stage = GameFlowManager.Instance.CurrentStage;
        int score = GameFlowManager.Instance.CurrentStageScore;
        float time = GameFlowManager.Instance.CurrentStageTime;

        if (stageNameText != null && stage != null)
            stageNameText.text = stage.stageName;
        if (scoreText != null)
            scoreText.text = $"{score}";
        if (timeText != null)
            timeText.text = FormatTime(time);
        if (rankText != null)
            rankText.text = CalculateRank(score);

        // Melhor pontuacao salva
        if (bestScoreText != null && SaveManager.Instance != null && stage != null)
        {
            var progress = SaveManager.Instance.GetStageProgress(stage.stageIndex);
            bestScoreText.text = progress != null ? $"Recorde: {progress.bestScore}" : "";
        }

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinue);
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetry);
    }

    private void OnContinue()
    {
        if (GameFlowManager.Instance == null) return;

        // Se era a ultima fase, vai pra cutscene final / creditos
        if (GameFlowManager.Instance.IsLastStage(GameFlowManager.Instance.CurrentStage))
            GameFlowManager.Instance.GoToCredits();
        else
            GameFlowManager.Instance.GoToStageSelect();
    }

    private void OnRetry()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToStage(GameFlowManager.Instance.CurrentStage);
    }

    private string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        int seconds = (int)(time % 60);
        return $"{minutes:00}:{seconds:00}";
    }

    private string CalculateRank(int score)
    {
        if (score >= 9000) return "S";
        if (score >= 7000) return "A";
        if (score >= 5000) return "B";
        if (score >= 3000) return "C";
        return "D";
    }
}
