using UnityEngine;

public class DirectPlayBootstrap : MonoBehaviour
{
    [Tooltip("Fase usada SO no teste isolado (Play direto nesta cena). No fluxo real e ignorada.")]
    [SerializeField] private StageData stageData;
    [Tooltip("Prefab do AudioManager (SoundManager). So pra teste isolado: garante audio (musica/SFX) quando a cena abre sem o fluxo do menu. No fluxo real o SoundManager ja existe e isto e ignorado.")]
    [SerializeField] private GameObject audioManagerPrefab;

    private void Awake()
    {
        if (SoundManager.Instance == null && audioManagerPrefab != null)
            Instantiate(audioManagerPrefab);

        if (stageData != null)
        {
            if (GameFlowManager.Instance == null)
                new GameObject("GameFlowManager (DirectPlay)").AddComponent<GameFlowManager>();
            GameFlowManager.Instance?.PrimeDirectPlay(stageData);
        }
    }
}
