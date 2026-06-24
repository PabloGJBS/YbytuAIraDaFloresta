using UnityEngine;

/// <summary>
/// Marcador na Scene View (linha vertical + label, no estilo do "Start" do StageZone)
/// que dispara UMA fala da arara quando o player cruza esta posicao X andando pra
/// frente. Coloque varios na fase, um por fala, e ajuste o X arrastando.
/// </summary>
public class DialogueTriggerLine : MonoBehaviour
{
    [Tooltip("Numero da fala (1 = scene_01, 2 = scene_02 ...).")]
    [SerializeField] private int dialogueNumber = 1;
    [Tooltip("Quantas falas seguidas disparar a partir desta, em sequencia (sem o player precisar andar). Ex: 2 = esta fala + a proxima.")]
    [SerializeField] private int dialogueCount = 1;
    [Tooltip("Secao de localizacao com as falas (scene_01, scene_02 ...).")]
    [SerializeField] private string localizationSection = "cutscene.stage_01_tutorial";
    [Tooltip("Cor da linha na Scene View.")]
    [SerializeField] private Color lineColor = new Color(1f, 0.55f, 0.1f);
    [Tooltip("So dispara quando o player passa indo pra direita (avanco).")]
    [SerializeField] private bool requireForwardCross = true;

    private bool fired;
    private Transform player;

    private void Update()
    {
        if (fired) return;
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go == null) return;
            player = go.transform;
        }
        if (player.position.x >= transform.position.x)
            Fire();
    }

    private void Fire()
    {
        fired = true;

        // Qualquer fala disparando significa que estamos na fase com tutorial: arma o
        // gate que impede o player de lutar antes de aprender os golpes.
        TutorialGate.Arm();

        var bubble = AraraSpeechBubble.Instance;
        for (int j = 0; j < Mathf.Max(1, dialogueCount); j++)
        {
            int number = dialogueNumber + j;
            if (bubble != null)
                bubble.Show(ResolveText(number));
            // A fala dos golpes (J K L) foi entregue: libera o combate.
            if (number == TutorialGate.GolpeDialogueNumber)
                TutorialGate.TeachGolpe();
        }
    }

    private string ResolveText(int number)
    {
        var loc = LocalizationManager.Instance;
        if (loc == null) return $"[fala {number}]";
        var lines = loc.GetSection(localizationSection);
        int i = number - 1;
        if (lines != null && i >= 0 && i < lines.Length) return lines[i];
        return $"[fala {number}]";
    }

    private void OnDrawGizmos()
    {
        float x = transform.position.x;
        Gizmos.color = lineColor;
        Gizmos.DrawLine(new Vector3(x, -50f, 0f), new Vector3(x, 50f, 0f));
#if UNITY_EDITOR
        UnityEditor.Handles.color = lineColor;
        UnityEditor.Handles.Label(new Vector3(x, 4.5f, 0f), $"Fala {dialogueNumber}");
#endif
    }
}
