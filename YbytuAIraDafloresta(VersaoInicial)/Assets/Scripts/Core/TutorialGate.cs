using UnityEngine;

public static class TutorialGate
{
    public static bool GateArmed { get; private set; }

    public static bool GolpeTaught { get; private set; }

    public const string TutorialSection = "cutscene.stage_01_tutorial";

    public const int GolpeDialogueNumber = 7;

    public static bool ShouldTeachGolpe => GateArmed && !GolpeTaught;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        GateArmed = false;
        GolpeTaught = false;
    }

    public static void Arm() => GateArmed = true;

    public static void TeachGolpe()
    {
        GateArmed = true;
        GolpeTaught = true;
    }

    public static string ResolveGolpeLine()
    {
        var loc = LocalizationManager.Instance;
        if (loc == null) return null;
        var lines = loc.GetSection(TutorialSection);
        int i = GolpeDialogueNumber - 1;
        if (lines != null && i >= 0 && i < lines.Length) return lines[i];
        return null;
    }
}
