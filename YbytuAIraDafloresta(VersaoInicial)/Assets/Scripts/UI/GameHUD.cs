using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [Header("Vida - Superior Esquerdo")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TMP_Text healthText;

    [Header("Retratos por estado de vida (saudavel -> morto)")]
    [SerializeField] private Sprite portraitSaudavel;
    [SerializeField] private Sprite portraitMeioSaudavel;
    [SerializeField] private Sprite portraitQuaseMorto;
    [SerializeField] private Sprite portraitMorto;

    [Header("Vidas e Score - Superior Direito")]
    [SerializeField] private RectTransform livesContainer;
    [SerializeField] private RectTransform scoreContainer;
    [SerializeField] private Sprite letraXSprite;
    [SerializeField] private Sprite lifeIconSprite;
    [SerializeField] private Sprite dotSprite;
    [SerializeField] private float livesDigitHeight = 60f;
    [SerializeField] private float scoreDigitHeight = 50f;
    [SerializeField] private float lifeIconHeight = 80f;
    [SerializeField] private float dotScale = 0.26f;
    [Tooltip("Labels TMP que substituem os digitos de sprite. Auto-criados se nulos.")]
    [SerializeField] private TMP_Text livesLabel;
    [SerializeField] private TMP_Text scoreLabel;
    [Tooltip("Fonte pixel (Bold02) para os valores numericos: score, vidas e hits.")]
    [SerializeField] private TMP_FontAsset valueFont;

    [Header("Combo - Direita (75% altura)")]
    [SerializeField] private Image comboRankImage;
    [SerializeField] private Image comboBarFill;
    [SerializeField] private RectTransform hitCountContainer;
    [SerializeField] private GameObject comboContainer;

    [Header("Sprites do Combo (rank C..SSS)")]
    [SerializeField] private Sprite[] rankSprites;
    [Header("Sprites de digitos (0..9) para hit count")]
    [SerializeField] private Sprite[] digitSprites;
    [SerializeField] private float digitSpacing = 8f;
    [SerializeField] private float digitHeight = 40f;
    [SerializeField] private TMP_Text hitCountLabel;

    [Header("Combo - Fade ao passar atras (como os props de frente)")]
    [SerializeField, Range(0f, 1f)] private float comboFadedAlpha = 0.4f;
    [SerializeField] private float comboFadeSpeed = 8f;

    [Header("Item - Inferior Direito")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemCountText;
    [SerializeField] private GameObject itemContainer;

    [Header("Wave Info")]
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private GameObject waveContainer;

    private PlayerCombatManager playerCombat;
    private ComboSystem combo;
    private HealthSystem health;
    private LivesSystem lives;
    private StageManager stageManager;
    private CombatZone currentZone;

    private int localScore;
    private bool useLocalScore;

    private Transform playerTransform;
    private CanvasGroup comboGroup;
    private RectTransform comboFadeRect;
    private Canvas hudCanvas;

    private void Start()
    {
        // Esconder wave info por padrao
        if (waveContainer != null) waveContainer.SetActive(false);
        if (comboContainer != null) comboContainer.SetActive(false);
        UpdateComboRank(ComboRank.C);
        UpdateComboBar(0f);

        SetupTextLabels();
        SetupComboFade();
        EnemyController.OnAnyEnemyDied += HandleAnyEnemyDied;

        FindPlayer();
        FindStageManager();

        useLocalScore = stageManager == null;
        UpdateScore(0);
    }

    private void OnDestroy()
    {
        EnemyController.OnAnyEnemyDied -= HandleAnyEnemyDied;
        UnsubscribeEvents();
        UnsubscribeStage();
    }

    private void Update()
    {
        UpdateComboFade();
    }

    private void SetupComboFade()
    {
        if (comboContainer == null) return;
        comboGroup = comboContainer.GetComponent<CanvasGroup>();
        if (comboGroup == null) comboGroup = comboContainer.AddComponent<CanvasGroup>();
        comboFadeRect = comboRankImage != null ? comboRankImage.rectTransform
                                               : comboContainer.GetComponent<RectTransform>();
        hudCanvas = comboContainer.GetComponentInParent<Canvas>();
    }

    private void UpdateComboFade()
    {
        if (comboGroup == null || comboFadeRect == null || playerTransform == null) return;
        if (comboContainer != null && !comboContainer.activeSelf) return;

        var cam = Camera.main;
        if (cam == null) return;

        Vector2 screenPoint = cam.WorldToScreenPoint(playerTransform.position + Vector3.up * 1f);
        Camera uiCam = (hudCanvas != null && hudCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? hudCanvas.worldCamera : null;
        bool behind = RectTransformUtility.RectangleContainsScreenPoint(comboFadeRect, screenPoint, uiCam);

        float target = behind ? comboFadedAlpha : 1f;
        comboGroup.alpha = Mathf.MoveTowards(comboGroup.alpha, target, comboFadeSpeed * Time.deltaTime);
    }

    private void HandleAnyEnemyDied(int score)
    {
        if (!useLocalScore) return;
        localScore += score;
        UpdateScore(localScore);
    }

    private void SetupTextLabels()
    {
        livesLabel = EnsureLabel(livesLabel, livesContainer, "3", livesDigitHeight, TextAlignmentOptions.MidlineRight);
        scoreLabel = EnsureLabel(scoreLabel, scoreContainer, "0", scoreDigitHeight, TextAlignmentOptions.MidlineRight);
        hitCountLabel = EnsureLabel(hitCountLabel, hitCountContainer, "0", digitHeight, TextAlignmentOptions.Center);
        ApplyValueFont(livesLabel);
        ApplyValueFont(scoreLabel);
        ApplyValueFont(hitCountLabel);
        BuildLifeIcon();
    }

    private void ApplyValueFont(TMP_Text label)
    {
        if (label != null && valueFont != null)
            label.font = valueFont;
    }

    private void BuildLifeIcon()
    {
        if (livesContainer == null || lifeIconSprite == null) return;
        if (livesContainer.Find("LifeIcon") != null) return;

        var go = new GameObject("LifeIcon", typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(livesContainer, false);
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(lifeIconHeight, lifeIconHeight);
        rt.anchoredPosition = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.sprite = lifeIconSprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        if (livesLabel != null)
        {
            var lrt = livesLabel.rectTransform;
            lrt.offsetMin = new Vector2(lifeIconHeight + 8f, lrt.offsetMin.y);
        }
    }

    private static TMP_Text EnsureLabel(TMP_Text existing, RectTransform parent, string initial, float height, TextAlignmentOptions align)
    {
        if (parent == null) return existing;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            if (existing != null && child == existing.transform) continue;
            Destroy(child.gameObject);
        }

        if (existing != null) return existing;

        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var t = (TextMeshProUGUI)go.GetComponent<TextMeshProUGUI>();
        t.text = initial;
        t.fontSize = Mathf.Max(12f, height * 0.9f);
        t.alignment = align;
        t.color = Color.white;
        t.fontStyle = FontStyles.Bold;
        t.raycastTarget = false;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    private void FindPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        playerTransform = player.transform;
        playerCombat = player.GetComponent<PlayerCombatManager>();
        combo = player.GetComponent<ComboSystem>();
        health = player.GetComponent<HealthSystem>();
        lives = player.GetComponent<LivesSystem>();

        SubscribeEvents();
        RefreshAll();
    }

    // --- Stage: score e waves ---

    private void FindStageManager()
    {
        stageManager = FindAnyObjectByType<StageManager>();
        if (stageManager == null) return;

        stageManager.OnScoreChanged += UpdateScore;
        UpdateScore(stageManager.TotalScore);

        if (stageManager.CombatZones != null)
        {
            foreach (var zone in stageManager.CombatZones)
            {
                if (zone == null) continue;
                zone.OnZoneActivated += HandleZoneActivated;
                zone.OnWaveStarted += HandleWaveStarted;
                zone.OnZoneCompleted += HandleZoneCompleted;
            }
        }
    }

    private void UnsubscribeStage()
    {
        if (stageManager == null) return;

        stageManager.OnScoreChanged -= UpdateScore;

        if (stageManager.CombatZones != null)
        {
            foreach (var zone in stageManager.CombatZones)
            {
                if (zone == null) continue;
                zone.OnZoneActivated -= HandleZoneActivated;
                zone.OnWaveStarted -= HandleWaveStarted;
                zone.OnZoneCompleted -= HandleZoneCompleted;
            }
        }
    }

    private void HandleZoneActivated(CombatZone zone)
    {
        currentZone = zone;
    }

    private void HandleWaveStarted(int waveIndex)
    {
        if (currentZone != null && currentZone.TotalWaves > 0)
            ShowWaveInfo(waveIndex, currentZone.TotalWaves);
    }

    private void HandleZoneCompleted(CombatZone zone)
    {
        HideWaveInfo();
        currentZone = null;
    }

    private void SubscribeEvents()
    {
        if (health != null) health.OnHealthChanged += UpdateHealthBar;
        if (lives != null) lives.OnLivesChanged += UpdateLives;
        if (combo != null)
        {
            combo.OnRankChanged += UpdateComboRank;
            combo.OnGaugeChanged += UpdateComboBar;
            combo.OnHitCountChanged += UpdateComboHitCount;
            combo.OnComboBreak += OnComboBreak;
        }
    }

    private void UnsubscribeEvents()
    {
        if (health != null) health.OnHealthChanged -= UpdateHealthBar;
        if (lives != null) lives.OnLivesChanged -= UpdateLives;
        if (combo != null)
        {
            combo.OnRankChanged -= UpdateComboRank;
            combo.OnGaugeChanged -= UpdateComboBar;
            combo.OnHitCountChanged -= UpdateComboHitCount;
            combo.OnComboBreak -= OnComboBreak;
        }
    }

    private void RefreshAll()
    {
        if (health != null) UpdateHealthBar(health.CurrentHealth, health.MaxHealth);
        if (lives != null) UpdateLives(lives.CurrentLives);
        if (combo != null)
        {
            UpdateComboRank(combo.CurrentRank);
            UpdateComboBar(combo.GaugePercent);
            UpdateComboHitCount(combo.CurrentHitCount);
        }
        UpdateScore(0);
    }

    // --- Health ---

    private void UpdateHealthBar(int current, int max)
    {
        float pct = max > 0 ? (float)current / max : 0f;

        if (healthBarFill != null)
            healthBarFill.fillAmount = pct;
        if (healthText != null)
            healthText.text = $"{current}/{max}";

        if (portraitImage != null)
        {
            Sprite target;
            if (current <= 0) target = portraitMorto;
            else if (pct < 0.35f) target = portraitQuaseMorto;
            else if (pct < 0.75f) target = portraitMeioSaudavel;
            else target = portraitSaudavel;

            if (target != null) portraitImage.sprite = target;
        }
    }

    // --- Lives ---

    private void UpdateLives(int currentLives)
    {
        if (livesLabel == null) return;
        int n = Mathf.Max(0, currentLives);
        livesLabel.text = valueFont != null
            ? $"<font=\"LiberationSans SDF\">X</font> {n}"
            : $"X {n}";
    }

    // --- Score ---

    public void UpdateScore(int score)
    {
        if (scoreLabel == null) return;
        string num = Mathf.Max(0, score).ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
        scoreLabel.text = valueFont != null
            ? $"<font=\"LiberationSans SDF\"><size=55%>PTS.</size></font> {num}"
            : $"PTS. {num}";
    }

    // --- Combo ---

    private void UpdateComboRank(ComboRank rank)
    {
        if (comboRankImage != null && rankSprites != null && (int)rank < rankSprites.Length)
        {
            var s = rankSprites[(int)rank];
            comboRankImage.sprite = s;
            comboRankImage.enabled = s != null;
        }
    }

    private void UpdateComboBar(float percent)
    {
        if (comboBarFill != null)
            comboBarFill.fillAmount = percent;
    }

    private void UpdateComboHitCount(int hits)
    {
        if (hitCountLabel != null)
            hitCountLabel.text = hits.ToString();
        if (comboContainer != null)
            comboContainer.SetActive(hits > 0);
    }

    private void OnComboBreak()
    {
    }

    // --- Wave ---

    public void ShowWaveInfo(int current, int total)
    {
        if (waveContainer != null) waveContainer.SetActive(false);
    }

    public void HideWaveInfo()
    {
        if (waveContainer != null) waveContainer.SetActive(false);
    }

    // --- Item ---

    public void SetItem(Sprite icon, int count)
    {
        if (itemContainer != null) itemContainer.SetActive(icon != null);
        if (itemIcon != null) itemIcon.sprite = icon;
        if (itemCountText != null) itemCountText.text = count > 1 ? $"x{count}" : "";
    }

    // --- Portrait ---

    public void SetPortrait(Sprite portrait)
    {
        if (portraitImage != null && portrait != null)
            portraitImage.sprite = portrait;
    }

}
