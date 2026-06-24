using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Desfecho da fase 1: depois da luta final, os javalis pais que SOBREVIVERAM ao confronto 3
/// entram andando pela esquerda e um filhote (50% menor) vem caminhando da direita chamando
/// a mae. Eles se encontram no meio da cena e a fase acaba.
///
/// Os javalis sao montados em codigo (SpriteRenderer + Animator), no estilo do RabbitHerd:
/// basta ligar o RuntimeAnimatorController do javali (Idle/Run) e um sprite de preview.
/// Chamado pelo FinalBossEncounter via Play(survivorCount, onComplete).
/// </summary>
public class BoarReunion : MonoBehaviour
{
    [Header("Visual do javali")]
    [Tooltip("Controller de animacao do javali (Idle/Run). Pode ser o mesmo do BoarAlly.")]
    public RuntimeAnimatorController boarController;
    public Sprite previewSprite;
    [Tooltip("Marque se o sprite original aponta pra direita.")]
    public bool spriteFacesRight = true;
    public string runState = "Run";
    public string idleState = "Idle";
    public float adultScale = 1f;
    [Tooltip("Escala do filhote (0.5 = metade do tamanho do adulto).")]
    public float babyScale = 0.5f;
    public int sortingOrder = 120;

    [Header("Movimento")]
    public float walkSpeed = 2.2f;
    [Tooltip("Y do chao onde os javalis caminham.")]
    public float groundY = -5f;
    public float adultSpacing = 1.3f;
    [Tooltip("Distancia de cada lado do ponto de encontro (adultos param a -X, filhote a +X).")]
    public float meetGap = 1.1f;

    [Header("Fala do filhote")]
    public string babyLine = "Mãe!";
    public string babyLocalizationKey = "world.fauna.filhote_mae";
    [Tooltip("Segundos parados (reencontro) antes de terminar a fase.")]
    public float holdAfterMeet = 2.5f;

    [Header("Sem sobreviventes (a arara consola o filhote)")]
    public string babySearchingLine = "Mãe...? Pai...?";
    public string babySearchingKey = "world.fauna.filhote_procura";
    public string araraApologyLine = "Me perdoa, pequeno... a gente não conseguiu salvar seus pais.";
    public string araraApologyKey = "world.fala.arara_filhote_sozinho";
    public string babyCryLine = "Buááá...";
    public string babyCryKey = "world.fauna.filhote_choro";

    private readonly Dictionary<Transform, string> lastAnim = new Dictionary<Transform, string>();

    /// <summary>Toca o reencontro. survivorCount = javalis pais sobreviventes do confronto 3.</summary>
    public void Play(int survivorCount, Action onComplete)
    {
        StartCoroutine(Run(Mathf.Max(0, survivorCount), onComplete));
    }

    private IEnumerator Run(int adults, Action onComplete)
    {
        var cam = Camera.main;
        float halfW = (cam != null && cam.orthographic) ? cam.orthographicSize * cam.aspect : 12f;
        float cx = cam != null ? cam.transform.position.x : transform.position.x;
        float meetX = cx;
        float leftStart = cx - halfW - 2f;
        float rightStart = cx + halfW + 2f;

        // Adultos entram pela esquerda (em fila), filhote pela direita.
        var adultsList = new List<Transform>();
        for (int i = 0; i < adults; i++)
            adultsList.Add(MakeBoar(adultScale, new Vector3(leftStart - i * adultSpacing, groundY, 0f)));
        var baby = MakeBoar(babyScale, new Vector3(rightStart, groundY, 0f));

        float adultTargetX = meetX - meetGap;
        float babyTargetX = adults > 0 ? meetX + meetGap : meetX; // sem pais, o filhote vai ate o centro

        bool moving = true;
        while (moving)
        {
            moving = false;
            for (int i = 0; i < adultsList.Count; i++)
                if (MoveToward(adultsList[i], adultTargetX - i * adultSpacing, +1)) moving = true;
            if (MoveToward(baby, babyTargetX, -1)) moving = true;
            yield return null;
        }

        // Reencontro: todos param.
        foreach (var t in adultsList) { FaceDir(t, +1); SetAnim(t, idleState); }
        FaceDir(baby, -1); SetAnim(baby, idleState);

        if (adultsList.Count > 0)
        {
            // Algum pai sobreviveu: reencontro feliz.
            ShowBabyLine(baby);
            yield return new WaitForSeconds(holdAfterMeet);
        }
        else
        {
            // Ninguem sobreviveu: a arara fala com o filhote no lugar dos pais.
            yield return LonelyBabyRoutine(baby);
        }
        onComplete?.Invoke();
    }

