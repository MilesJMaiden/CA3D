using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct VoronoiBiomeJob : IJobParallelFor
{
    [ReadOnly] public int width;
    [ReadOnly] public int length;
    [ReadOnly] public NativeArray<float2> points;
    [ReadOnly] public float maxDistance;
    [ReadOnly] public float2 heightRange;
    [ReadOnly] public NativeArray<float> falloffSamples; // Sampled falloff curve values
    [ReadOnly] public int sampleCount; // Number of samples in the falloff curve
    [ReadOnly] public float blendFactor; // Blending factor to control Voronoi influence

    public NativeArray<float> heights;

    public void Execute(int index)
    {
        int x = index % width;
        int y = index / width;

        float2 currentPoint = new float2(x, y);

        // Find the nearest Voronoi point
        float minDistSquared = float.MaxValue;
        foreach (float2 point in points)
        {
            float distSquared = math.distancesq(currentPoint, point);
            minDistSquared = math.min(minDistSquared, distSquared);
        }

        // Normalize the distance
        float normalizedDistance = math.sqrt(minDistSquared) / maxDistance;

        // Sample the falloff curve
        int sampleIndex = (int)(normalizedDistance * (sampleCount - 1));
        sampleIndex = math.clamp(sampleIndex, 0, sampleCount - 1);
        float falloffValue = falloffSamples[sampleIndex];

        // Calculate Voronoi height contribution
        float voronoiHeight = math.lerp(heightRange.x, heightRange.y, falloffValue);

        // Blend Voronoi height with existing height
        heights[index] = math.lerp(heights[index], voronoiHeight, blendFactor);
    }
}
