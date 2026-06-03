/// <summary>
/// Flags estaticas globais para toggles de debug visual.
/// Componentes de debug (ColliderDebugDraw, PlayerController.OnDrawGizmos)
/// leem estas flags antes de desenhar.
/// Atalhos via DebugFlagsHotkeys: F1 alterna colliders, F2 hitbox, F3 ambos.
/// </summary>
public static class DebugFlags
{
    public static bool ShowColliders = true;
    public static bool ShowAttackHitbox = true;

    /// <summary>Player nao recebe dano (debug). Padrao false. Toggle em runtime: F4.</summary>
    public static bool Godmode = false;
}
