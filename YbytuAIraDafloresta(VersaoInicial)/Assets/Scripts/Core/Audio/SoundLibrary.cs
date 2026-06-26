using UnityEngine;

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

    [Header("Chefe Trator (Fase 2)")]
    [Tooltip("Motor/investida do trator.")]
    public AudioClip tractorEngine;
    [Tooltip("Clangor de metal quando o player acerta o trator (superaquecido).")]
    public AudioClip tractorHitMetal;
    [Tooltip("Som de superaquecimento (vapor/alarme) ao abrir a janela vulneravel.")]
    public AudioClip tractorOverheat;
    [Tooltip("Explosoes (verticais, transicao, morte). Tocadas a ~60% do volume. Sorteia uma.")]
    public AudioClip[] explosions;

    [Header("Arara (companheira) - Fases 1 e 2")]
    [Tooltip("Som curto quando a arara fala (por linha de dialogo / aviso).")]
    public AudioClip araraTalk;

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
