using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class PerlinNoiseModifier : IHeightModifier
{
    public JobHandle ScheduleJob(NativeArray<float> heights, int width, int length, TerrainGenerationSettings settings, JobHandle dependency)
    {
        var job = new PerlinNoiseJob
        {
            width = width,
            length = length,
            baseScale = settings.perlinBaseScale,
            amplitudeDecay = settings.perlinAmplitudeDecay,
            frequencyGrowth = settings.perlinFrequencyGrowth,
            offset = new float2(settings.perlinOffset.x, settings.perlinOffset.y),
            layers = settings.perlinLayers,
            heights = heights
        };

        return job.Schedule(width * length, 64, dependency);
    }


    public void ModifyHeight(float[,] heights, TerrainGenerationSettings settings)
    {
        // This method is redundant when using ScheduleJob but kept for compatibility.
        Debug.LogWarning("ModifyHeight is not used when ScheduleJob is implemented.");
    }
}