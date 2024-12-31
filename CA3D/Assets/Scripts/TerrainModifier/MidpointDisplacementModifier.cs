using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;

public class MidpointDisplacementModifier : IHeightModifier
{
    public JobHandle ScheduleJob(NativeArray<float> heights, int width, int length, TerrainGenerationSettings settings, JobHandle dependency)
    {
        NativeArray<float> outputHeights = new NativeArray<float>(heights.Length, Allocator.TempJob);

        var job = new MidpointDisplacementJob
        {
            width = width,
            length = length,
            roughness = settings.roughness,
            seed = settings.seed,
            inputHeights = heights,
            outputHeights = outputHeights,
            displacementDecayRate = settings.displacementDecayRate
        };

        // Schedule the job
        JobHandle handle = job.Schedule(width * length, 64, dependency);

        // Copy the output heights back to the input heights after the job completes
        handle.Complete();
        NativeArray<float>.Copy(outputHeights, heights);

        // Dispose of the output buffer
        outputHeights.Dispose();

        return handle;
    }

    public void ModifyHeight(float[,] heights, TerrainGenerationSettings settings)
    {
        Debug.LogWarning("ModifyHeight is deprecated when using ScheduleJob.");
    }
}
