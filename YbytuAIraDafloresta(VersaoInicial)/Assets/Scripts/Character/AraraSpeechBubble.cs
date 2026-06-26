using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

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

    public bool IsBusy => processing || queue.Count > 0;

    public bool HasUntypedSpeech => pendingTypeCount > 0;

    private void Awake()
    {
        Instance = this;
        BuildBubble();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SetFreeze(false);
    }

    private void SetFreeze(bool on)
    {
        if (!freezePlayerWhileTalking) return;
        PlayerController.InputFrozen = on;
        EnemyController.CombatFrozen = on;
    }

    private void BuildBubble()
    {
        bubble = SpeechBubble.Create(transform, new Vector3(0f, height, 0f), 1000, fontSize, wrapWidth, textColor, true);
        bubble.SetText("");
    }

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
        SetFreeze(false);
    }

    private IEnumerator ProcessQueue()
    {
        processing = true;
        SetFreeze(true);
        while (queue.Count > 0)
        {
            string line = queue.Dequeue();
            yield return TypeRoutine(line);
            ShowAdvanceHint(line);
            yield return WaitForAdvance();
            pendingTypeCount = Mathf.Max(0, pendingTypeCount - 1);
        }
        bubble.SetText("");
        processing = false;
        SetFreeze(false);
    }

    private void ShowAdvanceHint(string line)
    {
        bubble.SetText(line + "  <size=55%><alpha=#88>[espaço]</size>");
        bubble.SetVisibleChars(99999);
    }

    private IEnumerator WaitForAdvance()
    {
        yield return null;
        while (!SkipPressed()) yield return null;
    }

    private IEnumerator TypeRoutine(string text)
    {
        var sm = SoundManager.Instance;
        if (sm != null && sm.Library != null && sm.Library.araraTalk != null)
            sm.PlaySFX(sm.Library.araraTalk);

        bubble.SetText(text);
        bubble.SetVisibleChars(0);
        float t = 0f;
        int shown = 0;
        while (shown < text.Length)
        {
            if (SkipPressed()) break;
            t += Time.deltaTime * charsPerSecond;
            shown = Mathf.Min(text.Length, Mathf.FloorToInt(t));
            bubble.SetVisibleChars(shown);
            yield return null;
        }
        bubble.SetVisibleChars(text.Length);
    }

    private static bool SkipPressed()
    {
        var kb = Keyboard.current;
        var gp = Gamepad.current;
        return (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
            || (gp != null && gp.buttonSouth.wasPressedThisFrame);
    }
}
