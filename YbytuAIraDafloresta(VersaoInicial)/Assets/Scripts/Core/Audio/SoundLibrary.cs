using UnityEngine;

/// <summary>
/// Biblioteca central de referencias a AudioClips do jogo.
/// Um unico SO listado no SoundManager, evita refs espalhadas em prefabs.
/// </summary>
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Game/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [Header("Player - Ataques")]
    public AudioClip playerPunch;
    public AudioClip playerKick;
    public AudioClip playerJab;
    public AudioClip playerJumpKick;
    public AudioClip playerDiveKick;

    [Header("Player - Movimento")]
    public AudioClip playerJump;
    public AudioClip playerLand;
    public AudioClip playerStep;

    [Header("Player - Combate sofrido")]
    public AudioClip playerHurt;
    public AudioClip playerDeath;
    public AudioClip playerRevive;

    [Header("Enemy")]
    public AudioClip enemyPunch;
    public AudioClip enemyHurt;
    public AudioClip enemyDeath;
    public AudioClip enemyAlert;
    [Tooltip("Tiro de arma de fogo. Usado por inimigos com EnemyData.usesGunshotSfx (Chefe1).")]
    public AudioClip enemyGunshot;

    [Header("Combate / Combo")]
    public AudioClip hitConnect;
    [Tooltip("Nota tocada ao subir de rank, indexada por ComboRank: [0]=C, [1]=B ... [5]=SSS.")]
    public AudioClip[] comboRankUp;
    public AudioClip comboBreak;

    [Header("UI")]
    public AudioClip uiSelect;
    public AudioClip uiConfirm;
    public AudioClip uiCancel;
    public AudioClip uiPause;
    public AudioClip uiUnpause;
    [Tooltip("Blip de digitacao do typewriter em cutscenes/dialogos.")]
    public AudioClip dialogueType;

    [Header("Stage / Pickup")]
    public AudioClip itemPickup;
    public AudioClip stageClear;
    public AudioClip gameOver;

    [Header("BGM")]
    [Tooltip("Trilha de exploracao (fora de combate).")]
    public AudioClip bgmExploration;
    [Tooltip("Trilha de combate (CombatZone ativa).")]
    public AudioClip bgmCombat;
    [Tooltip("Trilha de chefe (wave com inimigo isBoss).")]
    public AudioClip bgmBoss;
}
