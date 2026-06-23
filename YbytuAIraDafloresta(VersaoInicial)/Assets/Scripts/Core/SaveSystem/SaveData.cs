using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public string playerName;
    public int saveSlot;
    public string createdAt;
    public string lastPlayedAt;
    public float totalPlayTime;
    public bool introWatched;
    public int lastStageIndex;          // fase onde o player parou
    public int lastZoneIndex;
    public int carryOverScore;
    public int livesRemaining = 3;      // vidas restantes
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
        lastZoneIndex = 0;
        carryOverScore = 0;
        livesRemaining = 3;
        stageProgress = new List<StageProgress>();
    }
}

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
