using UnityEngine;

/// <summary>
/// "Grito" flutuante acima do inimigo (bark de combate). Agora usa o balao estilizado
/// compartilhado (SpeechBubble): caixa + borda + rabicho. Mantido como wrapper pra nao
/// mudar as chamadas existentes.
/// Uso: EnemyBark.Spawn(worldPos, "Ei, voce nao devia estar aqui!");
/// </summary>
public static class EnemyBark
{
    private static readonly Color BarkColor = new Color(1f, 0.92f, 0.55f); // amarelo claro

    // Gate global: evita falas sobrepostas (ex.: os 2 chefes gritando ao mesmo tempo).
    private static float lastBarkTime = -999f;
    private const float MinGapBetweenBarks = 2.2f;

    public static void Spawn(Vector3 worldPos, string text)
    {
        if (Time.time - lastBarkTime < MinGapBetweenBarks) return; // ja tem uma fala recente na tela
        lastBarkTime = Time.time;
        // hold maior (3.4s) pra dar tempo de ler as falas (chefes, capangas, etc).
        SpeechBubble.Pop(worldPos, text, 2.8f, BarkColor, 800, 3.4f, 0.45f, 8f);
    }
}
