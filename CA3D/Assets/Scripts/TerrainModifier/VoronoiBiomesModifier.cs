using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public class VoronoiBiomesModifier : IHeightModifier
{
    /// <summary>
    /// Implements the required ModifyHeight method for the IHeightModifier interface.
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
            // Flatten the heights array into a NativeArray
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < length; y++)
                {
                    heightsNative[x + y * width] = heights[x, y];
                }
            }

            // Schedule the Voronoi biome job
            ScheduleJob(heightsNative, biomeIndices, terrainLayerIndices, width, length, settings, default).Complete();

            // Write the results back into the heights array
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < length; y++)
                {
                    heights[x, y] = heightsNative[x + y * width];
                }
            }
        }
        finally
        {
            // Ensure all allocated memory is properly disposed
            heightsNative.Dispose();
            biomeIndices.Dispose();
            terrainLayerIndices.Dispose();
        }
    }

    /// <summary>
    /// Schedules the Voronoi biome job to process terrain heights and assign biome indices and layers.
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

        // Generate the Voronoi points and biome thresholds
        NativeArray<float2> voronoiPoints = GenerateVoronoiPoints(settings, width, length);
        NativeArray<float3x3> biomeThresholds = GenerateBiomeThresholds(settings);

        // Configure the job
        var voronoiBiomeJob = new VoronoiBiomeJob
        {
            width = width,
            length = length,
            voronoiPoints = voronoiPoints,
            biomeThresholds = biomeThresholds,
            biomeIndices = biomeIndices,
            terrainLayerIndices = terrainLayerIndices,
            heights = heights
        };

        // Schedule the job
        JobHandle jobHandle = voronoiBiomeJob.Schedule(width * length, 64, dependency);

        // Dispose of temporary data once the job completes
        voronoiPoints.Dispose(jobHandle);
        biomeThresholds.Dispose(jobHandle);

        return jobHandle;
    }

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
                throw new NotSupportedException($"Unsupported Voronoi distribution mode: {settings.voronoiDistributionMode}");
        }

        return points;
    }

    private void GenerateGridPoints(NativeArray<float2> points, int cellCount, int width, int length)
    {
        int gridSize = (int)math.sqrt(cellCount);
        float cellWidth = width / (float)gridSize;
        float cellHeight = length / (float)gridSize;

        for (int i = 0; i < cellCount; i++)
        {
            int x = i % gridSize;
            int y = i / gridSize;
            points[i] = new float2(x * cellWidth + cellWidth / 2f, y * cellHeight + cellHeight / 2f);
        }
    }

    private void GenerateRandomPoints(NativeArray<float2> points, int cellCount, int width, int length, int randomSeed)
    {
        Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)randomSeed);

        for (int i = 0; i < cellCount; i++)
        {
            points[i] = new float2(random.NextFloat(0, width), random.NextFloat(0, length));
        }
    }

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

        return thresholds;
    }

    private void ValidateSettings(TerrainGenerationSettings settings)
    {
        if (settings.biomes == null || settings.biomes.Length == 0)
            throw new ArgumentException("Biomes must be defined for Voronoi biome generation.");
        if (settings.voronoiCellCount <= 0)
            throw new ArgumentException("Voronoi cell count must be greater than 0.");
    }
}
