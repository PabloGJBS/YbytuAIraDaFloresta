using System;
using System.Collections.Generic;

/// <summary>
/// Dados de um save slot.
/// Serializado em JSON para persistencia.
/// </summary>
[Serializable]
public class SaveData
{
    public string playerName;
    public int saveSlot;
    public string createdAt;
    public string lastPlayedAt;
    public float totalPlayTime;
    public bool introWatched;
    public int lastStageIndex;
    public List<StageProgress> stageProgress = new List<StageProgress>();

    public SaveData(int slot)
    {
        saveSlot = slot;
        playerName = "";
        createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        lastPlayedAt = createdAt;
        totalPlayTime = 0f;
        introWatched = false;
        lastStageIndex = 0;
        stageProgress = new List<StageProgress>();
    }
}

/// <summary>
/// Progresso e pontuacao de uma fase especifica.
/// </summary>
[Serializable]
public class StageProgress
{
    public int stageIndex;
    public bool completed;
    public int bestScore;
    public float bestTime;
    public string rankGrade;
    public int timesPlayed;

    public StageProgress(int index)
    {
        stageIndex = index;
        completed = false;
        bestScore = 0;
        bestTime = 0f;
        rankGrade = "";
        timesPlayed = 0;
    }
}
