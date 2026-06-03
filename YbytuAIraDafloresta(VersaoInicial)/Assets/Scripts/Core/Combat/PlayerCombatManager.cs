using UnityEngine;
using System;

/// <summary>
/// Gerenciador central de combate do jogador.
/// Conecta HealthSystem, LivesSystem e ComboSystem.
/// </summary>
[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(LivesSystem))]
[RequireComponent(typeof(ComboSystem))]
public class PlayerCombatManager : MonoBehaviour
{
    private HealthSystem health;
    private LivesSystem lives;
    private ComboSystem combo;
    private PlayerController playerController;

    public HealthSystem Health => health;
    public LivesSystem Lives => lives;
    public ComboSystem Combo => combo;

    // Eventos de alto nivel
    public event Action OnPlayerDied;
    public event Action OnPlayerRevived;
    public event Action OnGameOver;

    private void Awake()
    {
        health = GetComponent<HealthSystem>();
        lives = GetComponent<LivesSystem>();
        combo = GetComponent<ComboSystem>();
        playerController = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        health.OnDeath += HandleDeath;
        health.OnDamageTaken += HandleDamageTaken;
    }

    private void OnDisable()
    {
        health.OnDeath -= HandleDeath;
        health.OnDamageTaken -= HandleDamageTaken;
    }

    /// <summary>
    /// Calcula o dano final de um ataque com base no rank atual do combo.
    /// O registro do hit no combo acontece quando o dano e confirmado
    /// (ver EnemyController.HandleDamageTaken), evitando contagem dupla
    /// quando um mesmo golpe atinge varios inimigos.
    /// </summary>
    public int CalculateAttackDamage(int baseDamage)
    {
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * combo.DamageMultiplier));
    }

    /// <summary>
    /// Chamado quando o player recebe dano.
    /// Aplica o multiplicador de dano recebido pelo rank do combo.
    /// </summary>
    public void ReceiveDamage(int baseDamage)
    {
        float multiplier = combo.DamageTakenMultiplier;
        health.TakeDamage(baseDamage, multiplier);
        combo.OnPlayerHurt();

        if (playerController != null)
            playerController.TakeHit();
    }

    private void HandleDamageTaken(int damage)
    {
        // Futuro: flash vermelho, screen shake, SFX
    }

    private void HandleDeath()
    {
        OnPlayerDied?.Invoke();
        // Sem auto-revive: apos a animacao de morte, dispara o Game Over.
        // O overlay (GameOverController) decide entre Continuar (custa uma vida) ou Desistir.
        Invoke(nameof(HandleGameOver), 1.2f);
    }

    private void HandleGameOver()
    {
        combo.ResetCombo();

        var sm = SoundManager.Instance;
        if (sm != null && sm.Library != null && sm.Library.gameOver != null)
        {
            sm.StopBGM();
            sm.PlaySFX(sm.Library.gameOver, 1f, 0f);
        }

        OnGameOver?.Invoke();
        // Futuro: tela de game over, opcao de continuar ou voltar ao menu
    }
}
