using UnityEngine;
using TMPro;

/// <summary>
/// Dispara uma revoada de passaros quando o player cruza este gatilho. Os passaros
/// surgem a frente (lado do fogo) e voam pra tras fugindo, passando por cima do
/// player, enquanto o lider grita uma frase. Posicione o GameObject (com um
/// BoxCollider2D trigger) onde a revoada deve comecar, ex.: entre as zonas de combate.
/// </summary>
public class BirdFlock : MonoBehaviour
{
    [Header("Revoada")]
    [Tooltip("Prefab do passaro (com SpriteRenderer + Animator + Bird).")]
    public GameObject birdPrefab;
    [Tooltip("Controllers de cor pra variar os passaros (ciclados).")]
    public RuntimeAnimatorController[] birdVariants;
    public int birdCount = 5;
    [Tooltip("Velocidade base da revoada (x negativo = fugindo pra tras/esquerda).")]
    public Vector2 flyVelocity = new Vector2(-8f, 0.4f);
    [Tooltip("Espacamento da formacao (cada passaro um pouco mais a frente e alto).")]
    public float spacingX = 1.8f;
    public float spacingY = 0.8f;
    public float speedVariation = 1.5f;
    public float birdScale = 1f;

    [Header("Grito")]
    [Tooltip("Frase gritada pelo lider. Se vazio, usa a chave de localizacao.")]
    public string shoutText = "Fuja, fuja do fogo!";
    public string shoutLocalizationKey = "";
    public float shoutHeight = 1.6f;

    [Header("Player / Disparo")]
    public string playerTag = "Player";
    [Tooltip("Se true, dispara sozinho quando o player cruza esta posicao. Se false, so dispara via Trigger() (ex.: chamado pelo totem).")]
    public bool autoTriggerOnCross = false;

    [Header("Limite (some ao cruzar)")]
    [Tooltip("Ponto arrastavel: os passaros somem ao cruzar o X deste Transform (linha no gizmo). Vazio = somem so por tempo (lifetime).")]
    public Transform despawnPoint;

    private bool fired;
    private Transform player;

    /// <summary>Dispara a revoada (uma vez). Chamado pelo totem ao interagir.</summary>
    public void Trigger()
    {
        if (fired) return;
        fired = true;
        Debug.Log($"[Revoada] Trigger! {birdCount} passaros, surge em x={transform.position.x:0.0}, prefab={(birdPrefab != null ? birdPrefab.name : "NULL (sem prefab!)")}", this);
        SpawnFlock();
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

    private void SpawnFlock()
    {
        if (birdPrefab == null) return;
        Vector3 origin = transform.position;

        for (int i = 0; i < birdCount; i++)
        {
            // formacao em diagonal (cada um um pouco mais a frente e mais alto)
            Vector3 pos = new Vector3(
                origin.x + i * spacingX,
                origin.y + i * spacingY,
                0f);

            var go = Instantiate(birdPrefab, pos, Quaternion.identity);
            go.transform.localScale = Vector3.one * birdScale;

            if (birdVariants != null && birdVariants.Length > 0)
            {
                var anim = go.GetComponentInChildren<Animator>();
                if (anim != null)
                    anim.runtimeAnimatorController = birdVariants[i % birdVariants.Length];
            }

            float vx = flyVelocity.x - i * (speedVariation / Mathf.Max(1, birdCount));
            var bird = go.GetComponent<Bird>();
            if (bird != null)
            {
                bird.Launch(new Vector2(vx, flyVelocity.y));
                if (despawnPoint != null)
                {
                    bird.useDespawnX = true;
                    bird.despawnX = despawnPoint.position.x;
                }
            }

            if (i == 0) AttachShout(go.transform);
        }
    }

    private void AttachShout(Transform leader)
    {
        string text = ResolveShout();
        if (string.IsNullOrEmpty(text)) return;

        var go = new GameObject("Shout", typeof(TextMeshPro));
        go.transform.SetParent(leader, false);
        go.transform.localPosition = new Vector3(0f, shoutHeight, 0f);

        var tmp = go.GetComponent<TextMeshPro>();
        tmp.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        tmp.text = text;
        tmp.fontSize = 2.6f;
        tmp.fontStyle = FontStyles.Bold | FontStyles.Italic;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.95f, 0.5f);
        tmp.outlineWidth = 0.22f;
        tmp.outlineColor = new Color32(0, 0, 0, 255);
        tmp.sortingOrder = 1000;
    }

    private string ResolveShout()
    {
        if (!string.IsNullOrEmpty(shoutLocalizationKey))
        {
            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                string s = loc.GetText(shoutLocalizationKey);
                if (!string.IsNullOrEmpty(s)) return s;
            }
        }
        return shoutText;
    }

    private void OnDrawGizmos()
    {
        Vector3 origin = transform.position;
        Gizmos.color = new Color(1f, 0.85f, 0.2f);

        // onde cada passaro surge (formacao)
        int n = Mathf.Max(1, birdCount);
        for (int i = 0; i < n; i++)
            Gizmos.DrawWireSphere(origin + new Vector3(i * spacingX, i * spacingY, 0f), 0.4f);

        // direcao do voo
        Vector3 dir = new Vector3(flyVelocity.x, flyVelocity.y, 0f);
        if (dir.sqrMagnitude > 0.001f)
        {
            dir.Normalize();
            Vector3 tip = origin + dir * 6f;
            Gizmos.DrawLine(origin, tip);
            Vector3 perp = new Vector3(-dir.y, dir.x, 0f) * 0.6f;
            Gizmos.DrawLine(tip, tip - dir * 1.2f + perp);
            Gizmos.DrawLine(tip, tip - dir * 1.2f - perp);
        }
        // linha-limite onde os passaros somem (arrastar o despawnPoint)
        if (despawnPoint != null)
        {
            float lx = despawnPoint.position.x;
            Gizmos.color = new Color(1f, 0.3f, 0.3f);
            Gizmos.DrawLine(new Vector3(lx, -50f, 0f), new Vector3(lx, 50f, 0f));
        }
#if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(1f, 0.85f, 0.2f);
        UnityEditor.Handles.Label(origin + Vector3.up * 1.6f, "Revoada (surge aqui)");
        if (despawnPoint != null)
        {
            UnityEditor.Handles.color = new Color(1f, 0.3f, 0.3f);
            UnityEditor.Handles.Label(new Vector3(despawnPoint.position.x, 4.5f, 0f), "Limite (passaros somem)");
        }
#endif
    }
}
