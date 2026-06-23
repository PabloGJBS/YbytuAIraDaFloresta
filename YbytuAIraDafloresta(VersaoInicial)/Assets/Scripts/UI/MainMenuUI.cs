using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Botoes")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button tutorialButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;

    [Header("Overlays")]
    [SerializeField] private SettingsOverlayUI settingsOverlay;
    [SerializeField] private TutorialOverlayUI tutorialOverlay;

    private void Start()
    {
        UpdateContinueButtonState();

        if (playButton != null) playButton.onClick.AddListener(OnPlayButton);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueButton);
        if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsButton);
        if (tutorialButton != null) tutorialButton.onClick.AddListener(OnTutorialButton);
        if (creditsButton != null) creditsButton.onClick.AddListener(OnCreditsButton);
        if (quitButton != null) quitButton.onClick.AddListener(OnQuitButton);
    }

    private void UpdateContinueButtonState()
    {
        if (continueButton == null) return;
        bool hasAnySave = SaveManager.Instance != null && SaveManager.Instance.HasAnySave();
        continueButton.interactable = hasAnySave;
    }

    public void OnPlayButton()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.StartNewGame();
    }

    public void OnContinueButton()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.ContinueGame();
    }

    public void OnSettingsButton()
    {
        if (settingsOverlay != null)
            settingsOverlay.Show();
    }

    public void OnTutorialButton()
    {
        if (tutorialOverlay != null)
            tutorialOverlay.Show();
    }

    public void OnCreditsButton()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToCredits();
    }

    public void OnQuitButton()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
