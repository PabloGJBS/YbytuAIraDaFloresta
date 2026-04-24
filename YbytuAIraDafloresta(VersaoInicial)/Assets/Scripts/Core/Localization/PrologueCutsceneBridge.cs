using UnityEngine;

/// <summary>
/// Quando o PrologueCutsceneController termina, dispara GameFlowManager.OnIntroCutsceneEnd().
/// Mantido separado pra deixar PrologueCutsceneController desacoplado do flow do jogo.
/// </summary>
[RequireComponent(typeof(PrologueCutsceneController))]
public class PrologueCutsceneBridge : MonoBehaviour
{
    [SerializeField] private PrologueCutsceneController prologue;

    private void Awake()
    {
        if (prologue == null) prologue = GetComponent<PrologueCutsceneController>();
    }

    private void OnEnable()
    {
        if (prologue != null)
            prologue.OnFinished += HandleFinished;
    }

    private void OnDisable()
    {
        if (prologue != null)
            prologue.OnFinished -= HandleFinished;
    }

    private void HandleFinished()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.OnIntroCutsceneEnd();
        else
            Debug.LogWarning("[PrologueCutsceneBridge] GameFlowManager.Instance nulo. Cutscene finalizada sem transicao.");
    }
}
