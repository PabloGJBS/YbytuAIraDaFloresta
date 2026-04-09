using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Transicao de tela com fade in/out.
/// Singleton que persiste entre cenas.
/// Usar: SceneTransition.Instance.FadeOut(() => { carregarCena(); });
/// </summary>
public class SceneTransition : MonoBehaviour
{
    private static SceneTransition instance;
    public static SceneTransition Instance => instance;

    [Header("Configuracao")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private Color fadeColor = Color.black;

    private Canvas canvas;
    private Image fadeImage;
    private bool isFading;

    public bool IsFading => isFading;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        SetupCanvas();
        // Comecar transparente
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        fadeImage.raycastTarget = false;
    }

    private void SetupCanvas()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        gameObject.AddComponent<CanvasScaler>();

        var imageGO = new GameObject("FadeImage");
        imageGO.transform.SetParent(transform, false);
        fadeImage = imageGO.AddComponent<Image>();

        var rt = imageGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// Fade para preto, executar acao, fade de volta.
    /// </summary>
    public void FadeOutIn(System.Action onMiddle)
    {
        if (isFading) return;
        StartCoroutine(FadeOutInCoroutine(onMiddle));
    }

    /// <summary>
    /// Apenas fade para preto (com callback).
    /// </summary>
    public void FadeOut(System.Action onComplete = null)
    {
        if (isFading) return;
        StartCoroutine(FadeCoroutine(0f, 1f, onComplete));
    }

    /// <summary>
    /// Apenas fade de preto para transparente.
    /// </summary>
    public void FadeIn(System.Action onComplete = null)
    {
        if (isFading) return;
        StartCoroutine(FadeCoroutine(1f, 0f, onComplete));
    }

    private IEnumerator FadeOutInCoroutine(System.Action onMiddle)
    {
        // Fade out
        yield return FadeCoroutine(0f, 1f, null);

        // Executar acao no meio (carregar cena, etc)
        onMiddle?.Invoke();

        // Esperar um frame para a cena carregar
        yield return null;

        // Fade in
        yield return FadeCoroutine(1f, 0f, null);
    }

    private IEnumerator FadeCoroutine(float from, float to, System.Action onComplete)
    {
        isFading = true;
        fadeImage.raycastTarget = true;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }

        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, to);
        fadeImage.raycastTarget = (to > 0.5f);
        isFading = false;

        onComplete?.Invoke();
    }
}
