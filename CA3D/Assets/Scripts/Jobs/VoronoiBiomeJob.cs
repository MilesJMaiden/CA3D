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
    [ReadOnly] public NativeArray<float> falloffSamples;
    [ReadOnly] public int sampleCount;

    public NativeArray<float> heights;
    public NativeArray<int> biomeIndices; // Output biome indices for material layers

    public void Execute(int index)
    {
        int x = index % width;
        int y = index / width;

        float2 currentPoint = new float2(x, y);

        // Find closest Voronoi point
        float minDistSquared = float.MaxValue;
        int closestPointIndex = 0;
        for (int i = 0; i < points.Length; i++)
        {
            float distSquared = math.distancesq(currentPoint, points[i]);
            if (distSquared < minDistSquared)
            {
                minDistSquared = distSquared;
                closestPointIndex = i;
            }
        }

        float normalizedDistance = math.sqrt(minDistSquared) / maxDistance;

        // Sample the falloff curve
        int sampleIndex = (int)(normalizedDistance * (sampleCount - 1));
        sampleIndex = math.clamp(sampleIndex, 0, sampleCount - 1);
        float falloffValue = falloffSamples[sampleIndex];

        // Calculate height and assign biome index
        heights[index] += math.lerp(heightRange.x, heightRange.y, falloffValue);
        biomeIndices[index] = closestPointIndex;
    }
}
