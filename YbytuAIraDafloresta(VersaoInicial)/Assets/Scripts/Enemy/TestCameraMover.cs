using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Move a camera livremente numa cena de TESTE (WASD/setas pra pan, Q/E pra zoom).
/// Pra inspecionar varios pontos sem precisar andar com o personagem.
/// </summary>
public class TestCameraMover : MonoBehaviour
{
    public float panSpeed = 14f;
    public float zoomSpeed = 10f;
    public float minZoom = 2f;
    public float maxZoom = 30f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        Vector3 move = Vector3.zero;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
        if (move.sqrMagnitude > 0.001f)
            transform.position += move.normalized * panSpeed * Time.deltaTime;

        if (cam != null && cam.orthographic)
        {
            float z = 0f;
            if (kb.qKey.isPressed) z += 1f;   // afasta
            if (kb.eKey.isPressed) z -= 1f;   // aproxima
            var scroll = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
            if (Mathf.Abs(scroll) > 0.01f) z -= Mathf.Sign(scroll);
            if (Mathf.Abs(z) > 0.001f)
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize + z * zoomSpeed * Time.deltaTime, minZoom, maxZoom);
        }
    }
}
