using UnityEngine;
using System.IO;

/// <summary>
/// Gerencia os 3 slots de save do jogo.
/// Salva/carrega em JSON no persistentDataPath.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public const int MaxSlots = 3;

    private static SaveManager instance;
    public static SaveManager Instance => instance;

    private SaveData currentSave;
    public SaveData CurrentSave => currentSave;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool HasSave(int slot)
    {
        return File.Exists(GetSavePath(slot));
    }

    public int GetSaveCount()
    {
        int count = 0;
        for (int i = 0; i < MaxSlots; i++)
            if (HasSave(i)) count++;
        return count;
    }

    /// <summary>
    /// Retorna o slot do save mais recente (pelo lastPlayedAt), ou -1 se nenhum existe.
    /// </summary>
    public int GetMostRecentSlot()
    {
        int bestSlot = -1;
        System.DateTime bestDate = System.DateTime.MinValue;

        for (int i = 0; i < MaxSlots; i++)
        {
            if (!HasSave(i)) continue;

            string json = File.ReadAllText(GetSavePath(i));
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) continue;

            if (System.DateTime.TryParse(data.lastPlayedAt, out var dt) && dt > bestDate)
            {
                bestDate = dt;
                bestSlot = i;
            }
            else if (bestSlot == -1)
            {
                bestSlot = i;
            }
        }

        return bestSlot;
    }

    public SaveData LoadSave(int slot)
    {
        string path = GetSavePath(slot);
        if (!File.Exists(path)) return null;

        string json = File.ReadAllText(path);
        currentSave = JsonUtility.FromJson<SaveData>(json);
        return currentSave;
    }

    public void CreateNewSave(int slot, string playerName = "")
    {
        currentSave = new SaveData(slot);
        currentSave.playerName = playerName;
        WriteSave();
    }

    public void SelectSave(int slot)
    {
        if (HasSave(slot))
            LoadSave(slot);
        else
            CreateNewSave(slot);
    }

    public void WriteSave()
    {
        if (currentSave == null) return;
        currentSave.lastPlayedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        string json = JsonUtility.ToJson(currentSave, true);
        File.WriteAllText(GetSavePath(currentSave.saveSlot), json);
    }

    public void DeleteSave(int slot)
    {
        string path = GetSavePath(slot);
        if (File.Exists(path))
            File.Delete(path);

        if (currentSave != null && currentSave.saveSlot == slot)
            currentSave = null;
    }

    public StageProgress GetStageProgress(int stageIndex)
    {
        if (currentSave == null) return null;
        return currentSave.stageProgress.Find(s => s.stageIndex == stageIndex);
    }

    public void SaveStageResult(int stageIndex, int score, float time)
    {
        if (currentSave == null) return;

        var progress = GetStageProgress(stageIndex);
        if (progress == null)
        {
            progress = new StageProgress(stageIndex);
            currentSave.stageProgress.Add(progress);
        }

        progress.completed = true;
        progress.timesPlayed++;

        if (score > progress.bestScore)
        {
            progress.bestScore = score;
            progress.rankGrade = CalculateRank(score);
        }

        if (progress.bestTime <= 0 || time < progress.bestTime)
            progress.bestTime = time;

        WriteSave();
    }

    public void MarkIntroWatched()
    {
        if (currentSave == null) return;
        currentSave.introWatched = true;
        WriteSave();
    }

    private string CalculateRank(int score)
    {
        if (score >= 9000) return "S";
        if (score >= 7000) return "A";
        if (score >= 5000) return "B";
        if (score >= 3000) return "C";
        return "D";
    }

    private string GetSavePath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, $"save_slot_{slot}.json");
    }
}
