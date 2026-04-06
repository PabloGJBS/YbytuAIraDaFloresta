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

    [Header("Combate / Combo")]
    public AudioClip hitConnect;
    public AudioClip comboRankUp;
    public AudioClip comboBreak;

    [Header("UI")]
    public AudioClip uiSelect;
    public AudioClip uiConfirm;
    public AudioClip uiCancel;
    public AudioClip uiPause;
    public AudioClip uiUnpause;

    [Header("Stage / Pickup")]
    public AudioClip itemPickup;
    public AudioClip stageClear;
    public AudioClip gameOver;
}
