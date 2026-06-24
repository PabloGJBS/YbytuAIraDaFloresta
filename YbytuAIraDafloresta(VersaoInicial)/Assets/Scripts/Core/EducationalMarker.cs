using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

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

    [Header("Extras opcionais (ex.: totem)")]
    [Tooltip("Se ligado, a MESMA interacao que mostra o texto tambem dispara esta revoada (uma vez).")]
    public BirdFlock birdFlockOnInteract;
    [Tooltip("Se > 0, ao interagir devolve essa fracao da vida do player (energia espiritual do totem).")]
    [Range(0f, 1f)] public float healPercentOnInteract = 0f;

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
        EducationalBanner.Show(phrase, holdSeconds, requireInteraction);

        if (healPercentOnInteract > 0f)
        {
            var playerGo = GameObject.FindGameObjectWithTag(playerTag);
            var hp = playerGo != null ? playerGo.GetComponentInChildren<HealthSystem>() : null;
            if (hp != null)
            {
                int amount = Mathf.RoundToInt(hp.MaxHealth * healPercentOnInteract);
                if (amount > 0) hp.Heal(amount);
            }
        }
        if (birdFlockOnInteract != null) birdFlockOnInteract.Trigger();
    }

    private void ShowPrompt(bool on)
    {
        if (on)
        {
            if (promptGo != null) return;
            promptGo = new GameObject("ReadPrompt", typeof(TextMeshPro));
            promptGo.transform.position = transform.position + Vector3.up * 2.2f;
            var tmp = promptGo.GetComponent<TextMeshPro>();
            tmp.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            tmp.text = "Espaço para ler";
            tmp.fontSize = 2.8f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.85f, 1f, 0.65f);
            tmp.outlineWidth = 0.22f;
            tmp.outlineColor = new Color32(0, 0, 0, 255);
            tmp.sortingOrder = 1000;
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
