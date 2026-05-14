using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Painel overlay de configuracoes exibido sobre a cena atual (ex: MainMenu).
/// Scaffold inicial: Show/Hide e botao de fechar. Sliders/dropdowns serao adicionados depois.
/// </summary>
public class SettingsOverlayUI : MonoBehaviour
{
    [Header("Painel raiz (ativado/desativado pelo Show/Hide)")]
    [SerializeField] private GameObject root;

    [Header("Botao de fechar")]
    [SerializeField] private Button closeButton;

    // TODO: sliders de volume, dropdown de idioma, toggles graficos

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    public void Show()
    {
        if (root != null)
            root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }
}
