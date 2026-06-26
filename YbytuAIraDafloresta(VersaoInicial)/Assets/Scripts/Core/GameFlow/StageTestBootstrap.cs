using UnityEngine;
using UnityEngine.SceneManagement;

public class StageTestBootstrap : MonoBehaviour
{
    [Tooltip("StageData desta fase (opcional). Se vazio, cria um em runtime apontando pra esta cena.")]
    [SerializeField] private StageData stage;

    private void Awake()
    {
        if (GameFlowManager.Instance != null) return;

        var go = new GameObject("GameFlowManager (TEST)");
        var gfm = go.AddComponent<GameFlowManager>();

        var s = stage;
        if (s == null)
        {
            s = ScriptableObject.CreateInstance<StageData>();
            s.gameplaySceneName = SceneManager.GetActiveScene().name;
        }
        gfm.PrimeDirectPlay(s);
    }
}
