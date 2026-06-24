using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Totem interativo: na PRIMEIRA interacao (player perto + tecla Espaco/E), devolve
/// energia espiritual (cura uma fracao da vida) e dispara a revoada de passaros.
/// Detecta o player por DISTANCIA (nao precisa de collider). Posicione este objeto
/// em cima do totem e ajuste interactRange (gizmo = esfera azul).
/// </summary>
public class TotemInteraction : MonoBehaviour
{
    [Header("Cura (energia espiritual)")]
    [Range(0f, 1f)] public float healPercent = 0.30f;
    public bool healOnce = true;

    [Header("Alcance")]
    [Tooltip("Distancia em X pra poder interagir.")]
    public float interactRange = 3.5f;
    public string playerTag = "Player";

    [Header("Prompt")]
    public string promptText = "Espaço para interagir";
    public float promptHeight = 2.5f;

    [Header("Revoada (opcional)")]
    [Tooltip("Revoada disparada ao interagir pela primeira vez.")]
    public BirdFlock birdFlock;

    private bool used;
    private bool playerInside;
    private GameObject promptGo;
    private InteractableHighlight highlight;
    private Transform player;

    private void Awake()
    {
        highlight = GetComponentInParent<InteractableHighlight>();

        // Seguro: se a revoada nao foi ligada no Inspector, acha ela sozinho na cena.
        if (birdFlock == null)
        {
            birdFlock = FindFirstObjectByType<BirdFlock>(FindObjectsInactive.Include);
            if (birdFlock != null)
                Debug.Log($"[Totem] birdFlock nao estava ligado; achei '{birdFlock.name}' na cena.", this);
        }
    }

    private void Update()
    {
        if (healOnce && used) return;
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go == null) return;
            player = go.transform;
        }

        bool near = Mathf.Abs(player.position.x - transform.position.x) <= interactRange;
        if (near != playerInside)
        {
            playerInside = near;
            ShowPrompt(near);
            SetHighlight(near);
            if (near) Debug.Log($"[Totem] Player no alcance (player x={player.position.x:0.0}, totem x={transform.position.x:0.0}, range={interactRange}). Aperte Espaco.", this);
        }

        var kbd = Keyboard.current;
        var gpd = Gamepad.current;
        bool pressed = (kbd != null && (kbd.spaceKey.wasPressedThisFrame || kbd.eKey.wasPressedThisFrame))
                    || (gpd != null && gpd.buttonNorth.wasPressedThisFrame);

        if (pressed)
        {
            if (playerInside) Fire();
            else Debug.Log($"[Totem] Apertou interagir FORA do alcance: dist={Mathf.Abs(player.position.x - transform.position.x):0.0} (range={interactRange}, totem x={transform.position.x:0.0}). Chegue mais perto.", this);
        }
    }

    private void Fire()
    {
        if (healOnce) used = true;
        ShowPrompt(false);
        SetHighlight(false);

        var hp = player != null ? player.GetComponentInChildren<HealthSystem>() : null;
        if (hp != null && healPercent > 0f)
        {
            int amount = Mathf.RoundToInt(hp.MaxHealth * healPercent);
            if (amount > 0) hp.Heal(amount);
        }

        Debug.Log($"[Totem] Interagido em x={transform.position.x:0.0}. Curou={hp != null}. birdFlock={(birdFlock != null ? birdFlock.name : "NULL (nao ligado!)")}", this);
        if (birdFlock != null) birdFlock.Trigger();
        else Debug.LogWarning("[Totem] birdFlock NAO esta ligado: a revoada nao vai disparar.", this);
    }

    private void SetHighlight(bool on)
    {
        if (highlight != null) highlight.SetHighlighted(on);
    }

    private void ShowPrompt(bool on)
    {
        if (on)
        {
            if (promptGo != null) return;
            promptGo = new GameObject("InteractPrompt", typeof(TextMeshPro));
            promptGo.transform.position = transform.position + Vector3.up * promptHeight;
            var tmp = promptGo.GetComponent<TextMeshPro>();
            tmp.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            tmp.text = promptText;
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
        Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
#if UNITY_EDITOR
        UnityEditor.Handles.color = Gizmos.color;
        UnityEditor.Handles.Label(transform.position + Vector3.up * (promptHeight + 0.3f), "Totem");
#endif
    }
}
