using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct TrailJob : IJobParallelFor
{
    [ReadOnly] public int width;
    [ReadOnly] public int length;
    [ReadOnly] public float2 startPoint;
    [ReadOnly] public float2 endPoint;
    [ReadOnly] public float trailWidth;
    [ReadOnly] public float randomness;

    public NativeArray<float> heights;

    public void Execute(int index)
    {
        int x = index % width;
        int y = index / width;

        float2 currentPoint = new float2(x, y);
        float2 trailCenter = math.lerp(startPoint, endPoint, math.smoothstep(0f, 1f, y / (float)length));

        // Apply randomness
        trailCenter.x += math.sin(trailCenter.y * randomness) * trailWidth / 4;

        float distanceToTrail = math.distance(currentPoint, trailCenter);
        if (distanceToTrail < trailWidth)
        {
            float carveDepth = math.smoothstep(trailWidth, 0, distanceToTrail);
            heights[index] -= carveDepth * 0.1f; // Carve a small trail
        }
    }
}
