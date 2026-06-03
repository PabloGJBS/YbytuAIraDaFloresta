using UnityEngine;

/// <summary>
/// Configuracoes de um tipo de inimigo.
/// Criar um SO para cada tipo (Punk, Boss, Ninja, etc).
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Identidade")]
    public string enemyName;

    [Header("Vida")]
    public int maxHealth = 30;

    [Header("Movimento")]
    public float moveSpeed = 2f;
    public float patrolRadius = 3f;
    public float patrolWaitTime = 1.5f;

    [Header("Deteccao")]
    public float detectionRange = 5f;
    public float loseTargetRange = 8f;

    [Header("Combate")]
    public int attackDamage = 10;
    public float attackRange = 0.6f;
    [Tooltip("Diferenca maxima em Y para que o inimigo possa atacar (beat'em up: precisa estar na mesma 'lane').")]
    public float attackYTolerance = 0.4f;
    public float attackCooldown = 1.5f;
    [Tooltip("Tempo (s) que o inimigo fica sem poder atacar depois de levar dano. Da janela pro combo do player.")]
    public float hurtRecoveryTime = 0.8f;
    [Tooltip("Lista de triggers do Animator a sortear em cada ataque. Default 'Attack' (Punch). Inimigos com variantes incluem Jab/Kick - esses slots ficam mapeados pra Attack2/Attack3 (ou Shot/Recharge no boss) via AnimatorOverrideController.")]
    public string[] attackTriggers = { "Attack" };

    [Header("Tipo")]
    [Tooltip("Marca este inimigo como chefe: dispara a BGM de boss quando entra numa wave.")]
    public bool isBoss;
    [Tooltip("Se true, o ataque toca o som de tiro (enemyGunshot) ao inves do soco. Chefe1 com arma de fogo.")]
    public bool usesGunshotSfx;
    [Tooltip("Frases gritadas quando o inimigo surge na arena (sorteadas). Vazio = sem grito. Usar so em chefes.")]
    public string[] spawnBarks;
    [Tooltip("Frases gritadas ao disparar o tiro em linha do combo-break (sorteadas). Boss.")]
    public string[] shotBarks;

    [Header("Pontuacao")]
    public int scoreValue = 100;

    [Header("IA")]
    public EnemyBehaviorType behaviorType = EnemyBehaviorType.Aggressive;
}

public enum EnemyBehaviorType
{
    Aggressive,
    Defensive,
    Patrol
}
