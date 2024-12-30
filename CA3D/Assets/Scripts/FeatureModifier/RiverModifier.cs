using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class RiverModifier : IFeatureModifier
{
    public JobHandle ScheduleJob(NativeArray<float> heights, int width, int length, TerrainGenerationSettings settings, JobHandle dependency)
    {
        var job = new RiverJob
        {
            width = width,
            length = length,
            startHeight = settings.riverHeight,
            riverWidth = settings.riverWidth,
            heights = heights
        };

        return job.Schedule(width * length, 64, dependency);
    }

    public void ApplyFeature(float[,] heights, TerrainGenerationSettings settings, Vector2 location, float intensity, float size)
    {
        Debug.LogWarning("ApplyFeature is deprecated. Use ScheduleJob for performance.");
    }
}