    /// <summary>Caso triste: nenhum javali sobreviveu. O filhote procura os pais, a arara
    /// volta e pede desculpas no lugar deles, o filhote chora e a fase encerra.</summary>
    private IEnumerator LonelyBabyRoutine(Transform baby)
    {
        // 1) filhote procura os pais
        ShowPop(baby.position + Vector3.up * 1.6f, Resolve(babySearchingLine, babySearchingKey));
        yield return new WaitForSeconds(1.8f);

        // 2) a arara APARECE ao lado do filhote e fala no lugar dos pais (entrada rapida,
        // sem o voo lento de volta).
        var arara = AraraSpeechBubble.Instance;
        if (arara != null)
        {
            var follow = arara.GetComponent<CompanionFollow>();
            if (follow != null) follow.enabled = false;            // para de seguir/fugir
            var asr = arara.GetComponent<SpriteRenderer>();
            if (asr != null) { asr.enabled = true; asr.flipX = true; } // visivel e virada pro filhote
            arara.transform.position = baby.position + new Vector3(2.2f, 2.2f, 0f);
            yield return new WaitForSeconds(0.5f);                  // um beat antes de falar
        }

        string apology = Resolve(araraApologyLine, araraApologyKey);
        if (arara != null)
        {
            arara.Show(apology);
            yield return new WaitUntil(() => !arara.IsBusy);
        }
        else
        {
            ShowPop(baby.position + Vector3.up * 2.4f, apology);
            yield return new WaitForSeconds(2.5f);
        }

        // 3) o filhote chora e a fase encerra
        ShowPop(baby.position + Vector3.up * 1.6f, Resolve(babyCryLine, babyCryKey));
        yield return new WaitForSeconds(holdAfterMeet);
    }

    private string Resolve(string literal, string key)
    {
        if (!string.IsNullOrEmpty(key))
        {
            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                string s = loc.GetText(key);
                if (!string.IsNullOrEmpty(s) && !s.StartsWith("[")) return s;
            }
        }
        return literal;
    }

    private void ShowPop(Vector3 pos, string text)
    {
        if (!string.IsNullOrEmpty(text))
            SpeechBubble.Pop(pos, text, 3.0f, new Color(1f, 0.97f, 0.86f), 1000, 2.5f, 0.3f, 6f);
    }

    // Retorna true se ainda esta andando (nao chegou no alvo).
    private bool MoveToward(Transform t, float targetX, int dir)
    {
        if (t == null) return false;
        var p = t.position;
        p.x = Mathf.MoveTowards(p.x, targetX, walkSpeed * Time.deltaTime);
        t.position = p;
        bool arrived = Mathf.Abs(p.x - targetX) < 0.03f;
        FaceDir(t, dir);
        SetAnim(t, arrived ? idleState : runState);
        return !arrived;
    }

    private Transform MakeBoar(float scale, Vector3 pos)
    {
        var go = new GameObject("ReunionBoar");
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;

        var sr = go.AddComponent<SpriteRenderer>();
        if (previewSprite != null) sr.sprite = previewSprite;
        sr.sortingOrder = sortingOrder;

        var anim = go.AddComponent<Animator>();
        if (boarController != null) anim.runtimeAnimatorController = boarController;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        return go.transform;
    }

    private void FaceDir(Transform t, int dir)
    {
        var sr = t.GetComponent<SpriteRenderer>();
        if (sr == null) return;
        bool goingLeft = dir < 0;
        sr.flipX = spriteFacesRight ? goingLeft : !goingLeft;
    }

    // So chama anim.Play quando o estado muda (senao reseta no frame 0 todo frame).
    private void SetAnim(Transform t, string state)
    {
        var anim = t.GetComponent<Animator>();
        if (anim == null || anim.runtimeAnimatorController == null || string.IsNullOrEmpty(state)) return;
        if (lastAnim.TryGetValue(t, out var cur) && cur == state) return;
        lastAnim[t] = state;
        anim.Play(state, 0, 0f);
    }

    private void ShowBabyLine(Transform baby)
    {
        string text = babyLine;
        if (!string.IsNullOrEmpty(babyLocalizationKey))
        {
            var loc = LocalizationManager.Instance;
            if (loc != null)
            {
                string s = loc.GetText(babyLocalizationKey);
                if (!string.IsNullOrEmpty(s) && !s.StartsWith("[")) text = s;
            }
        }
        if (!string.IsNullOrEmpty(text))
            SpeechBubble.Pop(baby.position + Vector3.up * 1.6f, text, 3.0f, new Color(1f, 0.97f, 0.86f), 1000, 2.5f, 0.3f, 6f);
    }
}
