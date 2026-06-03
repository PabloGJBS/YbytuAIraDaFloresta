using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

/// <summary>
/// Banner de frase educativa no rodape da tela. Dois modos:
///  - Auto (modal=false): aparece com fade, segura e some sozinho. Nao pausa o jogo.
///  - Modal (modal=true): CONGELA o player e so sai quando o jogador aperta continuar
///    (Espaco/Enter/clique/A). Usado pelos elementos interativos (troncos).
/// UI construida em codigo, reaproveitada entre as chamadas.
/// Uso: EducationalBanner.Show("frase", 5f, modal: true);
/// </summary>
public class EducationalBanner : MonoBehaviour
{
    private static EducationalBanner instance;

    private CanvasGroup group;
    private TMP_Text label;
    private TMP_Text continuePrompt;
    private RectTransform panelRect;
    private Coroutine routine;
    private bool modalActive;
    private bool canDismiss;

    public static void Show(string phrase, float holdSeconds = 5f, bool modal = false)
    {
        if (string.IsNullOrWhiteSpace(phrase)) return;
        if (instance == null) instance = Build();
        instance.Display(phrase, holdSeconds, modal);
    }

    private void Display(string phrase, float hold, bool modal)
    {
        label.text = phrase;
        label.ForceMeshUpdate();
        if (panelRect != null)
        {
            float extra = modal ? 78f : 44f; // espaco extra pra linha de "continuar"
            float h = label.preferredHeight + extra;
            panelRect.sizeDelta = new Vector2(1300f, Mathf.Max(130f, h));
        }
        continuePrompt.gameObject.SetActive(modal);

        if (routine != null) StopCoroutine(routine);
        routine = modal ? StartCoroutine(ModalRoutine()) : StartCoroutine(AutoRoutine(hold, phrase));
    }

    private IEnumerator AutoRoutine(float hold, string phrase)
    {
        modalActive = false;
        if (hold <= 0f) hold = Mathf.Clamp(phrase.Length * 0.05f, 4f, 11f);
        yield return Fade(group.alpha, 1f, 0.4f);
        yield return new WaitForSecondsRealtime(hold);
        yield return Fade(1f, 0f, 0.6f);
    }

    private IEnumerator ModalRoutine()
    {
        modalActive = true;
        canDismiss = false;
        PlayerController.InputFrozen = true;

        yield return Fade(group.alpha, 1f, 0.35f);
        canDismiss = true; // so aceita continuar depois do fade (evita consumir o mesmo aperto que abriu)

        while (!ContinuePressed())
            yield return null;

        canDismiss = false;
        yield return Fade(1f, 0f, 0.5f);
        PlayerController.InputFrozen = false;
        modalActive = false;
    }

    private void OnDisable()
    {
        // Evita deixar o player travado se o banner for destruido durante o modal (troca de cena).
        if (modalActive) PlayerController.InputFrozen = false;
    }

    private bool ContinuePressed()
    {
        if (!canDismiss) return false;
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame))
            return true;
        var gp = Gamepad.current;
        if (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.buttonNorth.wasPressedThisFrame))
            return true;
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            return true;
        return false;
    }

    private IEnumerator Fade(float from, float to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
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

        // Painel no rodape, centralizado
        var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelGo.transform.SetParent(canvasGo.transform, false);
        var prt = panelGo.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0f);
        prt.anchorMax = new Vector2(0.5f, 0f);
        prt.pivot = new Vector2(0.5f, 0f);
        prt.anchoredPosition = new Vector2(0f, 70f);
        prt.sizeDelta = new Vector2(1300f, 150f);
        var panelImg = panelGo.GetComponent<Image>();
        panelImg.color = new Color(0.04f, 0.08f, 0.04f, 0.86f); // verde-escuro translucido
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

        // Texto da frase
        var textGo = new GameObject("Phrase", typeof(RectTransform));
        textGo.transform.SetParent(panelGo.transform, false);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        var trt = tmp.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(45f, 40f);
        trt.offsetMax = new Vector2(-35f, -18f);
        tmp.font = LoadFont();
        tmp.fontSize = 30f;
        tmp.color = new Color(0.95f, 0.97f, 0.92f);
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;

        // Linha "continuar" (so aparece no modo modal)
        var contGo = new GameObject("ContinuePrompt", typeof(RectTransform));
        contGo.transform.SetParent(panelGo.transform, false);
        var ctmp = contGo.AddComponent<TextMeshProUGUI>();
        var crt = ctmp.rectTransform;
        crt.anchorMin = new Vector2(0f, 0f);
        crt.anchorMax = new Vector2(1f, 0f);
        crt.pivot = new Vector2(0.5f, 0f);
        crt.offsetMin = new Vector2(45f, 8f);
        crt.offsetMax = new Vector2(-25f, 32f);
        ctmp.font = LoadFont();
        ctmp.fontSize = 20f;
        ctmp.color = new Color(0.7f, 1f, 0.6f, 0.9f);
        ctmp.alignment = TextAlignmentOptions.MidlineRight;
        ctmp.text = "Espaço para continuar";
        ctmp.raycastTarget = false;

        var group = canvasGo.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        var banner = canvasGo.AddComponent<EducationalBanner>();
        banner.group = group;
        banner.label = tmp;
        banner.continuePrompt = ctmp;
        banner.panelRect = prt;
        return banner;
    }
}
