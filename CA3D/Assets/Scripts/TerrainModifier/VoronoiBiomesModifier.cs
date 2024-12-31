using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class VoronoiBiomesModifier : IHeightModifier
{
    public JobHandle ScheduleJob(
        NativeArray<float> heights,
        int width,
        int length,
        TerrainGenerationSettings settings,
        JobHandle dependency,
        out NativeArray<int> biomeIndices)
    {
        biomeIndices = new NativeArray<int>(width * length, Allocator.TempJob);

        // Generate Voronoi points
        NativeArray<float2> points = GenerateVoronoiPoints(settings, width, length);

        // Preprocess the Voronoi falloff curve into a NativeArray
        int sampleCount = 256;
        NativeArray<float> falloffSamples = new NativeArray<float>(sampleCount, Allocator.TempJob);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            falloffSamples[i] = settings.voronoiFalloffCurve.Evaluate(t);
        }

        var job = new VoronoiBiomeJob
        {
            width = width,
            length = length,
            points = points,
            maxDistance = Mathf.Max(width, length),
            heightRange = new float2(settings.voronoiHeightRange.x, settings.voronoiHeightRange.y),
            falloffSamples = falloffSamples,
            sampleCount = sampleCount,
            blendFactor = settings.voronoiBlendFactor, // Blend factor from settings
            heights = heights
        };

        JobHandle handle = job.Schedule(width * length, 64, dependency);

        // Dispose of temporary arrays
        points.Dispose(handle);
        falloffSamples.Dispose(handle);

        return handle;
    }


    public void ModifyHeight(float[,] heights, TerrainGenerationSettings settings)
    {
        // This method is required by the IHeightModifier interface.
        // Implementing as a fallback for legacy code or direct manipulation.

        Debug.LogWarning("ModifyHeight is not optimized for Voronoi Biomes. Use ScheduleJob instead.");

        int width = heights.GetLength(0);
        int length = heights.GetLength(1);

        NativeArray<float> heightsNative = new NativeArray<float>(width * length, Allocator.Temp);

        // Flatten the 2D heights array into a NativeArray
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                heightsNative[x + y * width] = heights[x, y];
            }
        }

        NativeArray<int> biomeIndices;
        ScheduleJob(heightsNative, width, length, settings, default, out biomeIndices).Complete();

        // Copy back the modified heights
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                heights[x, y] = heightsNative[x + y * width];
            }
        }

        // Dispose of NativeArrays
        heightsNative.Dispose();
        biomeIndices.Dispose();
    }

    private NativeArray<float2> GenerateVoronoiPoints(TerrainGenerationSettings settings, int width, int length)
    {
        var points = new NativeArray<float2>(settings.voronoiCellCount, Allocator.TempJob);

        // Ensure seed is non-zero
        uint seed = (uint)settings.randomSeed;
        if (seed == 0)
        {
            seed = 1; // Fallback to a default non-zero seed
            Debug.LogWarning("Random seed was zero. Using default seed of 1.");
        }

        switch (settings.voronoiDistributionMode)
        {
            case TerrainGenerationSettings.DistributionMode.Grid:
                int cellsX = (int)math.ceil(math.sqrt(settings.voronoiCellCount));
                int cellsY = cellsX;

                for (int i = 0; i < settings.voronoiCellCount; i++)
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
                Unity.Mathematics.Random random = new Unity.Mathematics.Random(seed);
                for (int i = 0; i < settings.voronoiCellCount; i++)
                {
                    points[i] = new float2(
                        random.NextFloat(0, width),
                        random.NextFloat(0, length)
                    );
                }
                break;

            case TerrainGenerationSettings.DistributionMode.Custom:
                if (settings.customVoronoiPoints.Count != settings.voronoiCellCount)
                    Debug.LogWarning("Custom Voronoi Points count doesn't match cell count.");

                for (int i = 0; i < settings.customVoronoiPoints.Count; i++)
                {
                    points[i] = new float2(
                        settings.customVoronoiPoints[i].x * width,
                        settings.customVoronoiPoints[i].y * length
                    );
                }
                break;
        }

        return points;
    }


    private NativeArray<float> PreprocessFalloffCurve(AnimationCurve curve, int sampleCount)
    {
        var samples = new NativeArray<float>(sampleCount, Allocator.TempJob);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            samples[i] = curve.Evaluate(t);
        }

        return samples;
    }
}
