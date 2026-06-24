using UnityEngine;
using UnityEngine.UI;

public class TutorialOverlayUI : MonoBehaviour
{
    [Header("Painel raiz (ativado/desativado pelo Show/Hide)")]
    [SerializeField] private GameObject root;

    [Header("Paginas (GameObjects ativados um por vez)")]
    [SerializeField] private GameObject[] pages;

    [Header("Botoes de navegacao")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button closeButton;

    private int currentPage;

    private void Awake()
    {
        if (root != null)
            root.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
        if (nextButton != null)
            nextButton.onClick.AddListener(NextPage);
        if (prevButton != null)
            prevButton.onClick.AddListener(PrevPage);
    }

    public void Show()
    {
        currentPage = 0;
        if (root != null)
            root.SetActive(true);
        RefreshPages();
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    private void NextPage()
    {
        if (pages == null || pages.Length == 0) return;
        currentPage = Mathf.Min(currentPage + 1, pages.Length - 1);
        RefreshPages();
    }

    private void PrevPage()
    {
        if (pages == null || pages.Length == 0) return;
        currentPage = Mathf.Max(currentPage - 1, 0);
        RefreshPages();
    }

    private void RefreshPages()
    {
        if (pages == null) return;
        for (int i = 0; i < pages.Length; i++)
            if (pages[i] != null) pages[i].SetActive(i == currentPage);

        if (prevButton != null) prevButton.interactable = currentPage > 0;
        if (nextButton != null) nextButton.interactable = pages != null && currentPage < pages.Length - 1;
    }
}
