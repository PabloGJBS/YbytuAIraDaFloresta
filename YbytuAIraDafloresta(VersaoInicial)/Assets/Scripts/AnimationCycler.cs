using UnityEngine;

public class AnimationCycler : MonoBehaviour
{
    public string[] states;
    public float interval = 3f;
    public float labelY = 3.4f;
    public float labelSize = 0.18f;

    private Animator anim;
    private TextMesh label;
    private int idx = -1;
    private float t = 0f;

    private void Start()
    {
        anim = GetComponent<Animator>();

        var go = new GameObject(gameObject.name + "_Label");
        go.transform.position = new Vector3(transform.position.x, labelY, 0f);
        label = go.AddComponent<TextMesh>();
        label.characterSize = labelSize;
        label.fontSize = 64;
        label.anchor = TextAnchor.LowerCenter;
        label.alignment = TextAlignment.Center;
        label.color = Color.white;
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) { mr.sortingOrder = 1000; }

        Next();
    }

    private void Next()
    {
        if (anim == null || states == null || states.Length == 0) return;
        idx = (idx + 1) % states.Length;
        anim.Play(states[idx], 0, 0f);
        UpdateLabel();
        t = 0f;
    }

    private void UpdateLabel()
    {
        if (label == null) return;
        string s = (states != null && states.Length > 0) ? states[idx < 0 ? 0 : idx] : "-";
        label.text = gameObject.name + "\n[" + s + "]";
    }

    private void Update()
    {
        if (states == null || states.Length == 0) return;
        if (states.Length == 1) { UpdateLabel(); return; }
        t += Time.deltaTime;
        if (t >= interval) Next();
    }
}
