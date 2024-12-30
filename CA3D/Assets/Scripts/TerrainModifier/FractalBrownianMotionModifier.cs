using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class FractalBrownianMotionModifier : IHeightModifier
{
    public JobHandle ScheduleJob(NativeArray<float> heights, int width, int length, TerrainGenerationSettings settings, JobHandle dependency)
    {
        var job = new FractalBrownianMotionJob
        {
            width = width,
            length = length,
            baseScale = settings.fBmBaseScale,
            amplitudeDecay = settings.fBmAmplitudeDecay,
            frequencyGrowth = settings.fBmFrequencyGrowth,
            offset = new float2(settings.fBmOffset.x, settings.fBmOffset.y),
            layers = settings.fBmLayers,
            heights = heights
        };

        return job.Schedule(width * length, 64, dependency);
    }


    public void ModifyHeight(float[,] heights, TerrainGenerationSettings settings)
    {
        // Legacy implementation, replaced by ScheduleJob
        Debug.LogWarning("ModifyHeight is deprecated when using ScheduleJob.");
    }
}
