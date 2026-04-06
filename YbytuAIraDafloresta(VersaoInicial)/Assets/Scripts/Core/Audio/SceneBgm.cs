using UnityEngine;

/// <summary>
/// Define qual BGM toca quando a cena carrega.
/// Adicione um GameObject com este componente em cada cena que tem trilha sonora.
/// O SoundManager faz o crossfade automaticamente se ja estiver tocando outra BGM.
/// </summary>
public class SceneBgm : MonoBehaviour
{
    [SerializeField] private AudioClip clip;
    [SerializeField] private bool loop = true;
    [Tooltip("Tocar automaticamente no Start.")]
    [SerializeField] private bool autoPlay = true;

    private void Start()
    {
        if (!autoPlay || clip == null) return;
        var manager = SoundManager.Instance;
        if (manager != null)
            manager.PlayBGM(clip, loop);
    }

    public void Play()
    {
        if (clip == null) return;
        var manager = SoundManager.Instance;
        if (manager != null)
            manager.PlayBGM(clip, loop);
    }
}
