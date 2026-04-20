using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Ferramenta de editor que cria toda a estrutura do Canvas da HUD.
/// Menu: Tools > Setup Game HUD
/// </summary>
public class HUDSetupTool
{
    [MenuItem("Tools/Setup Game HUD")]
    public static void CreateHUD()
    {
        // Canvas principal
        var canvasGO = new GameObject("GameHUD_Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var hud = canvasGO.AddComponent<GameHUD>();

        // ============================================
        // SUPERIOR ESQUERDO - Retrato + Barra de Vida
        // ============================================
        var topLeft = CreatePanel(canvasGO.transform, "TopLeft_HealthPanel",
            TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(20, -20), new Vector2(350, 80));

        // Retrato do personagem
        var portrait = CreateImage(topLeft.transform, "Portrait",
            new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(10, 0), new Vector2(70, 70));
        portrait.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Fundo da barra de vida
        var healthBarBg = CreateImage(topLeft.transform, "HealthBar_Background",
            new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(90, 10), new Vector2(240, 25));
        healthBarBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        // Preenchimento da barra de vida
        var healthBarFill = CreateImage(healthBarBg.transform, "HealthBar_Fill",
            new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(2, 2), new Vector2(236, 21));
        healthBarFill.color = new Color(0.2f, 0.8f, 0.2f, 1f);
        healthBarFill.type = Image.Type.Filled;
        healthBarFill.fillMethod = Image.FillMethod.Horizontal;
        healthBarFill.fillAmount = 1f;

        // Texto de HP
        var healthText = CreateText(topLeft.transform, "HealthText",
            new Vector2(0, 0.5f), new Vector2(0, 0.5f),
            new Vector2(90, -20), new Vector2(240, 25),
            "100/100", 16, TextAlignmentOptions.Center);

        // ============================================
        // SUPERIOR DIREITO - Vidas + Score
        // ============================================
        var topRight = CreatePanel(canvasGO.transform, "TopRight_LivesScorePanel",
            TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-20, -20), new Vector2(250, 80));

        // Container de vidas (X + digitos)
        var livesContainerGO = new GameObject("LivesContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        livesContainerGO.transform.SetParent(topRight.transform, false);
        var livesRT = livesContainerGO.GetComponent<RectTransform>();
        livesRT.anchorMin = new Vector2(1, 1);
        livesRT.anchorMax = new Vector2(1, 1);
        livesRT.pivot = new Vector2(1, 1);
        livesRT.anchoredPosition = new Vector2(-10, -10);
        livesRT.sizeDelta = new Vector2(180, 70);
        var livesLayout = livesContainerGO.GetComponent<HorizontalLayoutGroup>();
        livesLayout.childAlignment = TextAnchor.MiddleRight;
        livesLayout.childForceExpandWidth = false;
        livesLayout.childForceExpandHeight = false;
        livesLayout.spacing = 6f;

        // Container de score (digitos)
        var scoreContainerGO = new GameObject("ScoreContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        scoreContainerGO.transform.SetParent(topRight.transform, false);
        var scoreRT = scoreContainerGO.GetComponent<RectTransform>();
        scoreRT.anchorMin = new Vector2(1, 1);
        scoreRT.anchorMax = new Vector2(1, 1);
        scoreRT.pivot = new Vector2(1, 1);
        scoreRT.anchoredPosition = new Vector2(-10, -90);
        scoreRT.sizeDelta = new Vector2(220, 60);
        var scoreLayout = scoreContainerGO.GetComponent<HorizontalLayoutGroup>();
        scoreLayout.childAlignment = TextAnchor.MiddleRight;
        scoreLayout.childForceExpandWidth = false;
        scoreLayout.childForceExpandHeight = false;
        scoreLayout.spacing = 6f;

        // ============================================
        // DIREITA (75% altura) - Combo Rank + Barra
        // ============================================
        var comboContainer = CreatePanel(canvasGO.transform, "Combo",
            TextAnchor.MiddleRight, new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-30, 270), new Vector2(100, 120)); // 270px from bottom ≈ 75% em 1080p

        // Letra grande do rank (imagem)
        var comboRankImageGO = new GameObject("ComboRankImage", typeof(RectTransform), typeof(Image));
        comboRankImageGO.transform.SetParent(comboContainer.transform, false);
        var crRT = comboRankImageGO.GetComponent<RectTransform>();
        crRT.anchorMin = new Vector2(0.5f, 1);
        crRT.anchorMax = new Vector2(0.5f, 1);
        crRT.pivot = new Vector2(0.5f, 1);
        crRT.anchoredPosition = new Vector2(0, -5);
        crRT.sizeDelta = new Vector2(80, 80);
        var comboRankImage = comboRankImageGO.GetComponent<Image>();
        comboRankImage.preserveAspect = true;
        comboRankImage.raycastTarget = false;

        // Container de digitos do hit count (HorizontalLayoutGroup)
        var hitCountGO = new GameObject("HitCountContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        hitCountGO.transform.SetParent(comboContainer.transform, false);
        var hcRT = hitCountGO.GetComponent<RectTransform>();
        hcRT.anchorMin = new Vector2(0.5f, 1);
        hcRT.anchorMax = new Vector2(0.5f, 1);
        hcRT.pivot = new Vector2(0.5f, 1);
        hcRT.anchoredPosition = new Vector2(0, -90);
        hcRT.sizeDelta = new Vector2(120, 40);
        var hcLayout = hitCountGO.GetComponent<HorizontalLayoutGroup>();
        hcLayout.childAlignment = TextAnchor.MiddleCenter;
        hcLayout.childForceExpandWidth = false;
        hcLayout.childForceExpandHeight = false;
        hcLayout.spacing = -4f;

        // Fundo da barra de combo
        var comboBarBg = CreateImage(comboContainer.transform, "ComboBar_Background",
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 5), new Vector2(80, 15));
        comboBarBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        // Preenchimento da barra de combo
        var comboBarFill = CreateImage(comboBarBg.transform, "ComboBar_Fill",
            new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(2, 2), new Vector2(76, 11));
        comboBarFill.color = Color.cyan;
        comboBarFill.type = Image.Type.Filled;
        comboBarFill.fillMethod = Image.FillMethod.Horizontal;
        comboBarFill.fillAmount = 0f;

        // ============================================
        // INFERIOR DIREITO - Item utilizável
        // ============================================
        var itemContainer = CreatePanel(canvasGO.transform, "ItemContainer",
            TextAnchor.LowerRight, new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-30, 30), new Vector2(80, 80));

        // Icone do item
        var itemIcon = CreateImage(itemContainer.transform, "ItemIcon",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(60, 60));
        itemIcon.color = new Color(1, 1, 1, 0.5f);

        // Quantidade do item
        var itemCountText = CreateText(itemContainer.transform, "ItemCountText",
            new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-5, 5), new Vector2(40, 25),
            "x1", 16, TextAlignmentOptions.Right);

