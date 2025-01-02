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

    private void Start()
    {
        terrainData = terrain.terrainData;
        PlaceFeatures();
    }

    public void PlaceFeatures()
    {
        // Step 1: Get terrain data
        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        NativeArray<float> heightMap = FlattenHeightMap(heights);

        // Step 2: Iterate through each feature and place based on rules
        foreach (var feature in featureSettings)
        {
            if (!feature.enabled) continue;

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
            if (placementMap[i] == 1) // Placement map marks valid locations with 1
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

                GameObject instance = Instantiate(feature.prefab, worldPosition, Quaternion.Euler(0f, rotation, 0f));
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
