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

    [WriteOnly] public NativeArray<int> biomeIndices;
    [WriteOnly] public NativeArray<int> terrainLayerIndices;

    public NativeArray<float> heights;

    public void Execute(int index)
    {
        int x = index % width;
        int y = index / width;
        float2 currentPoint = new float2(x, y);

        // Find the nearest and second-nearest Voronoi points
        int nearestBiome, secondNearestBiome;
        float nearestWeight, secondNearestWeight;
        FindNearestBiomes(currentPoint, out nearestBiome, out secondNearestBiome, out nearestWeight, out secondNearestWeight);

        // Assign biome index
        biomeIndices[index] = nearestBiome;

        // Blend terrain layers based on biome weights
        float height = heights[index];
        terrainLayerIndices[index] = DetermineBlendedLayer(
            height,
            biomeThresholds[nearestBiome],
            biomeThresholds[secondNearestBiome],
            nearestWeight,
            secondNearestWeight
        );
    }

    private void FindNearestBiomes(
        float2 currentPoint,
        out int nearestBiome,
        out int secondNearestBiome,
        out float nearestWeight,
        out float secondNearestWeight)
    {
        float nearestDistSquared = float.MaxValue;
        float secondNearestDistSquared = float.MaxValue;
        nearestBiome = -1;
        secondNearestBiome = -1;

        for (int i = 0; i < voronoiPoints.Length; i++)
        {
            float distSquared = math.distancesq(currentPoint, voronoiPoints[i]);

            if (distSquared < nearestDistSquared)
            {
                secondNearestDistSquared = nearestDistSquared;
                secondNearestBiome = nearestBiome;

                nearestDistSquared = distSquared;
                nearestBiome = i;
            }
            else if (distSquared < secondNearestDistSquared)
            {
                secondNearestDistSquared = distSquared;
                secondNearestBiome = i;
            }
        }

        float totalDistSquared = nearestDistSquared + secondNearestDistSquared;
        nearestWeight = totalDistSquared > 0 ? 1.0f - (nearestDistSquared / totalDistSquared) : 0.5f;
        secondNearestWeight = totalDistSquared > 0 ? 1.0f - nearestWeight : 0.5f;
    }

    private int DetermineBlendedLayer(
        float height,
        float3x3 nearestThresholds,
        float3x3 secondNearestThresholds,
        float nearestWeight,
        float secondNearestWeight)
    {
        // Use float variables instead of an array to track weights
        float weightLayer0 = 0f;
        float weightLayer1 = 0f;
        float weightLayer2 = 0f;

        // Accumulate weights for nearest biome
        AccumulateLayerWeights(ref weightLayer0, ref weightLayer1, ref weightLayer2, height, nearestThresholds, nearestWeight);

        // Accumulate weights for second-nearest biome
        AccumulateLayerWeights(ref weightLayer0, ref weightLayer1, ref weightLayer2, height, secondNearestThresholds, secondNearestWeight);

        // Determine dominant layer
        return FindDominantLayer(weightLayer0, weightLayer1, weightLayer2);
    }

    private void AccumulateLayerWeights(
        ref float weightLayer0,
        ref float weightLayer1,
        ref float weightLayer2,
        float height,
        float3x3 thresholds,
        float biomeWeight)
    {
        if (height >= thresholds.c0.x && height <= thresholds.c0.y)
            weightLayer0 += biomeWeight;
        if (height >= thresholds.c1.x && height <= thresholds.c1.y)
            weightLayer1 += biomeWeight;
        if (height >= thresholds.c2.x && height <= thresholds.c2.y)
            weightLayer2 += biomeWeight;
    }

    private int FindDominantLayer(float weightLayer0, float weightLayer1, float weightLayer2)
    {
        if (weightLayer0 >= weightLayer1 && weightLayer0 >= weightLayer2)
            return 0;
        if (weightLayer1 >= weightLayer0 && weightLayer1 >= weightLayer2)
            return 1;
        return 2;
    }
}