        // ============================================
        // CENTRO - Wave Info (aparece temporariamente)
        // ============================================
        var waveContainer = CreatePanel(canvasGO.transform, "WaveContainer",
            TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -100), new Vector2(300, 50));

        var waveText = CreateText(waveContainer.transform, "WaveText",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(280, 40),
            "Wave 1/3", 28, TextAlignmentOptions.Center);
        waveText.fontStyle = FontStyles.Bold;

        // ============================================
        // Conectar referencias no GameHUD via SerializedObject
        // ============================================
        var so = new SerializedObject(hud);
        so.FindProperty("portraitImage").objectReferenceValue = portrait;
        so.FindProperty("healthBarFill").objectReferenceValue = healthBarFill;
        so.FindProperty("healthText").objectReferenceValue = healthText.GetComponent<TMP_Text>();
        so.FindProperty("livesContainer").objectReferenceValue = livesRT;
        so.FindProperty("scoreContainer").objectReferenceValue = scoreRT;
        so.FindProperty("comboRankImage").objectReferenceValue = comboRankImage;
        so.FindProperty("comboBarFill").objectReferenceValue = comboBarFill;
        so.FindProperty("hitCountContainer").objectReferenceValue = hcRT;
        so.FindProperty("comboContainer").objectReferenceValue = comboContainer;
        so.FindProperty("itemIcon").objectReferenceValue = itemIcon;
        so.FindProperty("itemCountText").objectReferenceValue = itemCountText.GetComponent<TMP_Text>();
        so.FindProperty("itemContainer").objectReferenceValue = itemContainer;
        so.FindProperty("waveText").objectReferenceValue = waveText.GetComponent<TMP_Text>();
        so.FindProperty("waveContainer").objectReferenceValue = waveContainer;
        so.ApplyModifiedProperties();

        // Desativar containers opcionais por padrao
        comboContainer.SetActive(false);
        itemContainer.SetActive(false);
        waveContainer.SetActive(false);

        Selection.activeGameObject = canvasGO;
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Game HUD");

        Debug.Log("[HUDSetup] HUD criada com sucesso! Todas as referencias conectadas.");
    }

    // --- Helpers de criacao ---

    private static GameObject CreatePanel(Transform parent, string name,
        TextAnchor childAlignment, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = anchorMin;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        return go;
    }

    private static Image CreateImage(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = anchorMin;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        return go.GetComponent<Image>();
    }

    private static TMP_Text CreateText(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta,
        string defaultText, float fontSize, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = anchorMin;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        return tmp;
    }
}
