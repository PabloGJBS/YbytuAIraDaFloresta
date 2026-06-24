using UnityEngine;

/// <summary>
/// Gate do tutorial da Fase 1. Garante que o player aprenda os golpes (J K L)
/// ANTES de entrar no primeiro combate.
///
/// Funciona sem nenhuma referencia ligada na cena:
/// - O gate eh "armado" assim que QUALQUER fala da arara dispara (ou seja, estamos
///   numa fase com tutorial). Fases sem DialogueTriggerLine nunca armam o gate.
/// - Enquanto o gate esta armado e os golpes ainda nao foram ensinados, a primeira
///   zona de combate que o player alcanca primeiro ENSINA a fala dos golpes (a arara
///   fala) e so depois inicia a luta. Assim o player nunca luta sem saber atacar,
///   independentemente da posicao exata das linhas de fala.
///
/// Estado estatico, resetado a cada Play (SubsystemRegistration roda mesmo com o
/// domain reload desligado).
/// </summary>
public static class TutorialGate
{
    /// <summary>True quando estamos numa fase com tutorial (alguma fala ja disparou).</summary>
    public static bool GateArmed { get; private set; }

    /// <summary>True depois que a fala dos golpes (J K L) ja foi entregue.</summary>
    public static bool GolpeTaught { get; private set; }

    /// <summary>Secao de localizacao com as falas do tutorial (scene_01, scene_02 ...).</summary>
    public const string TutorialSection = "cutscene.stage_01_tutorial";

    /// <summary>Numero da fala que ensina os golpes (J K L). Mesmo numero do DialogueTriggerLine.</summary>
    public const int GolpeDialogueNumber = 7;

    /// <summary>A zona de combate deve ensinar os golpes antes de comecar?</summary>
    public static bool ShouldTeachGolpe => GateArmed && !GolpeTaught;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        GateArmed = false;
        GolpeTaught = false;
    }

    /// <summary>Arma o gate: estamos numa fase com tutorial (chamado quando a arara fala).</summary>
    public static void Arm() => GateArmed = true;

    /// <summary>Marca os golpes como ensinados (libera o combate). Tambem arma o gate.</summary>
    public static void TeachGolpe()
    {
        GateArmed = true;
        GolpeTaught = true;
    }

    /// <summary>Texto localizado da fala dos golpes, ou null se indisponivel.</summary>
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
