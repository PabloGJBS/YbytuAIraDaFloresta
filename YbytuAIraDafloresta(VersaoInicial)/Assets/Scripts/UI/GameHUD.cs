using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD do gameplay.
///
/// Layout:
/// ┌──────────────────────────────────────────────────┐
/// │ [Foto] ████████ HP Bar              Vidas: x3  │
/// │                                     Score: 1500 │
/// │                                                  │
/// │                                                  │
/// │                                                  │
/// │                                          [C]     │  ← Combo Rank (75% altura)
/// │                               Item ►     ███     │  ← Combo Bar
/// └──────────────────────────────────────────────────┘
///
/// Canto sup. esquerdo: foto do personagem + barra de vida
/// Canto sup. direito: vidas restantes + score total
/// Canto inf. direito (75% altura): letra do combo rank + barra de combo
/// Canto inf. direito: item utilizavel
/// </summary>
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

    private void Start()
    {
        // Esconder wave info por padrao
        if (waveContainer != null) waveContainer.SetActive(false);
        // Combo oculto ate o primeiro hit
        if (comboContainer != null) comboContainer.SetActive(false);
        // Reset visual do combo (caso a cena tenha sido salva com estado SSS/cheio)
        UpdateComboRank(ComboRank.C);
        UpdateComboBar(0f);

        SetupTextLabels();
        EnemyController.OnAnyEnemyDied += HandleAnyEnemyDied;

        FindPlayer();
        FindStageManager();

        // Se nao ha StageManager, acumular score localmente via evento estatico de inimigos.
        useLocalScore = stageManager == null;
        UpdateScore(0);
    }

    private void OnDestroy()
    {
        EnemyController.OnAnyEnemyDied -= HandleAnyEnemyDied;
        UnsubscribeEvents();
        UnsubscribeStage();
    }

    private void HandleAnyEnemyDied(int score)
    {
        if (!useLocalScore) return;
        localScore += score;
        UpdateScore(localScore);
    }

    private void SetupTextLabels()
    {
        livesLabel = EnsureLabel(livesLabel, livesContainer, "x3", livesDigitHeight, TextAlignmentOptions.MidlineRight);
        scoreLabel = EnsureLabel(scoreLabel, scoreContainer, "0", scoreDigitHeight, TextAlignmentOptions.MidlineRight);
        hitCountLabel = EnsureLabel(hitCountLabel, hitCountContainer, "0", digitHeight, TextAlignmentOptions.Center);
    }

    private static TMP_Text EnsureLabel(TMP_Text existing, RectTransform parent, string initial, float height, TextAlignmentOptions align)
    {
        if (parent == null) return existing;

        // Limpa todos os children sprite (digitos antigos, icones)
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
        if (livesLabel != null)
            livesLabel.text = $"x{Mathf.Max(0, currentLives)}";
    }

    // --- Score ---

    public void UpdateScore(int score)
    {
        if (scoreLabel != null)
            scoreLabel.text = Mathf.Max(0, score).ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
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
        // Futuro: animacao de combo break, flash vermelho, etc
    }

    // --- Wave ---

    public void ShowWaveInfo(int current, int total)
    {
        if (waveContainer != null) waveContainer.SetActive(true);
        if (waveText != null)
        {
            if (LocalizationManager.Instance != null)
                waveText.text = LocalizationManager.Instance.GetTextFormatted("ui.hud.wave", current + 1, total);
            else
                waveText.text = $"Wave {current + 1}/{total}";
        }
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
