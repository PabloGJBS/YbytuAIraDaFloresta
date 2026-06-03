using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Encerramento em tela cheia (fundo preto) apos a cutscene de noticias. Quando o
/// PrologueCutsceneController termina, escreve a mensagem final na tela com efeito de
/// digitacao (como a caixa de dialogo, porem em tela cheia), uma tela por vez, e ao
/// final volta ao Menu Principal. Substitui o OutroCutsceneBridge. UI construida em codigo.
/// </summary>
[RequireComponent(typeof(PrologueCutsceneController))]
public class OutroEndingMessage : MonoBehaviour
{
    [SerializeField] private PrologueCutsceneController cutscene;
    [SerializeField] private string sectionKey = "cutscene.outro.ending";
    [SerializeField] private float charDelay = 0.022f;

    private GameObject root;
    private TMP_Text label;
    private TMP_Text continuePrompt;
    private AudioSource typingSource;
    private string[] pages;
    private bool running;
    private bool isTyping;

    private void Awake()
    {
        if (cutscene == null) cutscene = GetComponent<PrologueCutsceneController>();
        Build();
    }

    private void OnEnable()
    {
        if (cutscene != null) cutscene.OnFinished += HandleFinished;
    }

    private void OnDisable()
    {
        if (cutscene != null) cutscene.OnFinished -= HandleFinished;
    }

    private void HandleFinished()
    {
        if (running) return;
        var lm = LocalizationManager.Instance;
        pages = lm != null ? lm.GetSection(sectionKey) : null;
        if (pages == null || pages.Length == 0) { GoToMenu(); return; }
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        running = true;
        root.SetActive(true);
        // Descarta o aperto que veio da ultima noticia; senao ele "pula" a digitacao no 1o frame.
        yield return null;

        // Tudo numa unica tela, digitado de uma vez.
        string full = string.Join("\n\n", pages);
        continuePrompt.gameObject.SetActive(false);
        yield return TypeText(full);

        continuePrompt.gameObject.SetActive(true);
        yield return WaitForPress();

        GoToMenu();
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        label.text = "";
        int typed = 0;
        foreach (char c in text)
        {
            // Pular a digitacao mostra o texto completo.
            if (AdvancePressed())
            {
                label.text = text;
                break;
            }

            label.text += c;
            if (!char.IsWhiteSpace(c) && (typed++ % 2 == 0)) PlayTypeBlip();
            yield return new WaitForSecondsRealtime(charDelay);
        }
        isTyping = false;
        if (typingSource != null) typingSource.Stop();
        yield return null; // evita que o mesmo aperto que pulou ja avance a pagina
    }

    private IEnumerator WaitForPress()
    {
        while (!AdvancePressed()) yield return null;
    }

    private void PlayTypeBlip()
    {
        var sm = SoundManager.Instance;
        if (sm == null || sm.Library == null || sm.Library.dialogueType == null) return;
        if (typingSource.outputAudioMixerGroup == null) typingSource.outputAudioMixerGroup = sm.SfxGroup;
        typingSource.pitch = 1f + UnityEngine.Random.Range(-0.04f, 0.04f);
        typingSource.PlayOneShot(sm.Library.dialogueType, 0.5f);
    }

    private static bool AdvancePressed()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            return true;
        var gp = Gamepad.current;
        if (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.startButton.wasPressedThisFrame))
            return true;
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            return true;
        return false;
    }

    private void GoToMenu()
    {
        // O OutroCutscene_Manager pode ser DontDestroyOnLoad (via LocalizationManager),
        // entao a tela preta persistiria por cima do menu. Destruir o overlay antes de trocar.
        if (root != null) Destroy(root);
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.GoToMainMenu();
        else SceneManager.LoadScene("MainMenu");
    }

    private static TMP_FontAsset LoadFont()
    {
        var f = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        return f != null ? f : TMP_Settings.defaultFontAsset;
    }

    private void Build()
    {
        var canvasGo = new GameObject("OutroEndingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        root = canvasGo;

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Fundo PRETO opaco (corte seco, sem bleed da cena anterior)
        var bg = new GameObject("BG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bg.transform.SetParent(canvasGo.transform, false);
        var brt = bg.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = Color.black;
        bg.GetComponent<Image>().raycastTarget = false;

        // Texto central, escrito na tela
        var textGo = new GameObject("Message", typeof(RectTransform));
        textGo.transform.SetParent(canvasGo.transform, false);
        label = textGo.AddComponent<TextMeshProUGUI>();
        var trt = label.rectTransform;
        trt.anchorMin = new Vector2(0.5f, 0.5f);
        trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.pivot = new Vector2(0.5f, 0.5f);
        trt.sizeDelta = new Vector2(1450f, 820f);
        trt.anchoredPosition = new Vector2(0f, 30f);
        label.font = LoadFont();
        label.color = new Color(0.95f, 0.97f, 0.92f);
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.enableAutoSizing = true;
        label.fontSizeMin = 26f;
        label.fontSizeMax = 44f;
        label.lineSpacing = 12f;
        label.paragraphSpacing = 18f;
        label.raycastTarget = false;

        // Prompt "continuar"
        var contGo = new GameObject("Continue", typeof(RectTransform));
        contGo.transform.SetParent(canvasGo.transform, false);
        continuePrompt = contGo.AddComponent<TextMeshProUGUI>();
        var crt = continuePrompt.rectTransform;
        crt.anchorMin = new Vector2(0.5f, 0f);
        crt.anchorMax = new Vector2(0.5f, 0f);
        crt.pivot = new Vector2(0.5f, 0f);
        crt.sizeDelta = new Vector2(800f, 50f);
        crt.anchoredPosition = new Vector2(0f, 50f);
        continuePrompt.font = LoadFont();
        continuePrompt.fontSize = 26f;
        continuePrompt.color = new Color(0.7f, 1f, 0.6f, 0.9f);
        continuePrompt.alignment = TextAlignmentOptions.Center;
        continuePrompt.text = "Espaço para continuar";
        continuePrompt.raycastTarget = false;

        typingSource = canvasGo.AddComponent<AudioSource>();
        typingSource.playOnAwake = false;
        typingSource.loop = false;

        canvasGo.SetActive(false);
    }
}
