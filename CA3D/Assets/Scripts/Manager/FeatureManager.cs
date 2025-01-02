using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class FeatureManager : MonoBehaviour
{
    [Header("Feature Settings")]
    public List<FeatureSettings> featureSettings; // List of feature settings

    [Header("Terrain References")]
    public Terrain terrain;

    private TerrainData terrainData;
    private GameObject featureParent;

    private void Start()
    {
        terrainData = terrain.terrainData;
        InitializeFeatureParent();
    }

    /// <summary>
    /// Initializes the parent object for all features.
    /// </summary>
    private void InitializeFeatureParent()
    {
        if (featureParent != null)
        {
            Destroy(featureParent);
        }

        featureParent = new GameObject("FeatureParent");
        featureParent.transform.parent = transform; // Keep hierarchy tidy
    }

    /// <summary>
    /// Places features on the terrain.
    /// </summary>
    public void PlaceFeatures()
    {
        if (terrainData == null)
        {
            Debug.LogError("Terrain data is not assigned or loaded.");
            return;
        }

        // Step 1: Remove any previously instantiated features
        InitializeFeatureParent();

        // Step 2: Get terrain heightmap
        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        NativeArray<float> heightMap = FlattenHeightMap(heights);

        // Step 3: Iterate through each feature and place based on rules
        foreach (var feature in featureSettings)
        {
            if (!feature.enabled)
            {
                Debug.Log($"Skipping disabled feature: {feature.featureName}");
                continue;
            }

            NativeArray<int> placementMap = new NativeArray<int>(heightMap.Length, Allocator.TempJob);

            // Schedule placement job for the feature
            var handle = SchedulePlacementJob(feature, heightMap, placementMap);
            handle.Complete();

            // Instantiate features based on placement map
            InstantiateFeatures(feature, placementMap);

            // Dispose of placement map
            placementMap.Dispose();
        }

        // Dispose of height map
        heightMap.Dispose();
    }

    /// <summary>
    /// Clears all instantiated features.
    /// </summary>
    public void ClearFeatures()
    {
        if (featureParent != null)
        {
            Destroy(featureParent);
            InitializeFeatureParent();
        }
    }

    /// <summary>
    /// Toggles feature placement.
    /// </summary>
    public void ToggleFeatures(bool enabled)
    {
        if (enabled)
        {
            PlaceFeatures();
        }
        else
        {
            ClearFeatures();
        }
    }

    private JobHandle SchedulePlacementJob(FeatureSettings feature, NativeArray<float> heightMap, NativeArray<int> placementMap)
    {
        var job = new FeaturePlacementJob
        {
            heightMap = heightMap,
            placementMap = placementMap,
            terrainSize = new int2(terrainData.heightmapResolution, terrainData.heightmapResolution),
            heightRange = feature.heightRange,
            slopeRange = feature.slopeRange,
            spawnProbability = feature.spawnProbability,
            biomeIndex = feature.requiresBiome ? feature.biomeIndex : -1,
            random = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, int.MaxValue))
        };

        return job.Schedule(heightMap.Length, 64);
    }

    private void InstantiateFeatures(FeatureSettings feature, NativeArray<int> placementMap)
    {
        for (int i = 0; i < placementMap.Length; i++)
        {
            if (placementMap[i] == 1) // Valid placement index
            {
                int x = i % terrainData.heightmapResolution;
                int z = i / terrainData.heightmapResolution;

                Vector3 worldPosition = new Vector3(
                    x / (float)terrainData.heightmapResolution * terrainData.size.x,
                    terrainData.GetHeight(x, z),
                    z / (float)terrainData.heightmapResolution * terrainData.size.z
                );

                float scale = UnityEngine.Random.Range(feature.scaleRange.x, feature.scaleRange.y);
                float rotation = UnityEngine.Random.Range(feature.rotationRange.x, feature.rotationRange.y);

                if (feature.prefab == null)
                {
                    Debug.LogError($"Feature {feature.featureName} has no prefab assigned.");
                    continue;
                }

                GameObject instance = Instantiate(feature.prefab, worldPosition, Quaternion.Euler(0f, rotation, 0f), featureParent.transform);
                instance.transform.localScale = Vector3.one * scale;
            }
        }
    }

    private NativeArray<float> FlattenHeightMap(float[,] heights)
    {
        int width = heights.GetLength(0);
        int length = heights.GetLength(1);
        NativeArray<float> flatMap = new NativeArray<float>(width * length, Allocator.TempJob);

        for (int x = 0; x < width; x++)
            for (int y = 0; y < length; y++)
                flatMap[x + y * width] = heights[x, y];

        return flatMap;
    }
}
