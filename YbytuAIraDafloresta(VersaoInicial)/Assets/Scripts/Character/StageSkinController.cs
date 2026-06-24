using UnityEngine;

public class StageSkinController : MonoBehaviour
{
    [SerializeField] private StageSkin currentSkin;

    [Header("Referencia das Layers")]
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private SpriteRenderer foregroundRenderer;

    public StageSkin CurrentSkin => currentSkin;

    private void Awake()
    {
        if (currentSkin != null)
            ApplySkin(currentSkin);
    }

    public void ApplySkin(StageSkin skin)
    {
        currentSkin = skin;

        if (backgroundRenderer != null && skin.background != null)
            backgroundRenderer.sprite = skin.background;

        if (foregroundRenderer != null && skin.foreground != null)
            foregroundRenderer.sprite = skin.foreground;
    }
}
