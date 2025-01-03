using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Cinemachine.CinemachineDecollider;

public class FeatureManager : MonoBehaviour
{
    [Header("Feature Settings")]
    public List<FeatureSettings> featureSettings; // List of feature settings

    [Header("Generation Settings")]
    public TerrainGenerationSettings terrainSettings;

    [Header("Terrain References")]
    public Terrain terrain;

    public bool featuresEnabled = true; // Track the toggle state
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
        featureParent.transform.SetParent(transform);
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

        InitializeFeatureParent();

        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        NativeArray<float> heightMap = FlattenHeightMap(heights);

        // Generate biome indices
        NativeArray<int> biomeIndices = GenerateBiomeIndices(heightMap);

        foreach (var feature in featureSettings)
        {
            if (!feature.enabled)
            {
                Debug.Log($"Skipping disabled feature: {feature.featureName}");
                continue;
            }

            NativeArray<int> placementMap = new NativeArray<int>(heightMap.Length, Allocator.TempJob);

            var handle = SchedulePlacementJob(feature, heightMap, placementMap, biomeIndices);
            handle.Complete();

            InstantiateFeatures(feature, placementMap);

            placementMap.Dispose();
        }

        heightMap.Dispose();
        biomeIndices.Dispose();
    }

    /// <summary>
    /// Generates biome indices based on the heightmap.
    /// </summary>
    /// <param name="heightMap">NativeArray of terrain height values.</param>
    /// <returns>A NativeArray containing biome indices for each point.</returns>
    private NativeArray<int> GenerateBiomeIndices(NativeArray<float> heightMap)
    {
        int resolution = terrainData.heightmapResolution;
        NativeArray<int> biomeIndices = new NativeArray<int>(resolution * resolution, Allocator.TempJob);

        if (terrainSettings.biomes == null || terrainSettings.biomes.Length == 0)
        {
            Debug.LogWarning("No biomes defined in terrain settings. Returning default indices.");
            return biomeIndices; // All indices will be initialized to their default value of 0.
        }

        for (int i = 0; i < biomeIndices.Length; i++)
        {
            float height = heightMap[i];
            int biomeIndex = -1;

            // Iterate through biomes and check their thresholds
            for (int j = 0; j < terrainSettings.biomes.Length; j++)
            {
                var thresholds = terrainSettings.biomes[j].thresholds;

                if ((height >= thresholds.minHeight1 && height <= thresholds.maxHeight1) ||
                    (height >= thresholds.minHeight2 && height <= thresholds.maxHeight2) ||
                    (height >= thresholds.minHeight3 && height <= thresholds.maxHeight3))
                {
                    biomeIndex = j;
                    break; // Stop checking once a matching biome is found
                }
            }

            // Assign the determined biome index (or -1 if no match was found)
            biomeIndices[i] = biomeIndex;
        }

        return biomeIndices;
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

    private JobHandle SchedulePlacementJob(FeatureSettings feature, NativeArray<float> heightMap, NativeArray<int> placementMap, NativeArray<int> biomeIndices)
    {
        var job = new FeaturePlacementJob
        {
            heightMap = heightMap,
            placementMap = placementMap,
            terrainSize = new int2(terrainData.heightmapResolution, terrainData.heightmapResolution),
            heightRange = feature.heightRange,
            slopeRange = feature.slopeRange,
            spawnProbability = feature.spawnProbability,
            biomeIndex = feature.requiresBiome ? feature.biomeIndex : -1, // Check for required biome
            biomeIndices = biomeIndices, // Pass biome indices
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
