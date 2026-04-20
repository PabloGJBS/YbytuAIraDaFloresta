using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Carrega a cena HUD em modo additive sobre a fase atual.
/// Adicione um GameObject com este componente em cada cena de gameplay.
/// Idempotente: nao recarrega se a HUD ja estiver presente.
/// </summary>
public class HUDLoader : MonoBehaviour
{
    [SerializeField] private string hudSceneName = "HUD";

    private void Awake()
    {
        var s = SceneManager.GetSceneByName(hudSceneName);
        if (!s.isLoaded)
            SceneManager.LoadScene(hudSceneName, LoadSceneMode.Additive);
    }
}
