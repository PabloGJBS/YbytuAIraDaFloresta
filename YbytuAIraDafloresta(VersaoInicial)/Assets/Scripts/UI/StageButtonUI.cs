using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageButtonUI : MonoBehaviour
{
    [SerializeField] private TMP_Text stageNameText;
    [SerializeField] private Image stageIcon;
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private GameObject completedMark;
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private Button button;

    private StageData stageData;

    public void Setup(StageData data, bool unlocked, StageProgress progress)
    {
        stageData = data;

        if (stageNameText != null)
            stageNameText.text = data.stageName;
        if (stageIcon != null && data.stageIcon != null)
            stageIcon.sprite = data.stageIcon;
        if (lockOverlay != null)
            lockOverlay.SetActive(!unlocked);
        if (button != null)
            button.interactable = unlocked;

        bool completed = progress != null && progress.completed;
        if (completedMark != null) completedMark.SetActive(completed);
        if (rankText != null)
            rankText.text = completed ? progress.rankGrade : "";
    }

    public void OnClicked()
    {
        if (stageData == null) return;
        var parent = GetComponentInParent<StageSelectUI>();
        if (parent != null) parent.OnStageSelected(stageData);
    }
}
