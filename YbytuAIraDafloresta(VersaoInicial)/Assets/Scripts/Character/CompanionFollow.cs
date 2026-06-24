using UnityEngine;

/// <summary>
/// Faz um companheiro (ex.: a arara) seguir o player voando, com atraso suave.
/// Fica atras do player (lado oposto ao que ele esta virado) e um pouco acima,
/// com um leve balanco vertical pra dar sensacao de voo. Nao colide nem bloqueia
/// o player: mexe so no Transform.
///
/// Durante uma zona de combate (CombatZone.OnAnyZoneActivated) ela voa pra longe
/// (pra cima, saindo da tela) e volta voando quando a zona e limpa
/// (CombatZone.OnAnyZoneCompleted).
/// </summary>
public class CompanionFollow : MonoBehaviour
{
    [Header("Alvo")]
    [Tooltip("Quem seguir. Se vazio, procura pela tag.")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";

    [Header("Posicao relativa")]
    [Tooltip("X = distancia do player; Y = altura acima dele.")]
    [SerializeField] private Vector2 followOffset = new Vector2(1.6f, 1.2f);
    [Tooltip("Fica na FRENTE do player (lado pra onde ele olha) em vez de atras.")]
    [SerializeField] private bool stayInFront = false;
    [Tooltip("Tempo de amortecimento do follow (maior = mais lento/folgado).")]
    [SerializeField] private float smoothTime = 0.28f;
    [SerializeField] private float maxSpeed = 30f;

    [Header("Voo (balanco)")]
    [SerializeField] private float bobAmplitude = 0.25f;
    [SerializeField] private float bobFrequency = 2.5f;

    [Header("Fugir no combate")]
    [Tooltip("Pra onde a arara voa quando o combate comeca (relativo ao player). Y alto = sai pela parte de cima da tela.")]
    [SerializeField] private Vector2 fleeOffset = new Vector2(2.5f, 13f);
    [Tooltip("Amortecimento ao fugir (menor = sai mais rapido).")]
    [SerializeField] private float fleeSmoothTime = 0.2f;
    [Tooltip("Esconde o sprite quando ja saiu de cena durante o combate.")]
    [SerializeField] private bool hideWhileFleeing = true;
    [Tooltip("Distancia vertical a partir da qual considera que ja sumiu.")]
    [SerializeField] private float hideDistance = 9f;

    [Header("Direcao do sprite")]
    [Tooltip("Arara olha pro mesmo lado do player.")]
    [SerializeField] private bool matchTargetFacing = true;
    [Tooltip("Marque se o sprite original aponta pra direita.")]
    [SerializeField] private bool spriteFacesRightByDefault = true;

    private SpriteRenderer targetSprite;
    private SpriteRenderer selfSprite;
    private Vector3 velocity;
    private bool snapped;
    private bool combatActive;
    private bool fleeHeld;

    /// <summary>Segura a fuga: a arara continua seguindo o player mesmo no combate (pra falar antes de voar).</summary>
    public void HoldFlee(bool hold) => fleeHeld = hold;

    private void Awake()
    {
        selfSprite = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        snapped = false; // re-snap se reativado depois de teleporte
        CombatZone.OnAnyZoneActivated += HandleCombatStarted;
        CombatZone.OnAnyZoneCompleted += HandleCombatEnded;
    }

    private void OnDisable()
    {
        CombatZone.OnAnyZoneActivated -= HandleCombatStarted;
        CombatZone.OnAnyZoneCompleted -= HandleCombatEnded;
    }

    private void HandleCombatStarted() => combatActive = true;
    private void HandleCombatEnded() => combatActive = false;

    public void SetTarget(Transform t)
    {
        target = t;
        targetSprite = t != null ? t.GetComponentInChildren<SpriteRenderer>() : null;
        snapped = false;
    }

    private void EnsureTarget()
    {
        if (target != null) return;
        if (string.IsNullOrEmpty(targetTag)) return;
        var go = GameObject.FindGameObjectWithTag(targetTag);
        if (go != null) SetTarget(go.transform);
    }

    private void LateUpdate()
    {
        EnsureTarget();
        if (target == null) return;

        // lado oposto ao facing do player: +1 = player olha direita
        float facingSign = 1f;
        if (targetSprite != null) facingSign = targetSprite.flipX ? -1f : 1f;

        // A fuga pode estar SEGURADA (HoldFlee) enquanto a arara fala uma dica antes de voar.
        bool fleeing = combatActive && !fleeHeld;

        Vector3 desired;
        float t;
        if (fleeing)
        {
            // foge pra cima/lado, saindo da tela
            desired = new Vector3(
                target.position.x + fleeOffset.x,
                target.position.y + fleeOffset.y,
                transform.position.z);
            t = fleeSmoothTime;
        }
        else
        {
            float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            float sideSign = stayInFront ? 1f : -1f;
            desired = new Vector3(
                target.position.x + sideSign * facingSign * followOffset.x,
                target.position.y + followOffset.y + bob,
                transform.position.z);
            t = smoothTime;
        }

        if (!snapped && !fleeing)
        {
            transform.position = desired;
            velocity = Vector3.zero;
            snapped = true;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(
                transform.position, desired, ref velocity, t, maxSpeed);
        }

        UpdateVisibility(fleeing);
        UpdateFacing(facingSign);
    }

    private void UpdateVisibility(bool fleeing)
    {
        if (selfSprite == null || !hideWhileFleeing) return;
        bool farAway = fleeing &&
            Mathf.Abs(transform.position.y - target.position.y) >= hideDistance;
        if (selfSprite.enabled == farAway) selfSprite.enabled = !farAway;
    }

    private void UpdateFacing(float playerFacingSign)
    {
        if (selfSprite == null) return;
        bool lookRight = matchTargetFacing ? playerFacingSign > 0f : velocity.x >= 0f;
        selfSprite.flipX = spriteFacesRightByDefault ? !lookRight : lookRight;
    }
}
