using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Manages the placement of various features on the terrain.
/// Uses FeaturePlacementJob for initial spawn checks, then applies optional Cellular Automata.
/// </summary>
public class FeatureManager : MonoBehaviour
{
    [Header("Feature Settings")]
    public List<FeatureSettings> featureSettings;

    [Header("Generation Settings")]
    public TerrainGenerationSettings terrainSettings;

    [Header("Terrain References")]
    public Terrain terrain;

    public bool featuresEnabled = true;
    private TerrainData terrainData;
    private GameObject featureParent;

    private void Start()
    {
        terrainData = terrain.terrainData;
        InitializeFeatureParent();
    }

    private void InitializeFeatureParent()
    {
        if (featureParent != null)
        {
            Destroy(featureParent);
        }
        featureParent = new GameObject("FeatureParent");
        featureParent.transform.SetParent(transform);
    }

    /// <summary>
    /// Places features on the terrain. Clears existing ones, re-initializes, 
    /// generates biome indices, runs placement jobs, optionally applies CA, and instantiates.
    /// </summary>
    public void PlaceFeatures()
    {
        if (terrainData == null)
        {
            Debug.LogError("Terrain data is not assigned or loaded.");
            return;
        }

        InitializeFeatureParent();

        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        NativeArray<float> heightMap = FlattenHeightMap(heights);
        NativeArray<int> biomeIndices = GenerateBiomeIndices(heightMap);

        // Use featureCAIterations, featureNeighborThreshold, globalFeatureDensity from terrainSettings
        int caIterations = terrainSettings.featureCAIterations;
        int neighborThreshold = terrainSettings.featureNeighborThreshold;
        float globalDensity = terrainSettings.globalFeatureDensity;

        foreach (var featureEntry in terrainSettings.featureSettings)
        {
            if (featureEntry == null || featureEntry.feature == null) continue;

            if (!featureEntry.isEnabled)
            {
                Debug.Log($"Skipping disabled feature: {featureEntry.feature.featureName}");
                continue;
            }

            NativeArray<int> placementMap = new NativeArray<int>(heightMap.Length, Allocator.TempJob);

            // Schedule the job to figure out where to place this feature
            var handle = SchedulePlacementJob(featureEntry.feature, heightMap, placementMap, biomeIndices, globalDensity);
            handle.Complete(); // Wait here, so the next step can read from placementMap

            // Optionally apply a Cellular Automata pass
            ApplyCellularAutomata(placementMap, terrainData.heightmapResolution, caIterations, neighborThreshold);

            // Instantiate the features
            InstantiateFeatures(featureEntry.feature, placementMap);

            placementMap.Dispose();
        }

        heightMap.Dispose();
        biomeIndices.Dispose();
    }


    public void ClearFeatures()
    {
        if (featureParent != null)
        {
            Destroy(featureParent);
            InitializeFeatureParent();
        }
    }

    public void ToggleFeatures(bool enabled)
    {
        featuresEnabled = enabled;
        if (enabled)
        {
            PlaceFeatures();
        }
        else
        {
            ClearFeatures();
        }
    }

    private NativeArray<int> GenerateBiomeIndices(NativeArray<float> heightMap)
    {
        int resolution = terrainData.heightmapResolution;
        NativeArray<int> biomeIndices = new NativeArray<int>(resolution * resolution, Allocator.TempJob);

        if (terrainSettings.biomes == null || terrainSettings.biomes.Length == 0)
        {
            Debug.LogWarning("No biomes defined in terrain settings. Returning default indices (all 0).");
            // They remain 0 by default
            return biomeIndices;
        }

        for (int i = 0; i < biomeIndices.Length; i++)
        {
            float h = heightMap[i];
            int biomeIndex = -1;
            for (int j = 0; j < terrainSettings.biomes.Length; j++)
            {
                var th = terrainSettings.biomes[j].thresholds;
                // If h is in any of the 3 threshold ranges
                if ((h >= th.minHeight1 && h <= th.maxHeight1) ||
                    (h >= th.minHeight2 && h <= th.maxHeight2) ||
                    (h >= th.minHeight3 && h <= th.maxHeight3))
                {
                    biomeIndex = j;
                    break;
                }
            }
            biomeIndices[i] = biomeIndex;
        }

        return biomeIndices;
    }

