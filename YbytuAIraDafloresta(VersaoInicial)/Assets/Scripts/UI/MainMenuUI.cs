using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controlador da tela de menu principal.
/// Os handlers OnXxxButton() sao conectados aos botoes via Inspector (onClick).
/// </summary>
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

        // Os handlers tambem podem ser conectados via Inspector (onClick).
        // Fazemos bind em runtime como backup/conveniencia.
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
        bool hasAnySave = SaveManager.Instance != null && SaveManager.Instance.GetSaveCount() > 0;
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
