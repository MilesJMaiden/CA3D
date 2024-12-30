using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class MidpointDisplacementModifier : IHeightModifier
{
    public JobHandle ScheduleJob(NativeArray<float> heights, int width, int length, TerrainGenerationSettings settings, JobHandle dependency)
    {
        var job = new MidpointDisplacementJob
        {
            width = width,
            length = length,
            displacementFactor = settings.displacementFactor,
            displacementDecayRate = settings.displacementDecayRate,
            heights = heights
        };

        return job.Schedule(width * length, 64, dependency);
    }


    public void ModifyHeight(float[,] heights, TerrainGenerationSettings settings)
    {
        // Legacy implementation for compatibility
        Debug.LogWarning("ModifyHeight is deprecated when using ScheduleJob.");
    }
}
