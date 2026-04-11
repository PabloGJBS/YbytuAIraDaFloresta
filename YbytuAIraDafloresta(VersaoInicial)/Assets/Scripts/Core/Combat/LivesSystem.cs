using UnityEngine;
using System;

/// <summary>
/// Sistema de tentativas (vidas extras) do jogador.
/// Ao morrer, gasta uma tentativa para reviver.
/// Sem tentativas = Game Over.
/// </summary>
public class LivesSystem : MonoBehaviour
{
    [Header("Tentativas")]
    [SerializeField] private int startingLives = 3;
    [SerializeField] private int maxLives = 5;

    [Header("Revive")]
    [SerializeField] private float reviveInvincibilityDuration = 2f;

    private int currentLives;

    // Propriedades publicas
    public int CurrentLives => currentLives;
    public int MaxLives => maxLives;
    public bool HasLivesRemaining => currentLives > 0;

    // Eventos
    public event Action<int> OnLivesChanged;        // lives remaining
    public event Action OnRevive;
    public event Action OnGameOver;

    private void Awake()
    {
        currentLives = startingLives;
    }

    /// <summary>
    /// Tenta usar uma tentativa para reviver. Retorna true se conseguiu.
    /// </summary>
    public bool TryRevive(HealthSystem health)
    {
        if (currentLives <= 0)
        {
            OnGameOver?.Invoke();
            return false;
        }

        currentLives--;
        OnLivesChanged?.Invoke(currentLives);

        if (health != null)
            health.FullRestore();

        OnRevive?.Invoke();
        return true;
    }

    /// <summary>
    /// Adicionar tentativa extra (power-up, bonus, etc).
    /// </summary>
    public void AddLife()
    {
        if (currentLives < maxLives)
        {
            currentLives++;
            OnLivesChanged?.Invoke(currentLives);
        }
    }

    /// <summary>
    /// Resetar tentativas para o valor inicial.
    /// </summary>
    public void ResetLives()
    {
        currentLives = startingLives;
        OnLivesChanged?.Invoke(currentLives);
    }
}
