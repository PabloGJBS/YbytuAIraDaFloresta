using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Globalization;

/// <summary>
/// Controlador da tela de pontuacao ao final de uma fase. Mostra pontos/tempo/rank
/// (do GameFlowManager), toca a musica de finalizacao (parando a BGM da fase) e, ao
/// continuar (botao ou Espaco), segue o fluxo. Ultima fase -> cutscene final.
/// Em teste isolado (sem GameFlowManager) carrega a OutroCutscene direto.
/// </summary>
public class StageScoreUI : MonoBehaviour
{
    [Header("Textos")]
    [SerializeField] private TMP_Text stageNameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text hitsText;
    [SerializeField] private TMP_Text enemiesText;
    [SerializeField] private TMP_Text bestScoreText;

    [Header("Continuar")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private GameObject continuePrompt;
    [SerializeField] private float minTimeBeforeContinue = 1.2f;

    [Header("Audio")]
    [SerializeField] private AudioSource music;

    private static readonly CultureInfo PtBr = new CultureInfo("pt-BR");
    private float elapsed;
    private bool advanced;

    private void Start()
    {
        // Para a BGM persistente da fase e toca a musica de finalizacao
        if (SoundManager.Instance != null) SoundManager.Instance.StopBGM();
        if (music != null) music.Play();

        var stage = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentStage : null;
        int score = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentStageScore : 0;
        float time = GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentStageTime : 0f;

        if (stageNameText != null) stageNameText.text = stage != null ? stage.stageName : "";
        if (scoreText != null) scoreText.text = score.ToString("N0", PtBr);
        if (timeText != null) timeText.text = FormatTime(time);
        if (rankText != null) rankText.text = CalculateRank(score);
        if (hitsText != null)
            hitsText.text = (GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentStageHits : 0).ToString();
        if (enemiesText != null)
            enemiesText.text = (GameFlowManager.Instance != null ? GameFlowManager.Instance.CurrentStageEnemies : 0).ToString();

        if (bestScoreText != null && SaveManager.Instance != null && stage != null)
        {
            var progress = SaveManager.Instance.GetStageProgress(stage.stageIndex);
            bestScoreText.text = progress != null ? $"Recorde: {progress.bestScore}" : "";
        }

        if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
        if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
        if (continuePrompt != null) continuePrompt.SetActive(false);
    }

    private void Update()
    {
        if (advanced) return;
        elapsed += Time.deltaTime;
        if (elapsed < minTimeBeforeContinue) return;

        if (continuePrompt != null && !continuePrompt.activeSelf)
            continuePrompt.SetActive(true);

        var kb = Keyboard.current;
        var gp = Gamepad.current;
        bool pressed = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                    || (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.startButton.wasPressedThisFrame));
        if (pressed) OnContinue();
    }

    private void OnContinue()
    {
        if (advanced) return;
        advanced = true;

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.OnStageScoreContinue();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("OutroCutscene"); // fallback (teste isolado)
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
