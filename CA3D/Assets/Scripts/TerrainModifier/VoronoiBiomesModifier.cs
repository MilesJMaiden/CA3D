using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public class VoronoiBiomesModifier : IHeightModifier
{
    /// <summary>
    /// Synchronous method from IHeightModifier that modifies a 2D float array of heights directly.
    /// </summary>
    public void ModifyHeight(float[,] heights, TerrainGenerationSettings settings)
    {
        int width = heights.GetLength(0);
        int length = heights.GetLength(1);

        // Allocate NativeArrays
        NativeArray<float> heightsNative = new NativeArray<float>(width * length, Allocator.TempJob);
        NativeArray<int> biomeIndices = new NativeArray<int>(width * length, Allocator.TempJob);
        NativeArray<int> terrainLayers = new NativeArray<int>(width * length, Allocator.TempJob);

        try
        {
            // Flatten 2D -> 1D
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < length; y++)
                {
                    heightsNative[x + y * width] = heights[x, y];
                }
            }

            // Schedule the Voronoi job
            JobHandle handle = ScheduleJob(
                heightsNative,
                biomeIndices,
                terrainLayers,
                width,
                length,
                settings,
                default
            );
            handle.Complete(); // Wait for completion

            // Copy back results
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
            // Dispose all
            if (heightsNative.IsCreated) heightsNative.Dispose();
            if (biomeIndices.IsCreated) biomeIndices.Dispose();
            if (terrainLayers.IsCreated) terrainLayers.Dispose();
        }
    }

    // ---------------------------------------------------------------------
    // 1) This is the interface method your code complains is "not implemented."
    //    We create it here, matching EXACTLY the signature in IHeightModifier.
    // ---------------------------------------------------------------------
    public JobHandle ScheduleJob(
        NativeArray<float> heightsNative,
        int width,
        int length,
        TerrainGenerationSettings settings,
        JobHandle dependency)
    {
        // If the user calls this simpler signature, we create & dispose
        // the biomeIndices + terrainLayerIndices automatically.

        ValidateSettings(settings);

        // Create additional arrays needed by Voronoi
        NativeArray<int> biomeIndices = new NativeArray<int>(width * length, Allocator.TempJob);
        NativeArray<int> terrainLayers = new NativeArray<int>(width * length, Allocator.TempJob);

        // Create Voronoi points & thresholds
        NativeArray<float2> voronoiPoints = GenerateVoronoiPoints(settings, width, length);
        NativeArray<float3x3> biomeThresh = GenerateBiomeThresholds(settings);

        // Configure the job
        var job = new VoronoiBiomeJob
        {
            width = width,
            length = length,
            heights = heightsNative,
            voronoiPoints = voronoiPoints,
            biomeThresholds = biomeThresh,
            biomeIndices = biomeIndices,
            terrainLayerIndices = terrainLayers
        };

        // Schedule it
        JobHandle jobHandle = job.Schedule(width * length, 64, dependency);

        // Dispose Voronoi arrays after job
        voronoiPoints.Dispose(jobHandle);
        biomeThresh.Dispose(jobHandle);

        // Also dispose these arrays after job (since we created them)
        biomeIndices.Dispose(jobHandle);
        terrainLayers.Dispose(jobHandle);

        return jobHandle;
    }

    // ---------------------------------------------------------------------
    // 2) This is your existing "bigger" method that receives biomeIndices
    //    & terrainLayerIndices as parameters. It can remain "private" or
    //    "internal," or if you prefer, keep it public.  
    // ---------------------------------------------------------------------
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

        // Create the data needed by the job
        NativeArray<float2> voronoiPoints = GenerateVoronoiPoints(settings, width, length);
        NativeArray<float3x3> biomeThresh = GenerateBiomeThresholds(settings);

        // Configure the job
        var job = new VoronoiBiomeJob
        {
            width = width,
            length = length,
            voronoiPoints = voronoiPoints,
            biomeThresholds = biomeThresh,
            biomeIndices = biomeIndices,
            terrainLayerIndices = terrainLayerIndices,
            heights = heights
        };

        // Schedule the job
        JobHandle jobHandle = job.Schedule(width * length, 64, dependency);

        // Dispose of the temporary arrays after the job completes
        voronoiPoints.Dispose(jobHandle);
        biomeThresh.Dispose(jobHandle);

        return jobHandle;
    }

    // Existing code (GenerateVoronoiPoints, GenerateBiomeThresholds, etc.) ...
    private NativeArray<float2> GenerateVoronoiPoints(TerrainGenerationSettings settings, int width, int length)
    {
        int cellCount = math.clamp(settings.voronoiCellCount, 1, settings.biomes.Count);
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
            points[i] = new float2(
                x * cellWidth + cellWidth * 0.5f,
                y * cellHeight + cellHeight * 0.5f
            );
        }
    }

    private void GenerateRandomPoints(NativeArray<float2> points, int cellCount, int width, int length, int randomSeed)
    {
        Unity.Mathematics.Random rng = new Unity.Mathematics.Random((uint)randomSeed);

        for (int i = 0; i < cellCount; i++)
        {
            points[i] = new float2(rng.NextFloat(0, width), rng.NextFloat(0, length));
        }
    }

    private NativeArray<float3x3> GenerateBiomeThresholds(TerrainGenerationSettings settings)
    {
        NativeArray<float3x3> thresholds = new NativeArray<float3x3>(settings.biomes.Count, Allocator.TempJob);

        for (int i = 0; i < settings.biomes.Count; i++)
        {
            var b = settings.biomes[i];
            thresholds[i] = new float3x3(
                new float3(b.thresholds.minHeight1, b.thresholds.maxHeight1, 0),
                new float3(b.thresholds.minHeight2, b.thresholds.maxHeight2, 0),
                new float3(b.thresholds.minHeight3, b.thresholds.maxHeight3, 0)
            );
        }
        return thresholds;
    }

    private void ValidateSettings(TerrainGenerationSettings settings)
    {
        if (settings.biomes == null || settings.biomes.Count == 0)
            throw new ArgumentException("Biomes must be defined for Voronoi biome generation.");
        if (settings.voronoiCellCount <= 0)
            throw new ArgumentException("Voronoi cell count must be greater than 0.");
    }
}
