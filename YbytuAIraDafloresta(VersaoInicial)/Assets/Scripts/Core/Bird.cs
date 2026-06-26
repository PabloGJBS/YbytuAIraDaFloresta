using UnityEngine;

public class Bird : MonoBehaviour
{
    [Tooltip("Velocidade de voo (x negativo = pra esquerda).")]
    public Vector2 velocity = new Vector2(-7f, 0.3f);
    public float bobAmplitude = 0.35f;
    public float bobFrequency = 5f;
    [Tooltip("Segundos ate sumir (ja deve ter saido da tela).")]
    public float lifetime = 8f;
    [Tooltip("Marque se o sprite original aponta pra direita.")]
    public bool spriteFacesRight = true;
    [Tooltip("Nome do estado de voo no Animator.")]
    public string flyStateName = "Fly";

    [Header("Limite (some ao cruzar a linha)")]
    [Tooltip("Se true, o passaro some ao cruzar despawnX (definido pelo BirdFlock). Senao, some por tempo (lifetime).")]
    public bool useDespawnX = false;
    public float despawnX = 0f;

    private SpriteRenderer sr;
    private float baseY;
    private float phase;

    public void Launch(Vector2 vel)
    {
        velocity = vel;
        ApplyFacing();
    }

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        baseY = transform.position.y;
        phase = transform.position.x * 0.7f;
    }

    private void Start()
    {
        ApplyFacing();
        var anim = GetComponentInChildren<Animator>();
        if (anim != null && !string.IsNullOrEmpty(flyStateName))
        {
            float offset = Mathf.Repeat(Mathf.Abs(transform.position.x) * 0.37f, 1f);
            anim.Play(flyStateName, 0, offset); // dessincroniza as asas
        }
        Destroy(gameObject, lifetime);
    }

    private void ApplyFacing()
    {
        if (sr == null) return;
        bool goingLeft = velocity.x < 0f;
        sr.flipX = spriteFacesRight ? goingLeft : !goingLeft;
    }

    private void Update()
    {
        var p = transform.position;
        p.x += velocity.x * Time.deltaTime;
        baseY += velocity.y * Time.deltaTime;
        phase += bobFrequency * Time.deltaTime;
        p.y = baseY + Mathf.Sin(phase) * bobAmplitude;
        transform.position = p;

        if (useDespawnX && ((velocity.x < 0f && p.x <= despawnX) || (velocity.x > 0f && p.x >= despawnX)))
            Destroy(gameObject);
    }
}
