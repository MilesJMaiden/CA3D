using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class LakeModifier : IFeatureModifier
{
    public JobHandle ScheduleJob(NativeArray<float> heights, int width, int length, TerrainGenerationSettings settings, JobHandle dependency)
    {
        // Validate lake settings
        if (settings.lakeRadius <= 0f || settings.lakeWaterLevel < 0f)
        {
           // Debug.LogWarning("Invalid lake settings: Ensure radius > 0 and waterLevel >= 0.");
            return dependency; // Skip the lake application
        }

        float2 lakeCenter = new float2(settings.lakeCenter.x * width, settings.lakeCenter.y * length);
        //Debug.Log($"Scheduling LakeJob: Center={lakeCenter}, Radius={settings.lakeRadius}, WaterLevel={settings.lakeWaterLevel}");

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
