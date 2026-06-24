using UnityEngine;
using System;

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

    private void Start()
    {
        if (GameFlowManager.Instance != null)
        {
            currentLives = GameFlowManager.Instance.PlayerLives;
            OnLivesChanged?.Invoke(currentLives);
        }
    }

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

    public void AddLife()
    {
        if (currentLives < maxLives)
        {
            currentLives++;
            OnLivesChanged?.Invoke(currentLives);
        }
    }

    public void ResetLives()
    {
        currentLives = startingLives;
        OnLivesChanged?.Invoke(currentLives);
    }
}
