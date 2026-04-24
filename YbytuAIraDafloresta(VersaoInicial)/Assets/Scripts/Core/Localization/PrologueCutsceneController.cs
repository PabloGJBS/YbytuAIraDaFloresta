using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Orquestra a cutscene do prologo: para cada cena (imagem + secao de localizacao),
/// troca o fundo e dispara o CutsceneTextPlayer. Quando todos os textos da secao
/// terminam, avanca para a proxima cena. Ao terminar a ultima, chama OnFinished.
/// </summary>
public class PrologueCutsceneController : MonoBehaviour
{
    [Serializable]
    public class CutsceneScene
    {
        [Tooltip("Chave da secao no JSON de localizacao. Ex: cutscene.prologue.scene_01")]
        public string sectionKey;

        [Tooltip("Sprite da imagem de fundo. Se nulo, usa apenas a cor.")]
        public Sprite backgroundSprite;

        [Tooltip("Cor do fundo (placeholder ou tinta sobre o sprite).")]
        public Color backgroundColor = Color.black;

        [Tooltip("Tempo de fade de entrada/saida da imagem.")]
        public float fadeDuration = 0.5f;
    }

    [Header("Cenas (na ordem)")]
    [SerializeField] private CutsceneScene[] scenes;

    [Header("Refs")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private CutsceneTextPlayer textPlayer;

    [Header("Comportamento")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float delayBetweenScenes = 0.3f;

    public event Action OnFinished;

    private int currentSceneIndex = -1;
    private bool isRunning;

    private void Start()
    {
        if (textPlayer != null)
            textPlayer.OnAllTextsFinished += HandleSectionFinished;

        if (playOnStart)
            BeginPrologue();
    }

    private void OnDestroy()
    {
        if (textPlayer != null)
            textPlayer.OnAllTextsFinished -= HandleSectionFinished;
    }

    public void BeginPrologue()
    {
        if (isRunning) return;
        if (scenes == null || scenes.Length == 0)
        {
            OnFinished?.Invoke();
            return;
        }
        isRunning = true;
        currentSceneIndex = -1;
        AdvanceToNextScene();
    }

    private void HandleSectionFinished()
    {
        AdvanceToNextScene();
    }

    private void AdvanceToNextScene()
    {
        currentSceneIndex++;
        if (currentSceneIndex >= scenes.Length)
        {
            isRunning = false;
            OnFinished?.Invoke();
            return;
        }
        StartCoroutine(PlaySceneRoutine(scenes[currentSceneIndex]));
    }

    private IEnumerator PlaySceneRoutine(CutsceneScene scene)
    {
        if (textPlayer != null)
            textPlayer.ClearDisplay();

        if (backgroundImage != null)
            yield return FadeBackground(scene, scene.fadeDuration);

        if (delayBetweenScenes > 0f)
            yield return new WaitForSeconds(delayBetweenScenes);

        if (textPlayer != null)
            textPlayer.StartPlayingSection(scene.sectionKey);
    }

    private IEnumerator FadeBackground(CutsceneScene scene, float duration)
    {
        Color targetTint = scene.backgroundColor;
        Color black = new Color(0f, 0f, 0f, 1f);

        if (duration <= 0f)
        {
            backgroundImage.sprite = scene.backgroundSprite;
            backgroundImage.color = targetTint;
            yield break;
        }

        float halfDuration = duration * 0.5f;

        if (backgroundImage.sprite != null)
        {
            yield return LerpColor(backgroundImage, backgroundImage.color, black, halfDuration);
        }

        backgroundImage.sprite = scene.backgroundSprite;
        backgroundImage.color = black;

        yield return LerpColor(backgroundImage, black, targetTint, halfDuration);
    }

    private IEnumerator LerpColor(Image img, Color from, Color to, float duration)
    {
        if (duration <= 0f)
        {
            img.color = to;
            yield break;
        }
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            img.color = Color.Lerp(from, to, k);
            yield return null;
        }
        img.color = to;
    }
}
