using UnityEngine;
using System;
using System.Collections;

[Serializable]
public class SpawnWave
{
    public string waveName;
    public SpawnEntry[] enemies;
    [Tooltip("Delay em segundos antes de iniciar esta wave")]
    public float delayBeforeWave = 1f;
    [Tooltip("Intervalo entre cada spawn dentro da wave")]
    public float spawnInterval = 0.5f;
}

[Serializable]
public class SpawnEntry
{
    public GameObject enemyPrefab;
    public int count = 1;
}
