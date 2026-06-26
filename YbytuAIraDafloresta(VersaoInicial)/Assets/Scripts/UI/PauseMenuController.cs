using UnityEngine;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private Button pauseButton;
    [SerializeField] private SettingsOverlayUI settingsOverlay;
    [Tooltip("Congela o tempo do jogo enquanto a tela esta aberta.")]
    [SerializeField] private bool freezeTimeScale = true;
    [Tooltip("Permite abrir/fechar a pausa com a tecla Esc.")]
    [SerializeField] private bool allowEscapeKey = true;

    private bool isPaused;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (pauseButton != null) pauseButton.onClick.AddListener(Open);
        if (settingsOverlay != null) settingsOverlay.OnHidden += HandleOverlayHidden;
    }

    private void OnDestroy()
    {
        if (pauseButton != null) pauseButton.onClick.RemoveListener(Open);
        if (settingsOverlay != null) settingsOverlay.OnHidden -= HandleOverlayHidden;

        if (isPaused && freezeTimeScale) Time.timeScale = 1f;
    }

    private void Update()
    {
        if (!allowEscapeKey) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) settingsOverlay?.Hide();
            else Open();
        }
    }

    public void Open()
    {
        if (settingsOverlay == null || isPaused) return;
        isPaused = true;
        if (freezeTimeScale)
        {
            previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
        }
        settingsOverlay.Show();
    }

    private void HandleOverlayHidden()
    {
        if (!isPaused) return;
        isPaused = false;
        if (freezeTimeScale) Time.timeScale = previousTimeScale;
    }
}
