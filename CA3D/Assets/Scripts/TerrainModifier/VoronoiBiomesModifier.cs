using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class VoronoiBiomesModifier : IHeightModifier
{
    /// <summary>
    /// Schedules a job to apply Voronoi biome modifications.
    /// </summary>
    /// <param name="heights">NativeArray of terrain heights.</param>
    /// <param name="biomeIndices">NativeArray to store biome indices.</param>
    /// <param name="terrainLayerIndices">NativeArray to store terrain layer indices.</param>
    /// <param name="width">The width of the terrain.</param>
    /// <param name="length">The length of the terrain.</param>
    /// <param name="settings">The terrain generation settings.</param>
    /// <param name="dependency">JobHandle for dependencies.</param>
    /// <returns>A JobHandle representing the scheduled job.</returns>
    public JobHandle ScheduleJob(
        NativeArray<float> heights,
        NativeArray<int> biomeIndices,
        NativeArray<int> terrainLayerIndices,
        int width,
        int length,
        TerrainGenerationSettings settings,
        JobHandle dependency)
    {
        if (settings.biomes == null || settings.biomes.Length == 0)
        {
            Debug.LogError("No biomes defined in settings. Cannot proceed with Voronoi biomes modification.");
            return dependency;
        }

        // Generate Voronoi points and biome thresholds
        NativeArray<float2> voronoiPoints = GenerateVoronoiPoints(settings, width, length);
        NativeArray<float3x3> biomeThresholds = GenerateBiomeThresholds(settings);

        var job = new VoronoiBiomeJob
        {
            width = width,
            length = length,
            voronoiPoints = voronoiPoints,
            biomeThresholds = biomeThresholds,
            maxDistance = Mathf.Max(width, length),
            biomeIndices = biomeIndices,
            terrainLayerIndices = terrainLayerIndices,
            heights = heights
        };

        // Schedule the job and dispose of temporary arrays after completion
        JobHandle handle = job.Schedule(width * length, 64, dependency);

        voronoiPoints.Dispose(handle);
        biomeThresholds.Dispose(handle);

        return handle;
    }

    /// <summary>
    /// Generates Voronoi points for the terrain.
    /// </summary>
    /// <param name="settings">The terrain generation settings.</param>
    /// <param name="width">The width of the terrain.</param>
    /// <param name="length">The length of the terrain.</param>
    /// <returns>A NativeArray of Voronoi points.</returns>
    private NativeArray<float2> GenerateVoronoiPoints(TerrainGenerationSettings settings, int width, int length)
    {
        // Clamp voronoiCellCount to the number of biomes
        int cellCount = Mathf.Clamp(settings.voronoiCellCount, 1, settings.biomes.Length);

        var points = new NativeArray<float2>(cellCount, Allocator.TempJob);

        switch (settings.voronoiDistributionMode)
        {
            case TerrainGenerationSettings.DistributionMode.Grid:
                int cellsX = (int)Mathf.Sqrt(cellCount);
                int cellsY = cellsX;

                for (int i = 0; i < cellCount; i++)
                {
                    int x = i % cellsX;
                    int y = i / cellsX;
                    points[i] = new float2(
                        (x + 0.5f) * (width / (float)cellsX),
                        (y + 0.5f) * (length / (float)cellsY)
                    );
                }
                break;

            case TerrainGenerationSettings.DistributionMode.Random:
                Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)settings.randomSeed);
                for (int i = 0; i < cellCount; i++)
                {
                    points[i] = new float2(random.NextFloat(0, width), random.NextFloat(0, length));
                }
                break;

            case TerrainGenerationSettings.DistributionMode.Custom:
                if (settings.biomes.Length != settings.voronoiCellCount)
                {
                    Debug.LogWarning($"Custom mode expects {settings.biomes.Length} cells, but got {settings.voronoiCellCount}. Adjusting.");
                }

                for (int i = 0; i < cellCount; i++)
                {
                    float normalizedX = (i / (float)cellCount) * width;
                    float normalizedY = (i % cellCount) * length;
                    points[i] = new float2(normalizedX, normalizedY);
                }
                break;

            default:
                Debug.LogError("Unsupported Voronoi distribution mode.");
                break;
        }

        return points;
    }


    /// <summary>
    /// Generates biome thresholds for terrain layers.
    /// </summary>
    /// <param name="settings">The terrain generation settings.</param>
    /// <returns>A NativeArray of biome thresholds.</returns>
    private NativeArray<float3x3> GenerateBiomeThresholds(TerrainGenerationSettings settings)
    {
        var thresholds = new NativeArray<float3x3>(settings.biomes.Length, Allocator.TempJob);

        for (int i = 0; i < settings.biomes.Length; i++)
        {
            var biome = settings.biomes[i];
            thresholds[i] = new float3x3(
                new float3(biome.thresholds.minHeight1, biome.thresholds.maxHeight1, 0),
                new float3(biome.thresholds.minHeight2, biome.thresholds.maxHeight2, 0),
                new float3(biome.thresholds.minHeight3, biome.thresholds.maxHeight3, 0)
            );
        }

        return thresholds;
    }

    public void ModifyHeight(float[,] heights, TerrainGenerationSettings settings)
    {
        int width = heights.GetLength(0);
        int length = heights.GetLength(1);

        NativeArray<float> heightsNative = new NativeArray<float>(width * length, Allocator.TempJob);
        NativeArray<int> biomeIndices = new NativeArray<int>(width * length, Allocator.TempJob);
        NativeArray<int> terrainLayerIndices = new NativeArray<int>(width * length, Allocator.TempJob);

        // Flatten 2D heights array into a NativeArray
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                heightsNative[x + y * width] = heights[x, y];
            }
        }

        // Schedule and execute the job
        ScheduleJob(heightsNative, biomeIndices, terrainLayerIndices, width, length, settings, default).Complete();

        // Dispose NativeArrays
        heightsNative.Dispose();
        biomeIndices.Dispose();
        terrainLayerIndices.Dispose();
    }
}
