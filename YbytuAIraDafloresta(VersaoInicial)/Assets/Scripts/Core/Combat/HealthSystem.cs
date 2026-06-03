using UnityEngine;
using System;

/// <summary>
/// Sistema de vida generico. Usado pelo player e inimigos.
/// </summary>
public class HealthSystem : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private bool destroyOnDeath = false;

    [Header("Invencibilidade apos dano")]
    [SerializeField] private float invincibilityDuration = 0.5f;

    private int currentHealth;
    private bool isDead;
    private float invincibilityTimer;

    // Propriedades publicas
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float HealthPercent => (float)currentHealth / maxHealth;
    public bool IsDead => isDead;
    public bool IsInvincible => invincibilityTimer > 0f;

    // Eventos
    public event Action<int, int> OnHealthChanged;      // current, max
    public event Action<int> OnDamageTaken;              // damage amount
    public event Action<int> OnHealed;                   // heal amount
    public event Action OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (invincibilityTimer > 0f)
            invincibilityTimer -= Time.deltaTime;
    }

    /// <summary>
    /// Aplicar dano. Retorna o dano real aplicado.
    /// </summary>
    public int TakeDamage(int baseDamage, float multiplier = 1f)
    {
        if (isDead || IsInvincible) return 0;

        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * multiplier));
        currentHealth = Mathf.Max(0, currentHealth - finalDamage);
        invincibilityTimer = invincibilityDuration;

        OnDamageTaken?.Invoke(finalDamage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
            Die();

        return finalDamage;
    }

    /// <summary>
    /// Curar vida.
    /// </summary>
    public void Heal(int amount)
    {
        if (isDead) return;

        int before = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        int healed = currentHealth - before;

        if (healed > 0)
        {
            OnHealed?.Invoke(healed);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    /// <summary>
    /// Define a vida maxima (ex.: vinda do EnemyData). refill=true enche a vida.
    /// </summary>
    public void SetMaxHealth(int newMax, bool refill = true)
    {
        maxHealth = Mathf.Max(1, newMax);
        currentHealth = refill ? maxHealth : Mathf.Min(currentHealth, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    /// <summary>
    /// Restaurar vida completa (revive, novo round, etc).
    /// </summary>
    public void FullRestore()
    {
        isDead = false;
        currentHealth = maxHealth;
        invincibilityTimer = 0f;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        isDead = true;
        OnDeath?.Invoke();

        if (destroyOnDeath)
            Destroy(gameObject, 1f);
    }
}
