using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Menu de pausa durante o gameplay.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;

    private bool isPaused;

    private void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void Update()
    {
        bool pausePressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                            (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

        if (pausePressed)
            TogglePause();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (pausePanel != null)
            pausePanel.SetActive(isPaused);

        if (GameFlowManager.Instance != null)
        {
            if (isPaused)
                GameFlowManager.Instance.PauseStage();
            else
                GameFlowManager.Instance.ResumeStage();
        }
    }

    public void OnResumeButton()
    {
        isPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.ResumeStage();
    }

    public void OnRetryButton()
    {
        Time.timeScale = 1f;
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToStage(GameFlowManager.Instance.CurrentStage);
    }

    public void OnQuitToMenuButton()
    {
        Time.timeScale = 1f;
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToMainMenu();
    }
}