    /// <summary>
    /// Schedules the FeaturePlacementJob for a specific feature, factoring in global spawn density.
    /// </summary>
    private JobHandle SchedulePlacementJob(FeatureSettings feature,
        NativeArray<float> heightMap,
        NativeArray<int> placementMap,
        NativeArray<int> biomeIndices,
        float globalDensity)
    {
        var job = new FeaturePlacementJob
        {
            heightMap = heightMap,
            placementMap = placementMap,
            biomeIndices = biomeIndices,
            terrainSize = new int2(terrainData.heightmapResolution, terrainData.heightmapResolution),
            heightRange = feature.heightRange,
            slopeRange = feature.slopeRange,
            // Multiply feature's spawnProbability by globalDensity
            spawnProbability = feature.spawnProbability * globalDensity,
            biomeIndex = feature.requiresBiome ? feature.biomeIndex : -1,
            random = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, int.MaxValue))
        };
        return job.Schedule(heightMap.Length, 64);
    }

    private void InstantiateFeatures(FeatureSettings feature, NativeArray<int> placementMap)
    {
        int resolution = terrainData.heightmapResolution;
        for (int i = 0; i < placementMap.Length; i++)
        {
            if (placementMap[i] == 1)
            {
                int x = i % resolution;
                int z = i / resolution;

                Vector3 worldPos = new Vector3(
                    x / (float)resolution * terrainData.size.x,
                    terrainData.GetHeight(x, z),
                    z / (float)resolution * terrainData.size.z
                );

                float scale = UnityEngine.Random.Range(feature.scaleRange.x, feature.scaleRange.y);
                float rotation = UnityEngine.Random.Range(feature.rotationRange.x, feature.rotationRange.y);

                if (feature.prefab == null)
                {
                    Debug.LogError($"Feature {feature.featureName} has no prefab assigned.");
                    continue;
                }

                GameObject instance = Instantiate(feature.prefab, worldPos,
                    Quaternion.Euler(0f, rotation, 0f), featureParent.transform);
                instance.transform.localScale = Vector3.one * scale;
            }
        }
    }

    private NativeArray<float> FlattenHeightMap(float[,] heights)
    {
        int w = heights.GetLength(0);
        int h = heights.GetLength(1);
        NativeArray<float> flatMap = new NativeArray<float>(w * h, Allocator.TempJob);

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                flatMap[x + y * w] = heights[x, y];
            }
        }
        return flatMap;
    }

    /// <summary>
    /// Uses a simple Cellular Automata pass to refine the placement map.
    /// </summary>
    private void ApplyCellularAutomata(NativeArray<int> placementMap, int resolution, int iterations, int neighborThreshold)
    {
        if (iterations <= 0) return; // No CA if zero or negative

        NativeArray<int> oldMap = new NativeArray<int>(placementMap.Length, Allocator.Temp);
        NativeArray<int> newMap = new NativeArray<int>(placementMap.Length, Allocator.Temp);

        placementMap.CopyTo(oldMap);

        for (int step = 0; step < iterations; step++)
        {
            for (int i = 0; i < oldMap.Length; i++)
            {
                int x = i % resolution;
                int y = i / resolution;
                int neighbors = 0;

                for (int nx = x - 1; nx <= x + 1; nx++)
                {
                    for (int ny = y - 1; ny <= y + 1; ny++)
                    {
                        if (nx < 0 || ny < 0 || nx >= resolution || ny >= resolution) continue;
                        if (nx == x && ny == y) continue;
                        int idx = nx + ny * resolution;
                        if (oldMap[idx] == 1) neighbors++;
                    }
                }

                newMap[i] = (neighbors >= neighborThreshold) ? 1 : 0;
            }
            newMap.CopyTo(oldMap);
        }

        newMap.CopyTo(placementMap);
        oldMap.Dispose();
        newMap.Dispose();
    }
}
