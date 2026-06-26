using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class DisclaimerUI : MonoBehaviour
{
    [Header("Tempo minimo antes de permitir avancar (segundos)")]
    [SerializeField] private float minDisplayTime = 1.5f;

    [Header("Prompt piscante")]
    [SerializeField] private float blinkInterval = 0.6f;

    private const string TitleText = "ATENÇÃO";

    private const string BodyText =
        "Este jogo apresenta cenas de violência simbólica e aborda temas sensíveis, como degradação ambiental, conflitos territoriais, destruição da floresta e impactos sobre povos originários.\n\n" +
        "O projeto foi desenvolvido em contexto acadêmico e possui finalidade educacional, buscando promover reflexão sobre a relação entre humanidade, natureza e sustentabilidade.\n\n" +
        "Mesmo sem a intenção de causar desconforto, alguns conteúdos podem ser sensíveis para determinadas pessoas.\n\n" +
        "Esperamos que você tenha uma boa experiência e que a jornada de Ybytu: A Ira da Floresta desperte reflexão sobre a importância da preservação da vida e da floresta.\n\n" +
        "Boa sorte!";

    private const string PromptText = "Espaço para continuar";

    private float elapsedTime;
    private bool advanced;
    private TMP_Text promptLabel;

    private void Start()
    {
        BuildUI();
        StartCoroutine(BlinkPrompt());
    }

    private void Update()
    {
        if (advanced) return;
        elapsedTime += Time.deltaTime;
        if (elapsedTime < minDisplayTime) return;

        bool space = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool gamepad = Gamepad.current != null &&
            (Gamepad.current.startButton.wasPressedThisFrame || Gamepad.current.buttonSouth.wasPressedThisFrame);

        if (space || gamepad)
            Advance();
    }

    public void Advance()
    {
        if (advanced) return;
        advanced = true;

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.OnDisclaimerEnd();
    }

    private void BuildUI()
    {
        var font = LoadFont();

        var canvasGO = new GameObject("DisclaimerCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Fundo preto full-screen
        var bg = new GameObject("Background", typeof(Image));
        bg.transform.SetParent(canvasGO.transform, false);
        var bgImg = bg.GetComponent<Image>();
        bgImg.color = Color.black;
        Stretch(bg.GetComponent<RectTransform>());

        // Titulo ATENCAO
        CreateText("Title", canvasGO.transform, font, TitleText, 72,
            FontStyles.Bold, new Color(0.95f, 0.27f, 0.22f), // vermelho de alerta
            anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f),
            pivot: new Vector2(0.5f, 1f), anchoredPos: new Vector2(0, -120),
            size: new Vector2(1400, 120), alignment: TextAlignmentOptions.Center);

        // Corpo do aviso
        CreateText("Body", canvasGO.transform, font, BodyText, 34,
            FontStyles.Normal, new Color(0.92f, 0.92f, 0.92f),
            anchorMin: new Vector2(0.5f, 0.5f), anchorMax: new Vector2(0.5f, 0.5f),
            pivot: new Vector2(0.5f, 0.5f), anchoredPos: new Vector2(0, 10),
            size: new Vector2(1300, 600), alignment: TextAlignmentOptions.Center);

        promptLabel = CreateText("Prompt", canvasGO.transform, font, PromptText, 30,
            FontStyles.Bold, new Color(1f, 0.95f, 0.6f),
            anchorMin: new Vector2(0.5f, 0f), anchorMax: new Vector2(0.5f, 0f),
            pivot: new Vector2(0.5f, 0f), anchoredPos: new Vector2(0, 80),
            size: new Vector2(1000, 60), alignment: TextAlignmentOptions.Center);
    }

    private IEnumerator BlinkPrompt()
    {
        bool visible = true;
        var wait = new WaitForSeconds(blinkInterval);
        while (true)
        {
            if (promptLabel != null)
            {
                var c = promptLabel.color;
                c.a = visible ? 1f : 0.15f;
                promptLabel.color = c;
            }
            visible = !visible;
            yield return wait;
        }
    }

    private static TMP_FontAsset LoadFont()
    {
        var f = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (f == null) f = TMP_Settings.defaultFontAsset;
        return f;
    }

    private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font,
        string text, float fontSize, FontStyles style, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos,
        Vector2 size, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
        return tmp;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
