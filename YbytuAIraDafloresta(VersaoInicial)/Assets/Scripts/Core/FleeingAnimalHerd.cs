using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FleeingAnimalHerd : MonoBehaviour
{
    [Header("Animal")]
    [Tooltip("Nome do bicho (vira o nome do GameObject e da fala): Sapo, Inseto, etc.")]
    public string creatureName = "Bicho";
    [Tooltip("Controller de animacao (estados de andar/parado). Ex.: Sapo.controller, Inseto.controller.")]
    public RuntimeAnimatorController animatorController;
    [Tooltip("Opcional: varios controllers (cores diferentes) sorteados por bicho. Se vazio, usa o animatorController acima.")]
    public RuntimeAnimatorController[] colorVariants;
    [Tooltip("Sprite inicial (opcional; o Animator assume no 1o frame).")]
    public Sprite previewSprite;

    [Header("Estados do Animator")]
    [Tooltip("Estado de andar. Estes controllers usam 'Walk'.")]
    public string runStateName = "Walk";
    [Tooltip("Estado ao morrer. Sem animacao de morte, deixe 'Idle' (vira corpo parado) ou vazio (congela).")]
    public string deathStateName = "Idle";
    public int count = 3;
    [Tooltip("Velocidade da fuga (x negativo = correndo pra esquerda). Sapo ~-3, Inseto ~-6.")]
    public Vector2 runVelocity = new Vector2(-5f, 0f);
    [Tooltip("Espacamento da formacao (cada bicho um pouco mais a frente e ao fundo).")]
    public float spacingX = 1.6f;
    public float spacingY = 0.15f;
    [Tooltip("Espalhamento vertical aleatorio (+/-) no spawn. Evita que os bichos fiquem em fila indiana.")]
    public float verticalSpread = 0f;
    [Tooltip("Os de tras correm um tico mais devagar.")]
    public float speedVariation = 1f;
    public float scale = 1f;
    public int sortingOrder = 120;

    [Header("Pulo (preset por bicho)")]
    [Tooltip("Altura do pulinho. Sapo: ~0.4 (pulao). Inseto: ~0.05 (rasteiro).")]
    public float hopAmplitude = 0.18f;
    [Tooltip("Frequencia do pulinho. Sapo: ~3. Inseto: ~12.")]
    public float hopFrequency = 7f;
    public bool spriteFacesRight = true;

    [Header("Passar mal / morrer")]
    [Tooltip("Liga o passar mal+morrer pela fumaca. Desligue pra bichos que so fogem (ex.: enxame).")]
    public bool enableSickDeath = true;
    [Tooltip("Indice (0 = primeiro) do bicho que fala. -1 = o ultimo (atrasado).")]
    public int sickIndex = -1;
    [Tooltip("Texto literal da fala (usado se a chave de localizacao estiver vazia).")]
    public string sickLine = "";
    [Tooltip("Chave de localizacao da fala (pt/en). Vazio = usa o texto literal.")]
    public string sickLocalizationKey = "";

    [Header("Spawn")]
    [Tooltip("Desloca o ponto de spawn em relacao a este objeto. Ex.: (13,0) = nascem 13u a direita (fora da tela). O GATILHO continua sendo a posicao deste objeto.")]
    public Vector2 spawnOffset = Vector2.zero;

    [Header("Repeticao (fluxo infinito)")]
    [Tooltip("Se true, fica spawnando levas para sempre (ex.: sapos passando sem parar). Se false, uma leva so.")]
    public bool loop = false;
    [Tooltip("Segundos entre cada leva quando loop=true.")]
    public float loopInterval = 4f;
    [Tooltip("Maximo de bichos vivos ao mesmo tempo (so no loop). 0 = sem limite. Evita acumular e travar.")]
    public int maxAlive = 0;

    [Header("Player / Disparo")]
    public string playerTag = "Player";
    [Tooltip("Se true, dispara sozinho quando o player cruza esta posicao. Se false, so via Trigger().")]
    public bool autoTriggerOnCross = true;

    [Header("Limite (some ao cruzar)")]
    [Tooltip("Ponto arrastavel: os bichos somem ao cruzar o X deste Transform. Vazio = somem so por tempo. O morto sempre fica.")]
    public Transform despawnPoint;

    private bool fired;
    private Transform player;
    private readonly List<GameObject> spawned = new List<GameObject>();

    public void Trigger()
    {
        if (fired) return;
        fired = true;
        if (loop) StartCoroutine(LoopSpawn());
        else SpawnHerd();
    }

    private IEnumerator LoopSpawn()
    {
        while (true)
        {
            SpawnHerd();
            yield return new WaitForSeconds(Mathf.Max(0.25f, loopInterval));
        }
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
        spawned.RemoveAll(g => g == null);
        int n = Mathf.Max(1, count);
        if (maxAlive > 0)
        {
            int free = maxAlive - spawned.Count;
            if (free <= 0) return;
            n = Mathf.Min(n, free);
        }
        int sick = sickIndex < 0 ? n - 1 : Mathf.Clamp(sickIndex, 0, n - 1);
        Vector3 origin = transform.position + new Vector3(spawnOffset.x, spawnOffset.y, 0f);

        for (int i = 0; i < n; i++)
        {
            float jy = verticalSpread > 0f ? Random.Range(-verticalSpread, verticalSpread) : 0f;
            Vector3 pos = new Vector3(origin.x + i * spacingX, origin.y + i * spacingY + jy, 0f);

            var go = new GameObject(string.IsNullOrEmpty(creatureName) ? "Bicho" : creatureName);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            spawned.Add(go);

            var sr = go.AddComponent<SpriteRenderer>();
            if (previewSprite != null) sr.sprite = previewSprite;
            sr.sortingOrder = sortingOrder;

            var ctrl = (colorVariants != null && colorVariants.Length > 0)
                ? colorVariants[Random.Range(0, colorVariants.Length)]
                : animatorController;
            var anim = go.AddComponent<Animator>();
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;

            float vx = runVelocity.x + i * (speedVariation / n);

            var animal = go.AddComponent<FleeingAnimal>();
            animal.creatureName = creatureName;
            animal.hopAmplitude = hopAmplitude;
            animal.hopFrequency = hopFrequency;
            animal.spriteFacesRight = spriteFacesRight;
            animal.runStateName = runStateName;
            animal.deathStateName = deathStateName;
            animal.enableSickDeath = enableSickDeath;

            bool hasBubble = enableSickDeath && (i == sick);
            animal.sickLine = hasBubble ? sickLine : "";
            animal.sickLocalizationKey = hasBubble ? sickLocalizationKey : "";
            animal.sickAfterSeconds = Random.Range(1.0f, 3.0f);
            animal.Launch(new Vector2(vx, runVelocity.y), enableSickDeath);
            if (despawnPoint != null)
            {
                animal.useDespawnX = true;
                animal.despawnX = despawnPoint.position.x;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 trigger = transform.position;
        Vector3 origin = trigger + new Vector3(spawnOffset.x, spawnOffset.y, 0f);
        Gizmos.color = new Color(0.55f, 0.75f, 0.4f);

        Gizmos.DrawWireCube(trigger, Vector3.one * 0.4f);
        if (spawnOffset.sqrMagnitude > 0.001f) Gizmos.DrawLine(trigger, origin);

        int n = Mathf.Max(1, count);
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
        if (despawnPoint != null)
        {
            float lx = despawnPoint.position.x;
            Gizmos.color = new Color(1f, 0.3f, 0.3f);
            Gizmos.DrawLine(new Vector3(lx, -50f, 0f), new Vector3(lx, 50f, 0f));
        }
#if UNITY_EDITOR
        UnityEditor.Handles.color = new Color(0.55f, 0.75f, 0.4f);
        UnityEditor.Handles.Label(origin + Vector3.up * 1.2f, $"Fuga: {creatureName}");
#endif
    }
}
