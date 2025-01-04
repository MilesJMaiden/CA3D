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

        // Find the nearest and second-nearest Voronoi points
        int nearestBiome, secondNearestBiome;
        float nearestWeight, secondNearestWeight;
        FindNearestBiomes(currentPoint, out nearestBiome, out secondNearestBiome, out nearestWeight, out secondNearestWeight);

        // Validate indices before assignment
        biomeIndices[index] = nearestBiome >= 0 ? nearestBiome : 0;

        // Blend terrain layers based on biome weights
        float height = heights[index];
        terrainLayerIndices[index] = DetermineBlendedLayer(
            height,
            nearestBiome >= 0 ? biomeThresholds[nearestBiome] : new float3x3(),
            secondNearestBiome >= 0 ? biomeThresholds[secondNearestBiome] : new float3x3(),
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
        float[] layerWeights = new float[3];
        AccumulateLayerWeights(layerWeights, height, nearestThresholds, nearestWeight);
        AccumulateLayerWeights(layerWeights, height, secondNearestThresholds, secondNearestWeight);

        return FindDominantLayer(layerWeights);
    }

    private void AccumulateLayerWeights(float[] layerWeights, float height, float3x3 thresholds, float biomeWeight)
    {
        if (height >= thresholds.c0.x && height <= thresholds.c0.y)
            layerWeights[0] += biomeWeight;
        if (height >= thresholds.c1.x && height <= thresholds.c1.y)
            layerWeights[1] += biomeWeight;
        if (height >= thresholds.c2.x && height <= thresholds.c2.y)
            layerWeights[2] += biomeWeight;
    }

    private int FindDominantLayer(float[] layerWeights)
    {
        int dominantLayer = 0;
        float maxWeight = float.MinValue;

        for (int i = 0; i < layerWeights.Length; i++)
        {
            if (layerWeights[i] > maxWeight)
            {
                maxWeight = layerWeights[i];
                dominantLayer = i;
            }
        }

        return dominantLayer;
    }
}
