using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;
using TMPro;

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
    private AudioSource typingSource;

    public bool IsPlaying => isPlaying;
    public int CurrentIndex => currentIndex;
    public int TotalTexts => texts != null ? texts.Length : 0;

    public event Action OnAllTextsFinished;
    public event Action<int> OnTextStarted;

    private void Awake()
    {
        typingSource = gameObject.AddComponent<AudioSource>();
        typingSource.playOnAwake = false;
        typingSource.loop = false;
    }

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

    public void StartPlayingSection(string section)
    {
        textSection = section;
        StartPlaying();
    }

    public void ClearDisplay()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        if (typingSource != null) typingSource.Stop();
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

        int typed = 0;
        foreach (char c in text)
        {
            displayText.text += c;
            if (!char.IsWhiteSpace(c) && (typed++ % 2 == 0))
                PlayTypeBlip();
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

    private void PlayTypeBlip()
    {
        var sm = SoundManager.Instance;
        if (sm == null || sm.Library == null || sm.Library.dialogueType == null) return;
        if (typingSource.outputAudioMixerGroup == null)
            typingSource.outputAudioMixerGroup = sm.SfxGroup;
        typingSource.pitch = 1f + UnityEngine.Random.Range(-0.04f, 0.04f);
        typingSource.PlayOneShot(sm.Library.dialogueType, 0.5f);
    }

    private void SkipTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (typingSource != null) typingSource.Stop();

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
