using UnityEngine;
using UnityEngine.InputSystem;

public class CreditsUI : MonoBehaviour
{
    [SerializeField] private float autoReturnTime = 30f;

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;

        bool inputPressed = (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
                            (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

        if (inputPressed || timer >= autoReturnTime)
            ReturnToStageSelect();
    }

    public void OnCreditsFinished()
    {
        ReturnToStageSelect();
    }

    private void ReturnToStageSelect()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToStageSelect();
    }
}
