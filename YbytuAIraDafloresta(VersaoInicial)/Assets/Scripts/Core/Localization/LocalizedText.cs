using UnityEngine;
using TMPro;

/// <summary>
/// Componente que traduz automaticamente um TMP_Text baseado na chave de localizacao.
/// Adicionar ao mesmo GameObject que tem o TMP_Text.
/// Quando o idioma muda, o texto atualiza automaticamente.
///
/// Uso:
///   1. Adicionar este componente ao GameObject com TMP_Text
///   2. Setar a chave no Inspector (ex: "ui.main_menu.play")
///   3. O texto sera traduzido automaticamente
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string localizationKey;

    private TMP_Text textComponent;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        UpdateText();

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
    }

    private void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(string newLanguage)
    {
        UpdateText();
    }

    public void SetKey(string key)
    {
        localizationKey = key;
        UpdateText();
    }

    private void UpdateText()
    {
        if (textComponent == null || LocalizationManager.Instance == null) return;
        if (string.IsNullOrEmpty(localizationKey)) return;

        textComponent.text = LocalizationManager.Instance.GetText(localizationKey);
    }
}
