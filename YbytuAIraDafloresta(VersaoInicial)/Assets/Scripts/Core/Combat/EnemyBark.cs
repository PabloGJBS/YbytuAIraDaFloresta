using UnityEngine;

public static class EnemyBark
{
    private static readonly Color BarkColor = new Color(1f, 0.92f, 0.55f); // amarelo claro

    private static float lastBarkTime = -999f;
    private const float MinGapBetweenBarks = 2.2f;

    public static void Spawn(Vector3 worldPos, string text)
    {
        if (Time.time - lastBarkTime < MinGapBetweenBarks) return;
        lastBarkTime = Time.time;
        SpeechBubble.Pop(worldPos, text, 2.8f, BarkColor, 800, 3.4f, 0.45f, 8f);
    }
}
