using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class LakeModifier : IFeatureModifier
{
    public JobHandle ScheduleJob(NativeArray<float> heights, int width, int length, TerrainGenerationSettings settings, JobHandle dependency)
    {
        if (settings.lakeRadius <= 0f || settings.lakeWaterLevel < 0f)
        {
            // Skip
            return dependency;
        }

        float2 lakeCenter = new float2(settings.lakeCenter.x * width, settings.lakeCenter.y * length);

        var job = new LakeJob
        {
            width = width,
            length = length,
            center = lakeCenter,
            lakeRadius = settings.lakeRadius,
            waterLevel = settings.lakeWaterLevel,
            heights = heights
        };

        return job.Schedule(width * length, 64, dependency);
    }
}

