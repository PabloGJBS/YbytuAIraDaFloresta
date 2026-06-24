using UnityEngine;

/// <summary>
/// Fuga de coelhos: espelha o esquema da revoada de passaros (BirdFlock), mas no chao.
/// Spawna alguns coelhos correndo pra esquerda (fugindo do fogo); UM deles passa mal,
/// fala que esta sem ar, toma dano da fumaca e morre no meio do caminho (vira corpo
/// que fica na cena). Os coelhos sao montados em codigo (SpriteRenderer + Animator +
/// Rabbit), entao basta ligar o Controller do coelho aqui.
///
/// Disparo: por padrao quando o player cruza esta posicao (autoTriggerOnCross), igual
/// ao gizmo da revoada. Tambem da pra chamar Trigger() de fora (ex.: um totem).
/// </summary>
public class RabbitHerd : MonoBehaviour
{
    [Header("Coelhos")]
    [Tooltip("Controller de animacao do coelho (Idle/Run/Death).")]
    public RuntimeAnimatorController rabbitController;
    [Tooltip("Sprite inicial (opcional; o Animator assume no 1o frame).")]
    public Sprite previewSprite;
    public int rabbitCount = 3;
    [Tooltip("Velocidade da fuga (x negativo = correndo pra esquerda).")]
    public Vector2 runVelocity = new Vector2(-5f, 0f);
    [Tooltip("Espacamento da formacao (cada coelho um pouco mais a frente e ao fundo).")]
    public float spacingX = 1.6f;
    public float spacingY = 0.15f;
    [Tooltip("Os coelhos de tras correm um tico mais devagar.")]
    public float speedVariation = 1f;
    public float rabbitScale = 1f;
    public int sortingOrder = 120;

    [Header("Coelho que passa mal")]
    [Tooltip("Indice (0 = primeiro) do coelho que fala, toma dano e morre. -1 = o ultimo (atrasado).")]
    public int sickIndex = -1;
    [Tooltip("Texto literal da fala (usado se a chave de localizacao estiver vazia).")]
    public string sickLine = "Tá difícil de respirar...";
    [Tooltip("Chave de localizacao da fala (pt/en). Vazio = usa o texto literal.")]
    public string sickLocalizationKey = "world.fauna.coelho_sufoco";

    [Header("Player / Disparo")]
    public string playerTag = "Player";
    [Tooltip("Se true, dispara sozinho quando o player cruza esta posicao. Se false, so via Trigger().")]
    public bool autoTriggerOnCross = true;

    [Header("Limite (some ao cruzar)")]
    [Tooltip("Ponto arrastavel: os coelhos somem ao cruzar o X deste Transform (linha no gizmo). Vazio = somem so por tempo (lifetime). O coelho morto sempre fica.")]
    public Transform despawnPoint;

    private bool fired;
    private Transform player;

    /// <summary>Dispara a fuga (uma vez).</summary>
    public void Trigger()
    {
        if (fired) return;
        fired = true;
        SpawnHerd();
    }

    private void Update()
    {
        if (fired || !autoTriggerOnCross) return;
        if (player == null)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go == null) return;
            player = go.transform;
        }
        if (player.position.x >= transform.position.x)
            Trigger();
    }

    private void SpawnHerd()
    {
        int n = Mathf.Max(1, rabbitCount);
        int sick = sickIndex < 0 ? n - 1 : Mathf.Clamp(sickIndex, 0, n - 1);
        Vector3 origin = transform.position;

        for (int i = 0; i < n; i++)
        {
            Vector3 pos = new Vector3(origin.x + i * spacingX, origin.y + i * spacingY, 0f);

            var go = new GameObject("Coelho");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * rabbitScale;

            var sr = go.AddComponent<SpriteRenderer>();
            if (previewSprite != null) sr.sprite = previewSprite;
            sr.sortingOrder = sortingOrder;

            var anim = go.AddComponent<Animator>();
            if (rabbitController != null) anim.runtimeAnimatorController = rabbitController;
            anim.applyRootMotion = false;

            // os de tras (i maior) correm um tico mais devagar -> ficam pra tras
            float vx = runVelocity.x + i * (speedVariation / n);

            var rabbit = go.AddComponent<Rabbit>();
            // TODOS os coelhos passam mal e morrem (a fumaca pega todos); so UM tem o balao.
            bool hasBubble = (i == sick);
            rabbit.sickLine = hasBubble ? sickLine : "";
            rabbit.sickLocalizationKey = hasBubble ? sickLocalizationKey : "";
            // escalona o momento de passar mal pra nao cairem todos juntos
            rabbit.sickAfterSeconds = Random.Range(1.0f, 3.0f);
            rabbit.Launch(new Vector2(vx, runVelocity.y), true);
            if (despawnPoint != null)
            {
                rabbit.useDespawnX = true;
                rabbit.despawnX = despawnPoint.position.x;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position;
        Gizmos.color = new Color(0.75f, 0.5f, 0.3f);

        int n = Mathf.Max(1, rabbitCount);
        for (int i = 0; i < n; i++)
            Gizmos.DrawWireSphere(origin + new Vector3(i * spacingX, i * spacingY, 0f), 0.3f);

        Vector3 dir = new Vector3(runVelocity.x, runVelocity.y, 0f);
        if (dir.sqrMagnitude > 0.001f)
        {
            dir.Normalize();
            Vector3 tip = origin + dir * 5f;
            Gizmos.DrawLine(origin, tip);
            Vector3 perp = new Vector3(-dir.y, dir.x, 0f) * 0.5f;
            Gizmos.DrawLine(tip, tip - dir * 1f + perp);
            Gizmos.DrawLine(tip, tip - dir * 1f - perp);
        }
        // linha-limite onde os coelhos somem (arrastar o despawnPoint)
        if (despawnPoint != null)
        {
            float lx = despawnPoint.position.x;
            Gizmos.color = new Color(1f, 0.3f, 0.3f);
            Gizmos.DrawLine(new Vector3(lx, -50f, 0f), new Vector3(lx, 50f, 0f));
        }
#if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(0.75f, 0.5f, 0.3f);
        UnityEditor.Handles.Label(origin + Vector3.up * 1.2f, "Fuga dos coelhos");
        if (despawnPoint != null)
        {
            UnityEditor.Handles.color = new Color(1f, 0.3f, 0.3f);
            UnityEditor.Handles.Label(new Vector3(despawnPoint.position.x, 4.5f, 0f), "Limite (coelhos somem)");
        }
#endif
    }
}
