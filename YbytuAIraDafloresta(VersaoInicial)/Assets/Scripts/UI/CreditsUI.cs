using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador da tela de creditos.
/// Ao finalizar ou skipar, volta para selecao de fases.
/// </summary>
public class CreditsUI : MonoBehaviour
{
    [SerializeField] private float autoReturnTime = 30f;

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;

        // Voltar ao menu com qualquer botao ou apos tempo
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
