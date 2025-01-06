using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct LakeJob : IJobParallelFor
{
    [ReadOnly] public int width;
    [ReadOnly] public int length;
    [ReadOnly] public float2 center;
    [ReadOnly] public float lakeRadius;
    [ReadOnly] public float waterLevel;

    public NativeArray<float> heights;

    public void Execute(int index)
    {
        int x = index % width;
        int y = index / width;

        float2 pt = new float2(x, y);
        float dist = math.distance(pt, center);
        if (dist <= lakeRadius)
        {
            // Carve down if it's above water level
            float oldVal = heights[index];
            heights[index] = math.min(oldVal, waterLevel);
        }
    }
}
