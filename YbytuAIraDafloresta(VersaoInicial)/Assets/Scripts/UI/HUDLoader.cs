using UnityEngine;
using UnityEngine.SceneManagement;

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
