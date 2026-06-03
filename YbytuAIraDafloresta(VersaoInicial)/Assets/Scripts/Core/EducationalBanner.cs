using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Banner de frase educativa no topo da tela. Aparece com fade, segura e some.
/// Nao pausa o jogo (pensado para os trechos de caminhada). UI construida em codigo,
/// reaproveitada entre as chamadas. Uso: EducationalBanner.Show("frase", 5f);
/// </summary>
public class EducationalBanner : MonoBehaviour
{
    private static EducationalBanner instance;

    private CanvasGroup group;
    private TMP_Text label;
    private RectTransform panelRect;
    private Coroutine routine;

    public static void Show(string phrase, float holdSeconds = 5f)
    {
        if (string.IsNullOrWhiteSpace(phrase)) return;
        if (instance == null) instance = Build();
        instance.Display(phrase, holdSeconds);
    }

    private void Display(string phrase, float hold)
    {
        label.text = phrase;
        label.ForceMeshUpdate();
        if (panelRect != null)
        {
            float h = label.preferredHeight + 44f; // texto + padding
            panelRect.sizeDelta = new Vector2(1300f, Mathf.Max(120f, h));
        }
        if (hold <= 0f) hold = Mathf.Clamp(phrase.Length * 0.05f, 4f, 11f); // tempo de leitura por tamanho
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(FadeRoutine(hold));
    }

    private IEnumerator FadeRoutine(float hold)
    {
        yield return Fade(group.alpha, 1f, 0.4f);
        yield return new WaitForSeconds(hold);
        yield return Fade(1f, 0f, 0.6f);
    }

    private IEnumerator Fade(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }
        group.alpha = to;
    }

    private static TMP_FontAsset LoadFont()
    {
        var f = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        return f != null ? f : TMP_Settings.defaultFontAsset;
    }

    private static EducationalBanner Build()
    {
        var canvasGo = new GameObject("EducationalBannerCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasGo);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Painel no topo, centralizado
        var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelGo.transform.SetParent(canvasGo.transform, false);
        var prt = panelGo.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 1f);
        prt.anchorMax = new Vector2(0.5f, 1f);
        prt.pivot = new Vector2(0.5f, 1f);
        prt.anchoredPosition = new Vector2(0f, -60f);
        prt.sizeDelta = new Vector2(1300f, 150f);
        var panelImg = panelGo.GetComponent<Image>();
        panelImg.color = new Color(0.04f, 0.08f, 0.04f, 0.82f); // verde-escuro translucido
        panelImg.raycastTarget = false;

        // Faixinha de destaque (verde) na esquerda
        var accentGo = new GameObject("Accent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        accentGo.transform.SetParent(panelGo.transform, false);
        var art = accentGo.GetComponent<RectTransform>();
        art.anchorMin = new Vector2(0f, 0f);
        art.anchorMax = new Vector2(0f, 1f);
        art.pivot = new Vector2(0f, 0.5f);
        art.offsetMin = Vector2.zero;
        art.offsetMax = new Vector2(10f, 0f);
        accentGo.GetComponent<Image>().color = new Color(0.45f, 0.85f, 0.4f, 1f);
        accentGo.GetComponent<Image>().raycastTarget = false;

        var textGo = new GameObject("Phrase", typeof(RectTransform));
        textGo.transform.SetParent(panelGo.transform, false);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        var trt = tmp.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(45f, 18f);
        trt.offsetMax = new Vector2(-35f, -18f);
        tmp.font = LoadFont();
        tmp.fontSize = 30f;
        tmp.color = new Color(0.95f, 0.97f, 0.92f);
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;

        var group = canvasGo.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        var banner = canvasGo.AddComponent<EducationalBanner>();
        banner.group = group;
        banner.label = tmp;
        banner.panelRect = prt;
        return banner;
    }
}
