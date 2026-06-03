using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Marcador interativo nos trechos de caminhada. Quando o player entra na area,
/// exibe uma frase educativa (via EducationalBanner). Pode ser automatico (aparece
/// ao passar) ou interativo (aparece quando o player aperta a tecla). Mostra uma vez.
///
/// Uso: GameObject vazio no caminho + BoxCollider2D (isTrigger) + este script.
/// Preencher "phrase" no Inspector com a frase educativa.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EducationalMarker : MonoBehaviour
{
    [Header("Conteudo")]
    [TextArea(2, 4)] public string phrase = "";
    [Tooltip("Sorteia uma das 7 frases aprovadas (sem repetir entre marcadores).")]
    public bool useRandomFromPool = false;

    [Header("Comportamento")]
    [Tooltip("Auto = aparece ao passar. Interativo = so quando apertar a tecla.")]
    public bool requireInteraction = false;
    [Tooltip("Dispara apenas uma vez.")]
    public bool showOnce = true;
    [Tooltip("Tempo na tela (auto). 0 = calculado pelo tamanho da frase.")]
    public float holdSeconds = 0f;

    [Header("Player")]
    public string playerTag = "Player";

    private bool used;
    private bool playerInside;
    private GameObject promptGo;
    private InteractableHighlight highlight;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        highlight = GetComponentInParent<InteractableHighlight>();
    }

    private void Start()
    {
        if (useRandomFromPool)
            phrase = EducationalPhrasePool.Take();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (showOnce && used) return;

        playerInside = true;
        SetHighlight(true);
        if (requireInteraction)
            ShowPrompt(true);
        else
            Fire();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = false;
        ShowPrompt(false);
        SetHighlight(false);
    }

    private void SetHighlight(bool on)
    {
        if (highlight != null) highlight.SetHighlighted(on);
    }

    private void Update()
    {
        if (!requireInteraction || !playerInside || (showOnce && used)) return;

        var kb = Keyboard.current;
        var gp = Gamepad.current;
        bool pressed = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame))
                    || (gp != null && gp.buttonNorth.wasPressedThisFrame);
        if (pressed)
        {
            ShowPrompt(false);
            Fire();
        }
    }

    private void Fire()
    {
        used = true;
        SetHighlight(false); // ja interagiu: apaga o brilho
        EducationalBanner.Show(phrase, holdSeconds);
    }

    private void ShowPrompt(bool on)
    {
        if (on)
        {
            if (promptGo != null) return;
            promptGo = new GameObject("ReadPrompt", typeof(TextMeshPro));
            promptGo.transform.position = transform.position + Vector3.up * 1.6f;
            var tmp = promptGo.GetComponent<TextMeshPro>();
            tmp.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            tmp.text = "Espaço para ler";
            tmp.fontSize = 2.2f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.7f, 1f, 0.6f);
            tmp.sortingOrder = 60;
        }
        else if (promptGo != null)
        {
            Destroy(promptGo);
            promptGo = null;
        }
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return;
        Gizmos.color = new Color(0.4f, 0.9f, 0.4f, 0.25f);
        Gizmos.DrawCube(transform.position + (Vector3)col.offset, col.bounds.size);
    }
}
