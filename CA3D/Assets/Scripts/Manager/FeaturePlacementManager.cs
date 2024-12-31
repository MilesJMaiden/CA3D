using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class FeaturePlacementManager : MonoBehaviour
{
    [Header("Feature Settings")]
    public FeatureDefinition[] featureDefinitions;
    public Terrain terrain;

    private TerrainData terrainData;

    private void Start()
    {
        terrainData = terrain.terrainData;
        PlaceFeaturesWithCA();
    }

    public void PlaceFeaturesWithCA()
    {
        // Step 1: Flatten height map and initialize biome indices
        float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
        NativeArray<float> heightMap = FlattenHeightMap(heights);
        NativeArray<int> biomeIndices = new NativeArray<int>(heightMap.Length, Allocator.TempJob); // Changed to NativeArray<int>
        NativeArray<int> caGrid = new NativeArray<int>(heightMap.Length, Allocator.TempJob); // Changed to NativeArray<int>

        // Step 2: Run CA job for feature placement
        var caHandle = ScheduleCAJob(heightMap, biomeIndices, caGrid);
        caHandle.Complete();

        // Step 3: Instantiate features based on CA results
        InstantiateFeaturesFromCA(caGrid);

        // Step 4: Dispose NativeArrays
        DisposeNativeArrays(heightMap);
        DisposeNativeArrays(biomeIndices, caGrid);
    }

    private JobHandle ScheduleCAJob(
        NativeArray<float> heightMap,
        NativeArray<int> biomeIndices, // Changed to NativeArray<int>
        NativeArray<int> caGrid) // Changed to NativeArray<int>
    {
        var caJob = new CellularAutomataFeaturePlacementJob
        {
            heightMap = heightMap,
            biomeIndices = biomeIndices,
            caGrid = caGrid,
            terrainSize = new int2(terrainData.heightmapResolution, terrainData.heightmapResolution),
            random = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, int.MaxValue))
        };

        return caJob.Schedule(heightMap.Length, 64);
    }

    private void InstantiateFeaturesFromCA(NativeArray<int> caGrid) // Changed to NativeArray<int>
    {
        for (int i = 0; i < caGrid.Length; i++)
        {
            int featureIndex = caGrid[i];
            if (featureIndex >= 0)
            {
                int x = i % terrainData.heightmapResolution;
                int z = i / terrainData.heightmapResolution;

                Vector3 worldPosition = new Vector3(
                    x / (float)terrainData.heightmapResolution * terrainData.size.x,
                    terrainData.GetHeight(x, z),
                    z / (float)terrainData.heightmapResolution * terrainData.size.z
                );

                Instantiate(featureDefinitions[featureIndex].prefab, worldPosition, Quaternion.identity);
            }
        }
    }

    private NativeArray<float> FlattenHeightMap(float[,] heights)
    {
        int width = heights.GetLength(0);
        int length = heights.GetLength(1);
        NativeArray<float> flatMap = new NativeArray<float>(width * length, Allocator.TempJob);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                flatMap[x + y * width] = heights[x, y];
            }
        }

        return flatMap;
    }

    private void DisposeNativeArrays(params NativeArray<float>[] arrays)
    {
        foreach (var array in arrays)
        {
            if (array.IsCreated) array.Dispose();
        }
    }

    private void DisposeNativeArrays(params NativeArray<int>[] arrays)
    {
        foreach (var array in arrays)
        {
            if (array.IsCreated) array.Dispose();
        }
    }
}
