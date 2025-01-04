using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Handles Voronoi biome generation and terrain height modification.
/// </summary>
public class VoronoiBiomesModifier : IHeightModifier
{
    /// <summary>
    /// Schedules a Voronoi biome job to assign biome indices and terrain layers.
    /// </summary>
    public JobHandle ScheduleJob(
        NativeArray<float> heights,
        NativeArray<int> biomeIndices,
        NativeArray<int> terrainLayerIndices,
        int width,
        int length,
        TerrainGenerationSettings settings,
        JobHandle dependency)
    {
        ValidateSettings(settings);

        // Generate Voronoi data
        NativeArray<float2> voronoiPoints = GenerateVoronoiPoints(settings, width, length);
        NativeArray<float3x3> biomeThresholds = GenerateBiomeThresholds(settings);

        var voronoiBiomeJob = new VoronoiBiomeJob
        {
            width = width,
            length = length,
            voronoiPoints = voronoiPoints,
            biomeThresholds = biomeThresholds,
            maxDistance = math.sqrt(width * width + length * length),
            biomeIndices = biomeIndices,
            terrainLayerIndices = terrainLayerIndices,
            heights = heights
        };

        // Schedule the job and handle resource cleanup
        JobHandle jobHandle = voronoiBiomeJob.Schedule(width * length, 64, dependency);

        voronoiPoints.Dispose(jobHandle);
        biomeThresholds.Dispose(jobHandle);

        return jobHandle;
    }

    /// <summary>
    /// Generates Voronoi points based on the chosen distribution mode.
    /// </summary>
    private NativeArray<float2> GenerateVoronoiPoints(TerrainGenerationSettings settings, int width, int length)
    {
        int cellCount = math.clamp(settings.voronoiCellCount, 1, settings.biomes.Length);
        NativeArray<float2> points = new NativeArray<float2>(cellCount, Allocator.TempJob);

        switch (settings.voronoiDistributionMode)
        {
            case TerrainGenerationSettings.DistributionMode.Grid:
                GenerateGridPoints(points, cellCount, width, length);
                break;

            case TerrainGenerationSettings.DistributionMode.Random:
                GenerateRandomPoints(points, cellCount, width, length, settings.randomSeed);
                break;

            default:
                throw new System.NotSupportedException($"Unsupported Voronoi distribution mode: {settings.voronoiDistributionMode}");
        }

        Debug.Log($"Generated {cellCount} Voronoi points using {settings.voronoiDistributionMode} mode.");
        return points;
    }

    /// <summary>
    /// Generates Voronoi points in a grid distribution.
    /// </summary>
    private void GenerateGridPoints(NativeArray<float2> points, int cellCount, int width, int length)
    {
        int cellsX = math.ceilpow2((int)math.sqrt(cellCount));
        int cellsY = math.ceilpow2((int)math.ceil((float)cellCount / cellsX));

        for (int i = 0; i < cellCount; i++)
        {
            int x = i % cellsX;
            int y = i / cellsX;

            points[i] = new float2(
                (x + 0.5f) * (width / (float)cellsX),
                (y + 0.5f) * (length / (float)cellsY)
            );
        }
    }

    /// <summary>
    /// Generates Voronoi points in a random distribution.
    /// </summary>
    private void GenerateRandomPoints(NativeArray<float2> points, int cellCount, int width, int length, int randomSeed)
    {
        var random = new Unity.Mathematics.Random((uint)randomSeed);

        for (int i = 0; i < cellCount; i++)
        {
            points[i] = new float2(
                random.NextFloat(0, width),
                random.NextFloat(0, length)
            );
        }
    }

    /// <summary>
    /// Generates biome thresholds for the terrain layers.
    /// </summary>
    private NativeArray<float3x3> GenerateBiomeThresholds(TerrainGenerationSettings settings)
    {
        NativeArray<float3x3> thresholds = new NativeArray<float3x3>(settings.biomes.Length, Allocator.TempJob);

        for (int i = 0; i < settings.biomes.Length; i++)
        {
            var biome = settings.biomes[i];

            thresholds[i] = new float3x3(
                new float3(biome.thresholds.minHeight1, biome.thresholds.maxHeight1, 0),
                new float3(biome.thresholds.minHeight2, biome.thresholds.maxHeight2, 0),
                new float3(biome.thresholds.minHeight3, biome.thresholds.maxHeight3, 0)
            );
        }

        Debug.Log($"Generated thresholds for {settings.biomes.Length} biomes.");
        return thresholds;
    }

    /// <summary>
    /// Validates that the settings are configured correctly for Voronoi biome generation.
    /// </summary>
    private void ValidateSettings(TerrainGenerationSettings settings)
    {
        if (settings.biomes == null || settings.biomes.Length == 0)
        {
            throw new System.ArgumentException("No biomes defined in settings. Ensure that the biomes are populated.");
        }

        if (settings.voronoiCellCount <= 0)
        {
            throw new System.ArgumentException("Voronoi cell count must be greater than 0.");
        }
    }

    /// <summary>
    /// Modifies the terrain heights and applies Voronoi-based biome layers.
    /// </summary>
    public void ModifyHeight(float[,] heights, TerrainGenerationSettings settings)
    {
        int width = heights.GetLength(0);
        int length = heights.GetLength(1);

        NativeArray<float> heightsNative = new NativeArray<float>(width * length, Allocator.TempJob);
        NativeArray<int> biomeIndices = new NativeArray<int>(width * length, Allocator.TempJob);
        NativeArray<int> terrainLayerIndices = new NativeArray<int>(width * length, Allocator.TempJob);

        try
        {
            // Flatten 2D heights into a NativeArray
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < length; y++)
                {
                    heightsNative[x + y * width] = heights[x, y];
                }
            }

            // Schedule and complete the job
            ScheduleJob(heightsNative, biomeIndices, terrainLayerIndices, width, length, settings, default).Complete();
        }
        finally
        {
            // Dispose of allocated memory
            heightsNative.Dispose();
            biomeIndices.Dispose();
            terrainLayerIndices.Dispose();
        }
    }
}
