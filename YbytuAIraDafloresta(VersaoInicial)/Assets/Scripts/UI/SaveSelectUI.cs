using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveSelectUI : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private SaveSlotUI[] saveSlots;

    [Header("Titulo")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private string newGameTitle = "Novo Jogo";
    [SerializeField] private string continueTitle = "Continuar";

    [Header("Input de nome (modo NewGame)")]
    [SerializeField] private GameObject nameInputPanel;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button confirmNameButton;
    [SerializeField] private Button cancelNameButton;
    [SerializeField] private int maxNameLength = 16;

    [Header("Navegacao")]
    [SerializeField] private Button backButton;

    private GameFlowManager.SaveSelectMode mode = GameFlowManager.SaveSelectMode.NewGame;
    private int pendingSlot = -1;

    private void Start()
    {
        if ((saveSlots == null || saveSlots.Length == 0) && slotsContainer != null)
            saveSlots = slotsContainer.GetComponentsInChildren<SaveSlotUI>(true);

        if (GameFlowManager.Instance != null)
            mode = GameFlowManager.Instance.PendingSaveSelectMode;

        if (titleText != null)
            titleText.text = mode == GameFlowManager.SaveSelectMode.NewGame ? newGameTitle : continueTitle;

        if (nameInputPanel != null)
            nameInputPanel.SetActive(false);

        if (nameInputField != null)
            nameInputField.characterLimit = maxNameLength;

        if (confirmNameButton != null)
            confirmNameButton.onClick.AddListener(OnConfirmName);
        if (cancelNameButton != null)
            cancelNameButton.onClick.AddListener(OnCancelName);
        if (backButton != null)
            backButton.onClick.AddListener(OnBackButton);

        RefreshSlots();
    }

    private void RefreshSlots()
    {
        if (SaveManager.Instance == null) return;

        for (int i = 0; i < saveSlots.Length && i < SaveManager.MaxSlots; i++)
        {
            bool hasSave = SaveManager.Instance.HasSave(i);
            SaveData data = hasSave ? SaveManager.Instance.LoadSave(i) : null;
            saveSlots[i].Setup(i, data);
            // cheio carrega e segue.
            saveSlots[i].SetSelectable(true);
        }
    }

    public void OnSlotSelected(int slot)
    {
        if (SaveManager.Instance == null) return;

        bool hasSave = SaveManager.Instance.HasSave(slot);
        if (!hasSave)
        {
            pendingSlot = slot;
            if (nameInputField != null)
                nameInputField.text = "";
            if (nameInputPanel != null)
                nameInputPanel.SetActive(true);
            return;
        }

        // Slot cheio: carrega e segue.
        SaveManager.Instance.SelectSave(slot);

        if (SaveManager.Instance.CurrentSave != null && !SaveManager.Instance.CurrentSave.introWatched)
        {
            if (GameFlowManager.Instance != null)
                GameFlowManager.Instance.GoToIntroCutscene();
            return;
        }

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToStageSelect();
    }

    public void OnDeleteSlot(int slot)
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSave(slot);
            RefreshSlots();
        }
    }

    private void OnConfirmName()
    {
        if (pendingSlot < 0 || SaveManager.Instance == null) return;

        string playerName = nameInputField != null ? nameInputField.text.Trim() : "";
        if (string.IsNullOrEmpty(playerName))
            playerName = $"Slot {pendingSlot + 1}";

        SaveManager.Instance.CreateNewSave(pendingSlot, playerName);

        if (nameInputPanel != null)
            nameInputPanel.SetActive(false);

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToIntroCutscene();
    }

    private void OnCancelName()
    {
        pendingSlot = -1;
        if (nameInputPanel != null)
            nameInputPanel.SetActive(false);
    }

    private void OnBackButton()
    {
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.GoToMainMenu();
    }
}
