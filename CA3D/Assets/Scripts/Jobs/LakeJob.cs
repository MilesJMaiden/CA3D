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

        float2 currentPoint = new float2(x, y);
        float distanceToCenter = math.distance(currentPoint, center);

        if (distanceToCenter < lakeRadius)
        {
            heights[index] = math.min(heights[index], waterLevel); // Flatten below water level
        }
    }
}
