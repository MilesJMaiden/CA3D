using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class ThermalErosionModifier : IFeatureModifier
{
    public JobHandle ScheduleJob(NativeArray<float> heights, int width, int length, TerrainGenerationSettings settings, JobHandle dependency)
    {
        var job = new ThermalErosionJob
        {
            width = width,
            length = length,
            talusAngle = settings.talusAngle,
            iterations = settings.erosionIterations,
            heights = heights
        };

        return job.Schedule(width * length, 64, dependency);
    }

    public void ApplyFeature(float[,] heights, TerrainGenerationSettings settings, Vector2 location, float intensity, float size)
    {
        Debug.LogWarning("ApplyFeature is deprecated. Use ScheduleJob for performance.");
    }
}
