using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class LakeModifier : IFeatureModifier
{
    public JobHandle ScheduleJob(NativeArray<float> heights, int width, int length, TerrainGenerationSettings settings, JobHandle dependency)
    {
        var job = new LakeJob
        {
            width = width,
            length = length,
            center = new float2(settings.lakeCenter.x * width, settings.lakeCenter.y * length),
            lakeRadius = settings.lakeRadius,
            waterLevel = settings.lakeWaterLevel,
            heights = heights
        };

        return job.Schedule(width * length, 64, dependency);
    }

    public void ApplyFeature(float[,] heights, TerrainGenerationSettings settings, Vector2 location, float intensity, float size)
    {
        Debug.LogWarning("ApplyFeature is deprecated. Use ScheduleJob for performance.");
    }
}
