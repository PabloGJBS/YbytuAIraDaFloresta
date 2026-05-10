using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente visual de um slot de save individual.
/// </summary>
public class SaveSlotUI : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    private const string IsSelectedParam = "isSelected";
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip burningSfx;
    [SerializeField, Range(0f, 1f)] private float burningSfxVolume = 0.7f;
    [SerializeField] private TMP_Text slotLabel;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text lastPlayedText;
    [SerializeField] private GameObject emptyState;
    [SerializeField] private GameObject filledState;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button deleteButton;

    private int slotIndex;
    private bool isBurning;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (selectButton != null)
            selectButton.onClick.AddListener(OnSelectClicked);
        if (deleteButton != null)
            deleteButton.onClick.AddListener(OnDeleteClicked);
    }

    public void OnSelect(BaseEventData eventData) => SetBurning(true);
    public void OnDeselect(BaseEventData eventData) => SetBurning(false);
    public void OnPointerEnter(PointerEventData eventData) => SetBurning(true);
    public void OnPointerExit(PointerEventData eventData)
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
            return;
        SetBurning(false);
    }

    private void SetBurning(bool on)
    {
        if (on == isBurning) return;
        isBurning = on;
        if (animator != null) animator.SetBool(IsSelectedParam, on);
        if (audioSource == null || burningSfx == null) return;
        if (on)
        {
            audioSource.clip = burningSfx;
            audioSource.loop = true;
            audioSource.volume = burningSfxVolume;
            audioSource.Play();
        }
        else
        {
            audioSource.Stop();
        }
    }

    public void Setup(int index, SaveData data)
    {
        slotIndex = index;

        bool hasSave = data != null;

        if (emptyState != null) emptyState.SetActive(false);
        if (filledState != null) filledState.SetActive(true);
        if (deleteButton != null) deleteButton.gameObject.SetActive(hasSave);

        if (slotLabel != null)
            slotLabel.text = $"Save {index + 1}";

        if (playerNameText != null)
            playerNameText.text = "Pontuação";

        if (progressText != null)
        {
            if (hasSave)
            {
                int total = 0;
                foreach (var p in data.stageProgress) total += p.bestScore;
                progressText.text = total.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
            }
            else
            {
                progressText.text = "-";
            }
        }

        if (lastPlayedText != null)
            lastPlayedText.text = hasSave ? data.lastPlayedAt : "Sem registros";
    }

    public void SetSelectable(bool selectable)
    {
        if (selectButton != null) selectButton.interactable = selectable;
    }

    public void OnSelectClicked()
    {
        var parent = GetComponentInParent<SaveSelectUI>();
        if (parent != null) parent.OnSlotSelected(slotIndex);
    }

    public void OnDeleteClicked()
    {
        var parent = GetComponentInParent<SaveSelectUI>();
        if (parent != null) parent.OnDeleteSlot(slotIndex);
    }
}
