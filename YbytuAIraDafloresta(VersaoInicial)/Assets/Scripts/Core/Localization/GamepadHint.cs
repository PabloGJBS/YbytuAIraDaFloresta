using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class GamepadHint : MonoBehaviour
{
    [SerializeField] private string keyboardText = "Espaço ▼";
    [SerializeField] private string gamepadText = "Espaço / A ▼";

    private TMP_Text label;
    private bool lastHadGamepad;
    private bool applied;

    private void Awake() => label = GetComponent<TMP_Text>();
    private void OnEnable() => Apply(true);
    private void Update() => Apply(false);

    private void Apply(bool force)
    {
        bool hasGamepad = Gamepad.current != null;
        if (applied && !force && hasGamepad == lastHadGamepad) return;
        lastHadGamepad = hasGamepad;
        applied = true;
        if (label != null) label.text = hasGamepad ? gamepadText : keyboardText;
    }
}
