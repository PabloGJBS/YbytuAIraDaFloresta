using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;
using TMPro;

/// <summary>
/// Exibe textos de uma secao de localizacao em sequencia (cutscenes, dialogos).
/// Busca todos os textos de uma secao e exibe um por um com efeito de digitacao.
///
/// Uso:
///   1. Adicionar ao GameObject da cutscene
///   2. Setar o textSection (ex: "story.intro", "stages.stage_01.intro")
///   3. Referenciar o TMP_Text onde o texto aparece
///   4. Chamar StartPlaying() ou deixar playOnStart = true
/// </summary>
public class CutsceneTextPlayer : MonoBehaviour
{
    [Header("Configuracao")]
    [SerializeField] private string textSection;
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private bool playOnStart = true;

    [Header("Timing")]
    [SerializeField] private float charDelay = 0.03f;
    [SerializeField] private float pauseBetweenTexts = 2f;
    [SerializeField] private bool waitForInput = true;

    [Header("Indicador de Avanco")]
    [SerializeField] private GameObject continuePrompt;

    private string[] texts;
    private int currentIndex;
    private bool isTyping;
    private bool isPlaying;
    private Coroutine typingCoroutine;

    public bool IsPlaying => isPlaying;
    public int CurrentIndex => currentIndex;
    public int TotalTexts => texts != null ? texts.Length : 0;

    public event Action OnAllTextsFinished;
    public event Action<int> OnTextStarted;

    private void Start()
    {
        if (continuePrompt != null)
            continuePrompt.SetActive(false);

        if (playOnStart)
            StartPlaying();
    }

    private void Update()
    {
        if (!isPlaying) return;

        bool inputPressed = AdvancePressedThisFrame();

        if (isTyping && inputPressed)
        {
            // Skipar efeito de digitacao - mostrar texto completo
            SkipTyping();
        }
        else if (!isTyping && waitForInput && inputPressed)
        {
            // Avancar para proximo texto
            ShowNextText();
        }
    }

    private static bool AdvancePressedThisFrame()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            return true;
        var gp = Gamepad.current;
        if (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.startButton.wasPressedThisFrame))
            return true;
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            return true;
        return false;
    }

    /// <summary>
    /// Iniciar a exibicao dos textos da secao configurada.
    /// </summary>
    public void StartPlaying()
    {
        if (LocalizationManager.Instance == null) return;

        texts = LocalizationManager.Instance.GetSection(textSection);
        if (texts.Length == 0)
        {
            OnAllTextsFinished?.Invoke();
            return;
        }

        currentIndex = 0;
        isPlaying = true;
        ShowCurrentText();
    }

    /// <summary>
    /// Iniciar com uma secao diferente (para reutilizar o componente).
    /// </summary>
    public void StartPlayingSection(string section)
    {
        textSection = section;
        StartPlaying();
    }

    /// <summary>
    /// Limpa o texto na tela e esconde o prompt. Util durante transicoes de cena.
    /// </summary>
    public void ClearDisplay()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        isTyping = false;
        if (displayText != null)
            displayText.text = "";
        if (continuePrompt != null)
            continuePrompt.SetActive(false);
    }

    private void ShowCurrentText()
    {
        if (currentIndex >= texts.Length)
        {
            FinishAll();
            return;
        }

        OnTextStarted?.Invoke(currentIndex);

        if (continuePrompt != null)
            continuePrompt.SetActive(false);

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(texts[currentIndex]));
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        displayText.text = "";

        foreach (char c in text)
        {
            displayText.text += c;
            yield return new WaitForSeconds(charDelay);
        }

        isTyping = false;

        if (continuePrompt != null)
            continuePrompt.SetActive(true);

        if (!waitForInput)
        {
            yield return new WaitForSeconds(pauseBetweenTexts);
            ShowNextText();
        }
    }

    private void SkipTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (currentIndex < texts.Length)
            displayText.text = texts[currentIndex];

        isTyping = false;

        if (continuePrompt != null)
            continuePrompt.SetActive(true);
    }

    private void ShowNextText()
    {
        currentIndex++;

        if (continuePrompt != null)
            continuePrompt.SetActive(false);

        if (currentIndex >= texts.Length)
            FinishAll();
        else
            ShowCurrentText();
    }

    private void FinishAll()
    {
        isPlaying = false;
        OnAllTextsFinished?.Invoke();
    }
}
