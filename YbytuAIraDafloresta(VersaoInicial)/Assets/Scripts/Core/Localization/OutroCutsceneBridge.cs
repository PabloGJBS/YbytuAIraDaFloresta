using UnityEngine;
using UnityEngine.SceneManagement;

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
            GameFlowManager.Instance.OnStageFinalizerEnd();
        else
            SceneManager.LoadScene("MainMenu");
    }
}
