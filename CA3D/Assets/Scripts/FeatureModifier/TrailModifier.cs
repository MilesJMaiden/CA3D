using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class TrailModifier : IFeatureModifier
{
    public JobHandle ScheduleJob(NativeArray<float> heights, int width, int length,
        TerrainGenerationSettings settings, JobHandle dependency)
    {
        var job = new TrailJob
        {
            width = width,
            length = length,
            startPoint = new float2(settings.trailStartPoint.x * width, settings.trailStartPoint.y * length),
            endPoint = new float2(settings.trailEndPoint.x * width, settings.trailEndPoint.y * length),
            trailWidth = settings.trailWidth,
            randomness = settings.trailRandomness,
            heights = heights
        };
        return job.Schedule(width * length, 64, dependency);
    }

    public void ApplyFeature(float[,] heights, TerrainGenerationSettings settings, Vector2 location, float intensity, float size)
    {
        Debug.LogWarning("ApplyFeature is deprecated. Use ScheduleJob for performance.");
    }
}
