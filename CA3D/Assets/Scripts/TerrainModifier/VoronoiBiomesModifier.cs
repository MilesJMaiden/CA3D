using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class VoronoiBiomesModifier : IHeightModifier
{
    public JobHandle ScheduleJob(NativeArray<float> heights, int width, int length, TerrainGenerationSettings settings, JobHandle dependency)
    {
        // Preprocess the Voronoi falloff curve into a NativeArray
        int sampleCount = 256; // Number of points to sample from the curve
        NativeArray<float> falloffSamples = new NativeArray<float>(sampleCount, Allocator.TempJob);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1); // Normalize index to range [0, 1]
            falloffSamples[i] = settings.voronoiFalloffCurve.Evaluate(t);
        }

        NativeArray<float2> points = new NativeArray<float2>(settings.customVoronoiPoints.Count, Allocator.TempJob);
        for (int i = 0; i < settings.customVoronoiPoints.Count; i++)
        {
            points[i] = new float2(settings.customVoronoiPoints[i].x, settings.customVoronoiPoints[i].y);
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
            heights = heights
        };

        JobHandle handle = job.Schedule(width * length, 64, dependency);
        points.Dispose(handle);
        falloffSamples.Dispose(handle);
        return handle;
    }

    public void ModifyHeight(float[,] heights, TerrainGenerationSettings settings)
    {
        // Legacy implementation for compatibility
        Debug.LogWarning("ModifyHeight is deprecated when using ScheduleJob.");
    }
}
