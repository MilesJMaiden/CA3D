using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
public interface IFeatureModifier
{
    JobHandle ScheduleJob(NativeArray<float> heights, int width, int length, TerrainGenerationSettings settings, JobHandle dependency);
}
