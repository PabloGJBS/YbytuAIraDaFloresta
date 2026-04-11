using UnityEngine;
using System;

/// <summary>
/// Sistema de combo do jogador.
/// Cada golpe acertado enche a barra de combo. Ao encher, o rank sobe.
/// Ranks mais altos aumentam dano causado mas tambem dano recebido.
/// A barra fica progressivamente mais dificil de encher em ranks altos.
/// Combo quebra por tempo sem acertar OU ao receber dano.
/// Timer pode ser pausado (transicao de cenario, cutscenes).
/// </summary>
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
    private float timeSinceLastHit;
    private bool isPaused;

    // Propriedades publicas
    public float CurrentGauge => currentGauge;
    public float MaxGauge => baseComboGauge * gaugeScalePerRank[(int)currentRank];
    public float GaugePercent => currentGauge / MaxGauge;
    public ComboRank CurrentRank => currentRank;
    public int CurrentHitCount => currentHitCount;
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

        // Timeout: combo quebra completamente se ficar muito tempo sem acertar
        if (currentHitCount > 0 && timeSinceLastHit >= comboTimeout)
        {
            BreakCombo();
            return;
        }

        // Decay: barra drena apos um delay menor que o timeout
        if (timeSinceLastHit > decayStartDelay && currentGauge > 0)
        {
            ChangeGauge(-comboDrainPerSecond * Time.deltaTime);
        }
    }

    /// <summary>
    /// Chamado quando o jogador acerta um golpe no inimigo.
    /// </summary>
    public void RegisterHit()
    {
        currentHitCount++;
        timeSinceLastHit = 0f;

        // Ganho ajustado pela dificuldade do rank atual
        float adjustedGain = comboGainPerHit * gainScalePerRank[(int)currentRank];
        ChangeGauge(adjustedGain);
        OnHitCountChanged?.Invoke(currentHitCount);
    }

    /// <summary>
    /// Chamado quando o jogador leva dano. Reseta o combo completamente para C.
    /// </summary>
    public void OnPlayerHurt()
    {
        FullResetCombo();
    }

    /// <summary>
    /// Quebra suave (timeout sem acertar): hit count zera, barra zera, rank desce 1.
    /// </summary>
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

    /// <summary>
    /// Reset total: zera tudo e volta direto para C.
    /// Usado quando o jogador toma dano.
    /// </summary>
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

    /// <summary>
    /// Reseta o combo completamente para D (morte, nova fase, etc).
    /// </summary>
    public void ResetCombo()
    {
        currentGauge = 0f;
        currentHitCount = 0;
        timeSinceLastHit = 0f;
        SetRank(ComboRank.C);
        OnGaugeChanged?.Invoke(0f);
        OnHitCountChanged?.Invoke(0);
    }

    /// <summary>
    /// Pausa o timer do combo (transicao de cenario, cutscenes, dialogo).
    /// A barra e o rank sao mantidos.
    /// </summary>
    public void Pause()
    {
        isPaused = true;
    }

    /// <summary>
    /// Retoma o timer do combo. Reseta o tempo desde ultimo hit
    /// para dar uma janela ao jogador apos a transicao.
    /// </summary>
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
        }
    }

    private void SetRank(ComboRank newRank)
    {
        if (currentRank == newRank) return;
        currentRank = newRank;
        OnRankChanged?.Invoke(currentRank);
    }
}
