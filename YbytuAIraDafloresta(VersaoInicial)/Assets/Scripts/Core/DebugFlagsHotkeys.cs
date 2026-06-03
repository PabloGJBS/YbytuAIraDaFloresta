using UnityEngine;

/// <summary>
/// MonoBehaviour leve que escuta teclas F1/F2/F3 para alternar as flags
/// globais de debug em runtime. Adicione ao CombatGlobals.prefab pra ter
/// sempre disponivel em fases de gameplay.
/// </summary>
public class DebugFlagsHotkeys : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) DebugFlags.ShowColliders = !DebugFlags.ShowColliders;
        if (Input.GetKeyDown(KeyCode.F2)) DebugFlags.ShowAttackHitbox = !DebugFlags.ShowAttackHitbox;
        if (Input.GetKeyDown(KeyCode.F3))
        {
            bool on = !(DebugFlags.ShowColliders || DebugFlags.ShowAttackHitbox);
            DebugFlags.ShowColliders = on;
            DebugFlags.ShowAttackHitbox = on;
        }
        if (Input.GetKeyDown(KeyCode.F4)) DebugFlags.Godmode = !DebugFlags.Godmode;
    }
}
