using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

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
    [SerializeField] private string finalCutsceneScene = "CutsceneFinalizadoraFase1";

    public enum SaveSelectMode { NewGame, Continue }
    public SaveSelectMode PendingSaveSelectMode { get; private set; } = SaveSelectMode.NewGame;

    public int StageCheckpointZone { get; private set; }
    public bool HasStageCheckpoint { get; private set; }

    public void SetStageCheckpoint(int zoneIndex)
    {
        StageCheckpointZone = Mathf.Max(0, zoneIndex);
        HasStageCheckpoint = true;
    }

    public void ClearStageCheckpoint()
    {
        StageCheckpointZone = 0;
        HasStageCheckpoint = false;
    }

    [Header("Fases")]
    [SerializeField] private StageData[] stages;
    [SerializeField] private int startingLives = 3;

    private GameState currentState;
    private StageData currentStage;
    private int currentStageScore;
    private int carryOverScore;
    private float currentStageTime;
    private int currentStageHits;
    private int currentStageEnemies;
    private bool pendingOutroCutscene;
    private int playerLives;

    public GameState CurrentState => currentState;
    public StageData CurrentStage => currentStage;
    public StageData[] Stages => stages;
    public int CurrentStageScore => currentStageScore;
    public int CarryOverScore => carryOverScore;
    public float CurrentStageTime => currentStageTime;
    public int PlayerLives => playerLives;
    public int CurrentStageHits => currentStageHits;
    public int CurrentStageEnemies => currentStageEnemies;

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
        playerLives = startingLives;
    }

    private void Start()
    {
        ChangeState(GameState.Disclaimer);
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

    public void StartNewGame()
    {
        PendingSaveSelectMode = SaveSelectMode.NewGame;
        ResetPlayerLives();
        carryOverScore = 0;
        if (SaveManager.Instance != null) SaveManager.Instance.CreateNewSave(SaveManager.SingleSlot);
        GoToIntroCutscene();
    }

    public void DecrementLife()
    {
        if (playerLives > 0) playerLives--;
    }

    public void PrimeDirectPlay(StageData stage)
    {
        if (currentStage == null) currentStage = stage;
    }

    public void ResetPlayerLives()
    {
        playerLives = startingLives;
    }

    public void ContinueGame()
    {
        if (SaveManager.Instance == null) return;
        var save = SaveManager.Instance.LoadSave(SaveManager.SingleSlot);
        if (save == null) return; // sem save: nada a continuar

        carryOverScore = save.carryOverScore;
        playerLives = Mathf.Max(1, save.livesRemaining);

        if (!save.introWatched) { GoToIntroCutscene(); return; }

        var stage = FindStageByIndex(save.lastStageIndex);
        if (stage == null) { GoToMainMenu(); return; }
        ResumeStageAtZone(stage, save.lastZoneIndex);
    }

    private void ResumeStageAtZone(StageData stage, int zoneIndex)
    {
        currentStage = stage;
        currentStageScore = 0;
        currentStageTime = 0f;
        if (zoneIndex > 0) SetStageCheckpoint(zoneIndex);
        else ClearStageCheckpoint();
        ChangeState(GameState.StagePlaying);
        LoadScene(stage.gameplaySceneName);
    }

    private StageData FindStageByIndex(int index)
    {
        if (stages == null) return null;
        for (int i = 0; i < stages.Length; i++)
            if (stages[i] != null && stages[i].stageIndex == index) return stages[i];
        return null;
    }

    public void GoToStageSelect()
    {
        ChangeState(GameState.StageSelect);
        LoadScene(stageSelectScene);
    }

    public void GoToStage(StageData stage)
    {
        ClearStageCheckpoint();
        currentStage = stage;
        currentStageScore = 0;
        currentStageTime = 0f;

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

    public void ContinueCurrentStage()
    {
        if (currentStage == null) return;
        DecrementLife();
        currentStageScore = 0;
        currentStageTime = 0f;
        Time.timeScale = 1f;
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

    public void CompleteStage(int score, float time, int hits = 0, int enemies = 0)
    {
        ClearStageCheckpoint();
        currentStageScore = score;
        carryOverScore = score;
        currentStageTime = time;
        currentStageHits = hits;
        currentStageEnemies = enemies;
        Time.timeScale = 1f;

        // Salvar resultado
        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveStageResult(currentStage.stageIndex, score, time);

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
        LoadScene(finalCutsceneScene);
    }

    public void OnStageScoreContinue()
    {
        if (currentStage != null && !string.IsNullOrEmpty(currentStage.finalizerCutsceneId))
        {
            ChangeState(GameState.FinalCutscene);
            LoadScene(currentStage.finalizerCutsceneId);
        }
        else
        {
            AdvanceAfterStage();
        }
    }

    public void OnStageFinalizerEnd()
    {
        AdvanceAfterStage();
    }

    private void AdvanceAfterStage()
    {
        var next = GetNextStage(currentStage);
        if (next != null)
            GoToStage(next);
        else
            GoToMainMenu();
    }

    private StageData GetNextStage(StageData stage)
    {
        if (stages == null || stage == null) return null;
        for (int i = 0; i < stages.Length; i++)
        {
            if (stages[i] != null && stages[i].stageIndex == stage.stageIndex)
                return (i + 1 < stages.Length) ? stages[i + 1] : null;
        }
        return null;
    }

    private StageData GetResumeStage()
    {
        if (stages == null || stages.Length == 0) return null;
        if (SaveManager.Instance != null && SaveManager.Instance.CurrentSave != null)
        {
            for (int i = 0; i < stages.Length; i++)
            {
                if (stages[i] == null) continue;
                var prog = SaveManager.Instance.GetStageProgress(stages[i].stageIndex);
                if (prog == null || !prog.completed) return stages[i];
            }
        }
        return stages[stages.Length - 1];
    }

    public void GoToCredits()
    {
        ChangeState(GameState.Credits);
        LoadScene(creditsScene);
    }

    public void OnStageCutsceneEnd()
    {
        if (currentState != GameState.StageCutscene) return;

        if (pendingOutroCutscene)
            GoToStageScore();
        else
            StartStageGameplay();
    }

    public void OnIntroCutsceneEnd()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.CurrentSave != null)
            SaveManager.Instance.MarkIntroWatched();

        if (stages != null && stages.Length > 0 && !string.IsNullOrEmpty(stages[0].gameplaySceneName))
        {
            GoToStage(stages[0]);
            return;
        }

        LoadScene("SampleScene");
    }

    public bool ShouldSkipIntro()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.CurrentSave == null)
            return false;
        return SaveManager.Instance.CurrentSave.introWatched;
    }

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
