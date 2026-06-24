using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Balao de fala da arara. Monta o proprio texto (TextMeshPro world) acima do objeto,
/// no estilo do prompt do EducationalMarker (fonte + outline forte, legivel sem fundo).
/// Os DialogueTriggerLine chamam Show(texto). As falas entram numa FILA e tocam em
/// sequencia (uma depois da outra) com efeito de digitacao; assim varias falas
/// disparadas juntas (ex.: as do inicio, antes do player saber andar) saem em ordem.
/// O player fica travado enquanto houver fala na fila.
///
/// O GameObject do texto fica SEMPRE ativo (o TMP so gera mesh ativo); a visibilidade
/// e controlada pelo conteudo (texto vazio = nada na tela).
/// </summary>
public class AraraSpeechBubble : MonoBehaviour
{
    public static AraraSpeechBubble Instance { get; private set; }

    [Header("Balao")]
    [SerializeField] private float height = 2f;
    [SerializeField] private float fontSize = 3.2f;
    [SerializeField] private float wrapWidth = 8f;
    [SerializeField] private Color textColor = new Color(1f, 0.97f, 0.82f);

    [Header("Tempo")]
    [SerializeField] private float charsPerSecond = 45f;
    [Tooltip("Tempo que a fala fica na tela depois de escrever (sem travar o player).")]
    [SerializeField] private float holdSeconds = 0.5f;
    [Tooltip("Trava o player so enquanto a fala ESCREVE (libera assim que termina de digitar).")]
    [SerializeField] private bool freezePlayerWhileTalking = true;

    private SpeechBubble bubble;
    private readonly Queue<string> queue = new Queue<string>();
    private bool processing;
    private int pendingTypeCount;

    /// <summary>True enquanto ha fala escrevendo ou na fila (inclui o tempo na tela).</summary>
    public bool IsBusy => processing || queue.Count > 0;

    /// <summary>True ate a ULTIMA fala terminar de ser DIGITADA (nao inclui o hold na tela).
    /// Coincide com o momento em que o player e liberado.</summary>
    public bool HasUntypedSpeech => pendingTypeCount > 0;

    private void Awake()
    {
        Instance = this;
        BuildBubble();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (freezePlayerWhileTalking) PlayerController.InputFrozen = false;
    }

    private void BuildBubble()
    {
        // Balao estilizado compartilhado (caixa + borda + rabicho), igual ao dos inimigos/javali.
        bubble = SpeechBubble.Create(transform, new Vector3(0f, height, 0f), 1000, fontSize, wrapWidth, textColor, true);
        bubble.SetText("");
    }

    /// <summary>Enfileira uma fala; toca em sequencia com as outras.</summary>
    public void Show(string text)
    {
        if (bubble == null || string.IsNullOrEmpty(text)) return;
        queue.Enqueue(text);
        pendingTypeCount++;
        if (!processing) StartCoroutine(ProcessQueue());
    }

    public void Clear()
    {
        queue.Clear();
        StopAllCoroutines();
        processing = false;
        pendingTypeCount = 0;
        if (bubble != null) bubble.SetText("");
        if (freezePlayerWhileTalking) PlayerController.InputFrozen = false;
    }

    private IEnumerator ProcessQueue()
    {
        processing = true;
        while (queue.Count > 0)
        {
            // trava so enquanto digita esta fala
            if (freezePlayerWhileTalking) PlayerController.InputFrozen = true;
            yield return TypeRoutine(queue.Dequeue());
            pendingTypeCount = Mathf.Max(0, pendingTypeCount - 1);
            // assim que termina de escrever a ultima da fila, libera o movimento
            if (queue.Count == 0 && freezePlayerWhileTalking)
                PlayerController.InputFrozen = false;
            // a fala fica mais um tempo na tela (mas o espaço pula esse dialogo).
            if (holdSeconds > 0f)
            {
                float h = 0f;
                while (h < holdSeconds && !SkipPressed())
                {
                    h += Time.deltaTime;
                    yield return null;
                }
            }
        }
        bubble.SetText("");
        processing = false;
    }

    private IEnumerator TypeRoutine(string text)
    {
        // Dimensiona a caixa pro texto completo de uma vez (nao "cresce" por caractere),
        // e revela letra por letra via maxVisibleCharacters.
        bubble.SetText(text);
        bubble.SetVisibleChars(0);
        float t = 0f;
        int shown = 0;
        while (shown < text.Length)
        {
            if (SkipPressed()) break; // espaço: revela a frase inteira de uma vez
            t += Time.deltaTime * charsPerSecond;
            shown = Mathf.Min(text.Length, Mathf.FloorToInt(t));
            bubble.SetVisibleChars(shown);
            yield return null;
        }
        bubble.SetVisibleChars(text.Length);
    }

    /// <summary>Espaço/Enter/A do controle: pula a digitacao ou avanca o dialogo.</summary>
    private static bool SkipPressed()
    {
        var kb = Keyboard.current;
        var gp = Gamepad.current;
        return (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
            || (gp != null && gp.buttonSouth.wasPressedThisFrame);
    }
}
