using UnityEngine;
using UnityEngine.InputSystem;

public class StageCutsceneUI : MonoBehaviour
{
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private GameObject skipPrompt;

    private void Start()
    {
        if (skipPrompt != null)
            skipPrompt.SetActive(allowSkip);
    }

    private void Update()
    {
        if (allowSkip &&
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame ||
             Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame))
        {
            SkipCutscene();
        }
    }

    public void OnCutsceneFinished()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.OnStageCutsceneEnd();
    }

    public void SkipCutscene()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.OnStageCutsceneEnd();
    }
}
