using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Singleton de audio. Carrega o SoundLibrary, mantem 1 AudioSource para BGM
/// e um pool de AudioSources para SFX. Roteado via AudioMixer (Master/Music/SFX).
/// Persiste entre cenas (DontDestroyOnLoad).
///
/// Uso:
///   SoundManager.Instance.PlaySFX(SoundManager.Library.playerPunch);
///   SoundManager.Instance.PlayBGM(myMusic);
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Configuracao")]
    [SerializeField] private SoundLibrary library;
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Pool de SFX")]
    [SerializeField] private int sfxPoolSize = 8;

    [Header("BGM")]
    [SerializeField] private float bgmFadeDuration = 1f;
    [Tooltip("Volume geral da musica (0..1). 0.7 = 70%.")]
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.7f;

    private AudioSource bgmSource;
    private AudioSource[] sfxPool;

    // Posicao (em segundos) de cada faixa de BGM, pra retomar de onde parou ao voltar.
    private readonly Dictionary<AudioClip, float> bgmPositions = new Dictionary<AudioClip, float>();

    public SoundLibrary Library => library;
    public AudioMixerGroup SfxGroup => sfxGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupSources();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // A cena onde o SoundManager nasce ja carregou antes do sceneLoaded; liga os botoes dela aqui.
        HookButtonSounds();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => HookButtonSounds();

    // Liga o som de clique (uiConfirm) em todos os Button da cena. Cobre botoes estaticos;
    // botoes instanciados em runtime (ex.: slots de save) precisam chamar PlayUiClick manualmente.
    private void HookButtonSounds()
    {
        if (library == null || library.uiConfirm == null) return;
        var buttons = FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None);
        foreach (var b in buttons)
        {
            if (b == null) continue;
            b.onClick.RemoveListener(PlayUiClick); // evita duplicar em botoes persistentes
            b.onClick.AddListener(PlayUiClick);
        }
    }

    public void PlayUiClick()
    {
        if (library != null && library.uiConfirm != null)
            PlaySFX(library.uiConfirm, 1f, 0f);
    }

    private void SetupSources()
    {
        var bgmGo = new GameObject("BGMSource");
        bgmGo.transform.SetParent(transform);
        bgmSource = bgmGo.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.playOnAwake = false;
        bgmSource.outputAudioMixerGroup = musicGroup;
        bgmSource.volume = 1f;

        sfxPool = new AudioSource[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
        {
            var go = new GameObject($"SFXSource_{i}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.loop = false;
            src.playOnAwake = false;
            src.outputAudioMixerGroup = sfxGroup;
            sfxPool[i] = src;
        }
    }

    /// <summary>
    /// Toca um SFX usando o pool. Pitch tem variacao aleatoria para evitar repeticao mecanica.
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f, float pitchVariation = 0.06f)
    {
        if (clip == null) return;
        var src = GetFreeSfxSource();
        src.clip = clip;
        src.volume = volume;
        src.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        src.Play();
    }

    private AudioSource GetFreeSfxSource()
    {
        for (int i = 0; i < sfxPool.Length; i++)
            if (!sfxPool[i].isPlaying) return sfxPool[i];
        return sfxPool[0]; // fallback: rouba o canal 0
    }

    /// <summary>
    /// Troca a BGM com crossfade. Se ja estiver tocando o mesmo clip, ignora.
    /// </summary>
    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;
        StopAllCoroutines();
        StartCoroutine(SwapBGM(clip, loop));
    }

    public void StopBGM()
    {
        SaveCurrentBgmPosition();
        StopAllCoroutines();
        StartCoroutine(FadeOutBGM());
    }

    private void SaveCurrentBgmPosition()
    {
        if (bgmSource != null && bgmSource.clip != null && bgmSource.isPlaying)
            bgmPositions[bgmSource.clip] = bgmSource.time;
    }

    private IEnumerator SwapBGM(AudioClip newClip, bool loop)
    {
        // Salva onde a faixa atual parou antes de troca-la.
        SaveCurrentBgmPosition();

        if (bgmSource.isPlaying)
            yield return FadeOutBGM();

        bgmSource.clip = newClip;
        bgmSource.loop = loop;
        if (newClip == null) yield break;

        // Retoma de onde parou (se essa faixa ja tocou antes); senao do inicio.
        float resumeAt = 0f;
        if (bgmPositions.TryGetValue(newClip, out float saved))
            resumeAt = Mathf.Clamp(saved, 0f, Mathf.Max(0f, newClip.length - 0.1f));

        bgmSource.volume = 0f;
        bgmSource.time = resumeAt;
        bgmSource.Play();
        float t = 0f;
        while (t < bgmFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Clamp01(t / bgmFadeDuration) * bgmVolume;
            yield return null;
        }
        bgmSource.volume = bgmVolume;
    }

    private IEnumerator FadeOutBGM()
    {
        float startVol = bgmSource.volume;
        float t = 0f;
        while (t < bgmFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            bgmSource.volume = Mathf.Lerp(startVol, 0f, t / bgmFadeDuration);
            yield return null;
        }
        bgmSource.Stop();
        bgmSource.volume = 0f;
    }

    // --- Volumes em dB (-80 a 0) ---

    public void SetMasterVolume(float linear01) => SetMixerVolume("MasterVolume", linear01);
    public void SetMusicVolume(float linear01)  => SetMixerVolume("MusicVolume", linear01);
    public void SetSfxVolume(float linear01)    => SetMixerVolume("SfxVolume", linear01);

    private void SetMixerVolume(string parameter, float linear01)
    {
        if (mixer == null) return;
        float db = linear01 <= 0.0001f ? -80f : Mathf.Log10(linear01) * 20f;
        mixer.SetFloat(parameter, db);
    }
}
