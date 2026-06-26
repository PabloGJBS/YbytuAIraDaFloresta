using UnityEngine;
using TMPro;

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
