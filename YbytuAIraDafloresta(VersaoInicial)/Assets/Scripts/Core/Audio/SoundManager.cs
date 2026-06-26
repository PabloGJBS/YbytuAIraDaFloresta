using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

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
        ApplySavedVolumes();
        HookButtonSounds();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => HookButtonSounds();

    private void HookButtonSounds()
    {
        if (library == null || library.uiConfirm == null) return;
        var buttons = FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None);
        foreach (var b in buttons)
        {
            if (b == null) continue;
            b.onClick.RemoveListener(PlayUiClick);
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

    public void PlaySFX(AudioClip clip, float volume = 1f, float pitchVariation = 0.06f)
    {
        if (clip == null) return;
        var src = GetFreeSfxSource();
        src.clip = clip;
        src.volume = volume * SfxVolume01;
        src.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        src.Play();
    }

    private AudioSource GetFreeSfxSource()
    {
        for (int i = 0; i < sfxPool.Length; i++)
            if (!sfxPool[i].isPlaying) return sfxPool[i];
        return sfxPool[0]; // fallback: rouba o canal 0
    }

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
        SaveCurrentBgmPosition();

        if (bgmSource.isPlaying)
            yield return FadeOutBGM();

        bgmSource.clip = newClip;
        bgmSource.loop = loop;
        if (newClip == null) yield break;

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
            bgmSource.volume = Mathf.Clamp01(t / bgmFadeDuration) * BgmTargetVolume;
            yield return null;
        }
        bgmSource.volume = BgmTargetVolume;
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

    private const string MusicVolKey = "Ybytu_MusicVolume";
    private const string SfxVolKey = "Ybytu_SfxVolume";

    public float MusicVolume01 => Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolKey, 1f));
    public float SfxVolume01 => Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolKey, 1f));

    private float BgmTargetVolume => bgmVolume * MusicVolume01;

    private void ApplySavedVolumes()
    {
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.volume = BgmTargetVolume;
    }

    public void SetMasterVolume(float linear01) => SetMixerVolume("MasterVolume", linear01);

    public void SetMusicVolume(float linear01)
    {
        linear01 = Mathf.Clamp01(linear01);
        PlayerPrefs.SetFloat(MusicVolKey, linear01);
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.volume = BgmTargetVolume;
    }

    public void SetSfxVolume(float linear01)
    {
        linear01 = Mathf.Clamp01(linear01);
        PlayerPrefs.SetFloat(SfxVolKey, linear01);
    }

    private void SetMixerVolume(string parameter, float linear01)
    {
        if (mixer == null) return;
        float db = linear01 <= 0.0001f ? -80f : Mathf.Log10(linear01) * 20f;
        mixer.SetFloat(parameter, db);
    }
}
