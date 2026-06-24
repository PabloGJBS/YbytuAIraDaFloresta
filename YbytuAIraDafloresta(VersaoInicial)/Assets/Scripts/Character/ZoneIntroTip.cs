using UnityEngine;
using System.Collections;

/// <summary>
/// Ao ATIVAR uma CombatZone, a arara fala uma DICA antes de voar embora; enquanto ela fala,
/// os inimigos ficam congelados (nao avancam). Usado nas zonas 1 (combo) e 2 (inimigo
/// enfurecido). Coloque um por zona, ligue a CombatZone e a chave de localizacao.
/// </summary>
public class ZoneIntroTip : MonoBehaviour
{
    [Tooltip("A zona de combate cuja ativacao dispara a dica.")]
    public CombatZone zone;
    [Tooltip("Chave de localizacao da dica (ex.: arara_tips.zona1). Vazio = usa o texto literal.")]
    public string tipLocalizationKey;
    [TextArea(2, 4)] public string tipText = "";

    private bool fired;

    private void Start()
    {
        if (zone != null) zone.OnZoneActivated += OnActivated;
    }

    private void OnDestroy()
    {
        if (zone != null) zone.OnZoneActivated -= OnActivated;
    }

    private void OnActivated(CombatZone z)
    {
        if (fired) return;
        fired = true;
        StartCoroutine(ShowTip());
    }

    private IEnumerator ShowTip()
    {
        var bubble = AraraSpeechBubble.Instance;
        string text = Resolve();
        if (bubble == null || string.IsNullOrEmpty(text)) yield break;

        // A arara segura a fuga (fala antes de voar) e os inimigos param.
        var companion = bubble.GetComponent<CompanionFollow>();
        if (companion != null) companion.HoldFlee(true);
        EnemyController.CombatFrozen = true;

        bubble.Show(text);
        yield return null;
        float timeout = 10f;
        while (bubble.IsBusy && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        // Acabou a dica: inimigos avancam e a arara voa.
        EnemyController.CombatFrozen = false;
        if (companion != null) companion.HoldFlee(false);
    }

    private string Resolve()
    {
        if (!string.IsNullOrEmpty(tipLocalizationKey))
        {
            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                string s = loc.GetText(tipLocalizationKey);
                if (!string.IsNullOrEmpty(s) && !s.StartsWith("[")) return s;
            }
        }
        return tipText;
    }
}
