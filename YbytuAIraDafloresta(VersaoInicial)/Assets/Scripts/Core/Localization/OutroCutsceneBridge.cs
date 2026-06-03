using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quando o PrologueCutsceneController (reusado na cutscene de encerramento) termina,
/// volta ao Menu Principal. Diferente do PrologueCutsceneBridge, que inicia a fase.
/// </summary>
[RequireComponent(typeof(PrologueCutsceneController))]
public class OutroCutsceneBridge : MonoBehaviour
{
    [SerializeField] private PrologueCutsceneController cutscene;

    private void Awake()
    {
        if (cutscene == null) cutscene = GetComponent<PrologueCutsceneController>();
    }

    private void OnEnable()
    {
        if (cutscene != null)
            cutscene.OnFinished += HandleFinished;
    }

    private void OnDisable()
    {
        if (cutscene != null)
            cutscene.OnFinished -= HandleFinished;
    }

    private void HandleFinished()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToMainMenu();
        else
            SceneManager.LoadScene("MainMenu"); // fallback (ex.: testando a cena isolada)
    }
}
