using UnityEngine;
using System;

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

    public int CalculateAttackDamage(int baseDamage)
    {
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * combo.DamageMultiplier));
    }

    public void ReceiveDamage(int baseDamage)
    {
        if (DebugFlags.Godmode) return;

        if (playerController != null && playerController.IsInvulnerable)
            return;

        float multiplier = combo.DamageTakenMultiplier;
        health.TakeDamage(baseDamage, multiplier);
        combo.OnPlayerHurt();

        if (playerController != null)
            playerController.TakeHit();
    }

    private void HandleDamageTaken(int damage)
    {
    }

    private void HandleDeath()
    {
        OnPlayerDied?.Invoke();
        Invoke(nameof(HandleGameOver), 0.6f);
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
    }
}
