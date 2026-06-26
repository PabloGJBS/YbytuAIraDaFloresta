using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class IntroCutsceneUI : MonoBehaviour
{
    [SerializeField] private GameObject skipPrompt;

    [Header("Botao temporario (scaffold) - funciona mesmo sem introWatched")]
    [SerializeField] private Button skipButton;

    private bool canSkip;

    private void Start()
    {
        canSkip = GameFlowManager.Instance != null && GameFlowManager.Instance.ShouldSkipIntro();

        if (skipPrompt != null)
            skipPrompt.SetActive(canSkip);

        if (skipButton != null)
            skipButton.onClick.AddListener(SkipCutscene);
    }

    private void Update()
    {
        if (canSkip && (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame ||
            Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame))
        {
            SkipCutscene();
        }
    }

    public void OnCutsceneFinished()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.OnIntroCutsceneEnd();
    }

    public void SkipCutscene()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.OnIntroCutsceneEnd();
    }
}
