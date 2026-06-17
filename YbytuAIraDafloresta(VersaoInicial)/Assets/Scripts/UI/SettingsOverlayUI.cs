using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsOverlayUI : MonoBehaviour
{
    [Header("Painel raiz (ativado/desativado pelo Show/Hide)")]
    [SerializeField] private GameObject root;

    [Header("Botao de fechar")]
    [SerializeField] private Button closeButton;

    [Header("Volume (0..1)")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Idioma")]
    [SerializeField] private Button languageButton;
    [SerializeField] private TMP_Text languageLabel;

    private const string PtBr = "pt-BR";
    private const string EnUs = "en-US";

    public event System.Action OnHidden;

    public bool IsOpen => root != null && root.activeSelf;

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        if (languageButton != null) languageButton.onClick.AddListener(ToggleLanguage);
    }

    public void Show()
    {
        var sm = SoundManager.Instance;
        if (sm != null)
        {
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(sm.MusicVolume01);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(sm.SfxVolume01);
        }

        UpdateLanguageLabel();

        if (root != null)
            root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
        PlayerPrefs.Save();
        OnHidden?.Invoke();
    }

    private void OnMusicChanged(float v)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SetMusicVolume(v);
    }

    private void OnSfxChanged(float v)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SetSfxVolume(v);
    }

    private void ToggleLanguage()
    {
        string current = CurrentLanguage();
        string next = current == PtBr ? EnUs : PtBr;

        var lm = LocalizationManager.Instance;
        if (lm != null)
        {
            lm.SetLanguage(next);
        }
        else
        {
            PlayerPrefs.SetString("game_language", next);
            PlayerPrefs.Save();
        }

        UpdateLanguageLabel();
    }

    private void UpdateLanguageLabel()
    {
        if (languageLabel == null) return;
        languageLabel.text = CurrentLanguage() == PtBr ? "Idioma: Português" : "Language: English";
    }

    private string CurrentLanguage()
    {
        var lm = LocalizationManager.Instance;
        if (lm != null && !string.IsNullOrEmpty(lm.CurrentLanguage)) return lm.CurrentLanguage;
        return PlayerPrefs.GetString("game_language", PtBr);
    }
}
