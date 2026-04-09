using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

/// <summary>
/// Gerenciador central do fluxo de telas do jogo.
/// Singleton que persiste entre cenas.
///
/// Fluxo:
/// IntroCutscene -> MainMenu -> SaveSelect -> StageSelect -> Stage -> StageCutscene -> StageScore -> StageSelect
///                                                                                                   (repete)
/// Ultima fase: StageCutscene -> FinalCutscene -> Credits -> StageSelect
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    private const string DisclaimerAcceptedKey = "Ybytu_DisclaimerAccepted";

    private static GameFlowManager instance;
    public static GameFlowManager Instance => instance;

    [Header("Cenas")]
    [SerializeField] private string disclaimerScene = "Disclaimer";
    [SerializeField] private string introCutsceneScene = "IntroCutscene";
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string saveSelectScene = "SaveSelect";
    [SerializeField] private string stageSelectScene = "StageSelect";
    [SerializeField] private string scoreScene = "StageScore";
    [SerializeField] private string creditsScene = "Credits";

    public enum SaveSelectMode { NewGame, Continue }
    public SaveSelectMode PendingSaveSelectMode { get; private set; } = SaveSelectMode.NewGame;

    [Header("Fases")]
    [SerializeField] private StageData[] stages;

    private GameState currentState;
    private StageData currentStage;
    private int currentStageScore;
    private float currentStageTime;
    private bool pendingOutroCutscene;

    public GameState CurrentState => currentState;
    public StageData CurrentStage => currentStage;
    public StageData[] Stages => stages;
    public int CurrentStageScore => currentStageScore;
    public float CurrentStageTime => currentStageTime;

    public event Action<GameState> OnStateChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (HasAcceptedDisclaimer())
            GoToMainMenu();
        else
            GoToDisclaimer();
    }

    public bool HasAcceptedDisclaimer()
    {
        return PlayerPrefs.GetInt(DisclaimerAcceptedKey, 0) == 1;
    }

    public void MarkDisclaimerAccepted()
    {
        PlayerPrefs.SetInt(DisclaimerAcceptedKey, 1);
        PlayerPrefs.Save();
    }

    private void ChangeState(GameState newState)
    {
        currentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    // --- Navegacao entre telas ---

    public void GoToDisclaimer()
    {
        ChangeState(GameState.Disclaimer);
        LoadScene(disclaimerScene);
    }

    public void OnDisclaimerEnd()
    {
        MarkDisclaimerAccepted();
        GoToMainMenu();
    }

    public void GoToIntroCutscene()
    {
        ChangeState(GameState.IntroCutscene);
        LoadScene(introCutsceneScene);
    }

    public void GoToMainMenu()
    {
        ChangeState(GameState.MainMenu);
        LoadScene(mainMenuScene);
    }

    public void GoToSaveSelect()
    {
        ChangeState(GameState.SaveSelect);
        LoadScene(saveSelectScene);
    }

    /// <summary>
    /// Jogar (Novo Jogo): mostra a cutscene do prologo e em seguida joga o player na primeira fase.
    /// </summary>
    public void StartNewGame()
    {
        PendingSaveSelectMode = SaveSelectMode.NewGame;
        GoToIntroCutscene();
    }

    /// <summary>
    /// Continuar: 0 saves = nada, 1 save = carrega direto, 2+ saves = SaveSelect em modo Continue.
    /// </summary>
    public void ContinueGame()
    {
        if (SaveManager.Instance == null) return;

        int count = SaveManager.Instance.GetSaveCount();
        if (count == 0) return;

        if (count == 1)
        {
            int slot = SaveManager.Instance.GetMostRecentSlot();
            if (slot < 0) return;

            SaveManager.Instance.SelectSave(slot);
            if (SaveManager.Instance.CurrentSave != null && !SaveManager.Instance.CurrentSave.introWatched)
            {
                GoToIntroCutscene();
                return;
            }
            GoToStageSelect();
            return;
        }

        PendingSaveSelectMode = SaveSelectMode.Continue;
        GoToSaveSelect();
    }

    public void GoToStageSelect()
    {
        ChangeState(GameState.StageSelect);
        LoadScene(stageSelectScene);
    }

    public void GoToStage(StageData stage)
    {
        currentStage = stage;
        currentStageScore = 0;
        currentStageTime = 0f;

        // Se a fase tem cutscene de intro, mostra primeiro
        if (!string.IsNullOrEmpty(stage.introCutsceneId))
        {
            pendingOutroCutscene = false;
            ChangeState(GameState.StageCutscene);
            LoadScene(stage.introCutsceneId);
        }
        else
        {
            StartStageGameplay();
        }
    }

    public void StartStageGameplay()
    {
        if (currentStage == null) return;
        ChangeState(GameState.StagePlaying);
        LoadScene(currentStage.gameplaySceneName);
    }

    public void PauseStage()
    {
        ChangeState(GameState.StagePaused);
        Time.timeScale = 0f;
    }

    public void ResumeStage()
    {
        ChangeState(GameState.StagePlaying);
        Time.timeScale = 1f;
    }

    public void CompleteStage(int score, float time)
    {
        currentStageScore = score;
        currentStageTime = time;
        Time.timeScale = 1f;

        // Salvar resultado
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveStageResult(currentStage.stageIndex, score, time);

        // Se tem cutscene de final da fase, mostra
        if (!string.IsNullOrEmpty(currentStage.outroCutsceneId))
        {
            pendingOutroCutscene = true;
            ChangeState(GameState.StageCutscene);
            LoadScene(currentStage.outroCutsceneId);
        }
        else
        {
            GoToStageScore();
        }
    }

    public void GoToStageScore()
    {
        ChangeState(GameState.StageScore);
        LoadScene(scoreScene);
    }

    public void GoToFinalCutscene()
    {
        ChangeState(GameState.FinalCutscene);
        // A cena de cutscene final pode ser definida na ultima fase
    }

    public void GoToCredits()
    {
        ChangeState(GameState.Credits);
        LoadScene(creditsScene);
    }

    // Chamado ao terminar a cutscene de intro ou de fim da fase
    public void OnStageCutsceneEnd()
    {
        if (currentState != GameState.StageCutscene) return;

        // Flag explicita evita misroteamento de uma fase concluida com score 0
        if (pendingOutroCutscene)
            GoToStageScore();
        else
            StartStageGameplay();
    }

    // Chamado ao terminar a intro do jogo: vai direto pra primeira fase
    public void OnIntroCutsceneEnd()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.CurrentSave != null)
            SaveManager.Instance.MarkIntroWatched();

        if (stages != null && stages.Length > 0 && !string.IsNullOrEmpty(stages[0].gameplaySceneName))
        {
            GoToStage(stages[0]);
            return;
        }

        // Fallback enquanto nao ha StageData configurada
        LoadScene("SampleScene");
    }

    // Verificar se deve pular a intro
    public bool ShouldSkipIntro()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.CurrentSave == null)
            return false;
        return SaveManager.Instance.CurrentSave.introWatched;
    }

    // Verificar se uma fase esta desbloqueada
    public bool IsStageUnlocked(StageData stage)
    {
        if (stage.unlockedByDefault) return true;
        if (stage.requiredStage == null) return true;
        if (SaveManager.Instance == null || SaveManager.Instance.CurrentSave == null) return false;

        var progress = SaveManager.Instance.GetStageProgress(stage.requiredStage.stageIndex);
        return progress != null && progress.completed;
    }

    // Verificar se eh a ultima fase
    public bool IsLastStage(StageData stage)
    {
        return stages != null && stages.Length > 0 && stage.stageIndex == stages[stages.Length - 1].stageIndex;
    }

    private void LoadScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
    }
}
