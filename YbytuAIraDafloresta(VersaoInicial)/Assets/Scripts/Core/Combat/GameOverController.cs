using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GameOverController : MonoBehaviour
{
    [Header("Refs (preenchidas pelo builder)")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button desistirButton;
    [SerializeField] private TMP_Text countdownText;

    [Header("Config")]
    [SerializeField] private float countdownSeconds = 10f;

    private PlayerCombatManager player;
    private bool shown;

    private void Start()
    {
        if (root != null) root.SetActive(false);

        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) player = go.GetComponent<PlayerCombatManager>();
        if (player != null) player.OnGameOver += Show;

        if (continueButton != null) continueButton.onClick.AddListener(OnContinuar);
        if (desistirButton != null) desistirButton.onClick.AddListener(OnDesistir);
    }

    private void OnDestroy()
    {
        if (player != null) player.OnGameOver -= Show;
    }

    public void Show()
    {
        if (shown) return;
        shown = true;

        var stage = FindAnyObjectByType<StageManager>();
        if (stage != null && GameFlowManager.Instance != null)
            GameFlowManager.Instance.SetStageCheckpoint(stage.CurrentZoneIndex);

        if (root != null) root.SetActive(true);
        Time.timeScale = 0f;

        int livesLeft = GameFlowManager.Instance != null ? GameFlowManager.Instance.PlayerLives
                      : (player != null && player.Lives != null ? player.Lives.CurrentLives : 1);
        bool canContinue = livesLeft > 0;

        if (continueButton != null) continueButton.gameObject.SetActive(canContinue);
        if (countdownText != null) countdownText.gameObject.SetActive(canContinue);

        if (canContinue) StartCoroutine(CountdownRoutine());
    }

    private void Update()
    {
        if (!shown) return;
        var kb = Keyboard.current;
        var gp = Gamepad.current;
        bool confirm = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                    || (gp != null && gp.buttonSouth.wasPressedThisFrame);
        bool cancel = (kb != null && kb.escapeKey.wasPressedThisFrame)
                   || (gp != null && gp.buttonEast.wasPressedThisFrame);
        bool canContinue = continueButton != null && continueButton.gameObject.activeSelf;

        if (confirm && canContinue) OnContinuar();
        else if (cancel || (confirm && !canContinue)) OnDesistir();
    }

    private IEnumerator CountdownRoutine()
    {
        float t = countdownSeconds;
        while (t > 0f)
        {
            if (countdownText != null) countdownText.text = Mathf.CeilToInt(t).ToString();
            t -= Time.unscaledDeltaTime;
            yield return null;
        }
        if (countdownText != null) countdownText.text = "0";
        if (continueButton != null) continueButton.gameObject.SetActive(false); // so resta Desistir
    }

    private void OnContinuar()
    {
        Time.timeScale = 1f;
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.CurrentStage != null)
            GameFlowManager.Instance.ContinueCurrentStage();
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name); // fallback: recarrega a cena
    }

    private void OnDesistir()
    {
        Time.timeScale = 1f;
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToMainMenu();
        else
            SceneManager.LoadScene("MainMenu");
    }
}
