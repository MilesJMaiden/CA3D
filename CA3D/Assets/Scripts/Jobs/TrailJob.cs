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

        float2 current = new float2(x, y);
        float totalDist = math.distance(startPoint, endPoint);

        // Param along the line
        float distAlongLine = math.distance(startPoint, current);
        float t = (totalDist > 0f) ? math.clamp(distAlongLine / totalDist, 0f, 1f) : 0f;

        // Compute a center point along line, add some randomness
        float2 lineCenter = math.lerp(startPoint, endPoint, t);
        lineCenter.x += math.sin(lineCenter.y * randomness) * (trailWidth * 0.25f);

        float distToLine = math.distance(current, lineCenter);
        if (distToLine < trailWidth)
        {
            // Carve based on distance to center
            float carveDepth = math.smoothstep(trailWidth, 0f, distToLine) * 0.1f;
            float old = heights[index];
            heights[index] = math.max(0, old - carveDepth);
        }
    }
}
