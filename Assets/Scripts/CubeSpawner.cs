using UnityEngine;
using System.Collections.Generic;

public class CubeSpawner : MonoBehaviour
{
    public GameObject cubePrefab;
    public CubeAppearanceManager appearanceManager;
    public float groundYLevel = 0f;
    public int initialSpawnCount = 20;
    public float spawnAreaSize = 50f;
    public float spawnInterval = 5f;
    public List<int> possibleValues = new List<int> { 2, 4, 8, 16, 32, 64, 128, 256, 512 };
    public int maxPlayerValueForSpawning = 512;

    float spawnTimer;
    public CubeController player;

    void Start()
    {
        if (appearanceManager == null)
        {
            appearanceManager = FindFirstObjectByType<CubeAppearanceManager>();
        }

        SpawnInitialCubes();
    }

    void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnRandomCube();
            spawnTimer = 0f;
        }
    }

    void SpawnInitialCubes()
    {
        for (int i = 0; i < initialSpawnCount; i++)
        {
            SpawnRandomCube();
        }
    }

    void SpawnRandomCube()
    {
        Vector3 spawnPosition = GetRandomSpawnPosition();
        int randomValue = GetWeightedRandomValue();

        GameObject newCube = Instantiate(cubePrefab, spawnPosition, Quaternion.identity);
        CubeController cubeController = newCube.GetComponent<CubeController>();

        if (cubeController != null)
        {
            if (appearanceManager != null)
            {
                cubeController.appearanceManager = appearanceManager;
            }
            cubeController.SetValue(randomValue);
        }
    }

    Vector3 GetRandomSpawnPosition()
    {
        float x = Random.Range(-spawnAreaSize / 2f, spawnAreaSize / 2f);
        float z = Random.Range(-spawnAreaSize / 2f, spawnAreaSize / 2f);

        return new Vector3(x, groundYLevel, z);
    }

    int GetWeightedRandomValue()
    {
        if (possibleValues.Count == 0) return 2;

        int playerValue = 2;
        playerValue = player.currentValue;

        List<int> availableValues = new List<int>();
        List<float> weights = new List<float>();

        foreach (int value in possibleValues)
        {
            if (value <= playerValue * 2 && value <= maxPlayerValueForSpawning)
            {
                availableValues.Add(value);

                float weight = 1f;
                if (value == 2) weight = 4f;
                else if (value == 4) weight = 3f;
                else if (value == 8) weight = 2f;
                else if (value == playerValue) weight = 1.5f;

                weights.Add(weight);
            }
        }

        if (availableValues.Count == 0)
        {
            availableValues.Add(2);
            weights.Add(1f);
        }

        float totalWeight = 0f;
        foreach (float weight in weights)
        {
            totalWeight += weight;
        }

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        for (int i = 0; i < availableValues.Count; i++)
        {
            currentWeight += weights[i];
            if (randomValue <= currentWeight)
            {
                return availableValues[i];
            }
        }

        return availableValues[0];
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector3(transform.position.x, groundYLevel, transform.position.z),
                           new Vector3(spawnAreaSize, 0.1f, spawnAreaSize));
    }
}
