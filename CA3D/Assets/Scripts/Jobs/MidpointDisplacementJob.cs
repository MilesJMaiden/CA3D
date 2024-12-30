using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct MidpointDisplacementJob : IJobParallelFor
{
    [ReadOnly] public int width;
    [ReadOnly] public int length;
    [ReadOnly] public float displacementFactor;
    [ReadOnly] public float displacementDecayRate;

    public NativeArray<float> heights;

    public void Execute(int index)
    {
        int x = index % width;
        int y = index / width;

        float randomDisplacement = math.clamp(math.sin(x * y * displacementFactor) * displacementFactor, 0f, 1f);
        heights[index] += randomDisplacement;
    }
}
