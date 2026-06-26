using UnityEngine;
using System;

public class ComboSystem : MonoBehaviour
{
    [Header("Barra de Combo")]
    [SerializeField] private float baseComboGauge = 100f;
    [SerializeField] private float comboGainPerHit = 15f;

    [Header("Dificuldade Progressiva (C, B, A, S, SS, SSS)")]
    [Tooltip("Multiplicador de tamanho da barra por rank.")]
    [SerializeField] private float[] gaugeScalePerRank = { 1.0f, 1.5f, 2.0f, 2.8f, 3.5f, 4.5f };
    [Tooltip("Ganho de barra diminui em ranks altos.")]
    [SerializeField] private float[] gainScalePerRank = { 1.0f, 0.85f, 0.7f, 0.55f, 0.45f, 0.35f };

    [Header("Combo Decay / Quebra")]
    [SerializeField] private float comboTimeout = 3f;
    [SerializeField] private float comboDrainPerSecond = 8f;
    [SerializeField] private float decayStartDelay = 1.5f;

    [Header("Multiplicadores por Rank (C, B, A, S, SS, SSS)")]
    [SerializeField] private float[] damageMultipliers = { 1.0f, 1.15f, 1.3f, 1.5f, 1.8f, 2.2f };
    [SerializeField] private float[] damageTakenMultipliers = { 1.0f, 1.05f, 1.15f, 1.25f, 1.4f, 1.6f };
    [Tooltip("Multiplicador de score por rank. Mais agressivo que o de dano pra premiar combo.")]
    [SerializeField] private float[] scoreMultipliers = { 1.0f, 2.0f, 3.5f, 5.0f, 8.0f, 12.0f };

    private float currentGauge;
    private ComboRank currentRank = ComboRank.C;
    private int currentHitCount;
    private int totalHitsLanded;
    private float timeSinceLastHit;
    private bool isPaused;
    private bool suppressTimeoutBreak;

    // Propriedades publicas
    public float CurrentGauge => currentGauge;
    public float MaxGauge => baseComboGauge * gaugeScalePerRank[(int)currentRank];
    public float GaugePercent => currentGauge / MaxGauge;
    public ComboRank CurrentRank => currentRank;
    public int CurrentHitCount => currentHitCount;
    public int TotalHitsLanded => totalHitsLanded;
    public float DamageMultiplier => damageMultipliers[(int)currentRank];
    public float DamageTakenMultiplier => damageTakenMultipliers[(int)currentRank];
    public float ScoreMultiplier => scoreMultipliers[(int)currentRank];
    public bool IsPaused => isPaused;

    // Eventos
    public event Action<ComboRank> OnRankChanged;
    public event Action<float> OnGaugeChanged;
    public event Action<int> OnHitCountChanged;
    public event Action OnComboBreak;

    private void Update()
    {
        if (isPaused) return;

        timeSinceLastHit += Time.deltaTime;

        if (!suppressTimeoutBreak && currentHitCount > 0 && timeSinceLastHit >= comboTimeout)
        {
            BreakCombo();
            return;
        }

        if (timeSinceLastHit > decayStartDelay && currentGauge > 0)
        {
            ChangeGauge(-comboDrainPerSecond * Time.deltaTime);
        }
    }

    public void RegisterHit()
    {
        currentHitCount++;
        totalHitsLanded++;
        timeSinceLastHit = 0f;

        float adjustedGain = comboGainPerHit * gainScalePerRank[(int)currentRank];
        ChangeGauge(adjustedGain);
        OnHitCountChanged?.Invoke(currentHitCount);
    }

    public void OnPlayerHurt()
    {
        FullResetCombo();
    }

    private void BreakCombo()
    {
        currentHitCount = 0;
        currentGauge = 0f;
        timeSinceLastHit = 0f;

        if (currentRank > ComboRank.C)
            SetRank(currentRank - 1);

        OnComboBreak?.Invoke();
        OnGaugeChanged?.Invoke(0f);
        OnHitCountChanged?.Invoke(0);
    }

    private void FullResetCombo()
    {
        currentHitCount = 0;
        currentGauge = 0f;
        timeSinceLastHit = 0f;
        SetRank(ComboRank.C);

        OnComboBreak?.Invoke();
        OnGaugeChanged?.Invoke(0f);
        OnHitCountChanged?.Invoke(0);
    }

    public void ResetCombo()
    {
        currentGauge = 0f;
        currentHitCount = 0;
        timeSinceLastHit = 0f;
        SetRank(ComboRank.C);
        OnGaugeChanged?.Invoke(0f);
        OnHitCountChanged?.Invoke(0);
    }

    public void Pause()
    {
        isPaused = true;
    }

    public void SetSuppressTimeoutBreak(bool value) => suppressTimeoutBreak = value;

    public void Resume()
    {
        isPaused = false;
        timeSinceLastHit = 0f; // Janela de graca apos retomar
    }

    private void ChangeGauge(float amount)
    {
        float max = MaxGauge;
        currentGauge = Mathf.Clamp(currentGauge + amount, 0f, max);
        OnGaugeChanged?.Invoke(GaugePercent);

        // Barra encheu: subir de rank
        if (currentGauge >= max)
            TryRankUp();
    }

    private void TryRankUp()
    {
        if (currentRank < ComboRank.SSS)
        {
            currentGauge = 0f;
            SetRank(currentRank + 1);
            OnGaugeChanged?.Invoke(0f);
            PlayRankUpSfx();
        }
    }

    private void PlayRankUpSfx()
    {
        var sm = SoundManager.Instance;
        if (sm == null || sm.Library == null) return;
        var notes = sm.Library.comboRankUp;
        if (notes == null) return;
        int i = (int)currentRank;
        if (i >= 0 && i < notes.Length && notes[i] != null)
            sm.PlaySFX(notes[i]);
    }

    private void SetRank(ComboRank newRank)
    {
        if (currentRank == newRank) return;
        currentRank = newRank;
        OnRankChanged?.Invoke(currentRank);
    }
}
