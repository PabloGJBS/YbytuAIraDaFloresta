using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tela de disclaimer exibida na primeira execucao do jogo.
/// Informa que eh um projeto de TCC e que a historia/personagens sao ficticios.
/// Avanca automaticamente apos o tempo minimo quando o jogador pressiona qualquer tecla.
/// </summary>
public class DisclaimerUI : MonoBehaviour
{
    [Header("Tempo minimo antes de permitir skip (segundos)")]
    [SerializeField] private float minDisplayTime = 3f;

    [Header("Auto-avanco apos este tempo (0 = desabilitado)")]
    [SerializeField] private float autoAdvanceTime = 8f;

    private float elapsedTime;
    private bool advanced;

    private void Update()
    {
        if (advanced) return;
        elapsedTime += Time.deltaTime;

        if (elapsedTime < minDisplayTime) return;

        bool anyKey = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool anyGamepad = Gamepad.current != null && (Gamepad.current.startButton.wasPressedThisFrame || Gamepad.current.aButton.wasPressedThisFrame);
        bool anyMouse = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool autoTimeout = autoAdvanceTime > 0 && elapsedTime >= autoAdvanceTime;

        if (anyKey || anyGamepad || anyMouse || autoTimeout)
            Advance();
    }

    public void Advance()
    {
        if (advanced) return;
        advanced = true;

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.OnDisclaimerEnd();
    }
}
