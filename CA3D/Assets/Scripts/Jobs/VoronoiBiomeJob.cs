using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct VoronoiBiomeJob : IJobParallelFor
{
    [ReadOnly] public int width;
    [ReadOnly] public int length;
    [ReadOnly] public NativeArray<float2> voronoiPoints;
    [ReadOnly] public NativeArray<float3x3> biomeThresholds;
    [ReadOnly] public float maxDistance;

    [WriteOnly] public NativeArray<int> biomeIndices;
    [WriteOnly] public NativeArray<int> terrainLayerIndices;

    public NativeArray<float> heights;

    public void Execute(int index)
    {
        int x = index % width;
        int y = index / width;
        float2 currentPoint = new float2(x, y);

        float minDistSquared = float.MaxValue;
        int nearestBiome = 0;

        for (int i = 0; i < voronoiPoints.Length; i++)
        {
            float distSquared = math.distancesq(currentPoint, voronoiPoints[i]);
            if (distSquared < minDistSquared)
            {
                minDistSquared = distSquared;
                nearestBiome = i;
            }
        }

        biomeIndices[index] = nearestBiome;

        // Determine terrain layer indices within the biome
        float height = heights[index];
        var thresholds = biomeThresholds[nearestBiome];

        if (height >= thresholds.c0.x && height <= thresholds.c0.y)
            terrainLayerIndices[index] = 0; // First layer
        else if (height >= thresholds.c1.x && height <= thresholds.c1.y)
            terrainLayerIndices[index] = 1; // Second layer
        else if (height >= thresholds.c2.x && height <= thresholds.c2.y)
            terrainLayerIndices[index] = 2; // Third layer
        else
            terrainLayerIndices[index] = -1; // No layer applies
    }

}
