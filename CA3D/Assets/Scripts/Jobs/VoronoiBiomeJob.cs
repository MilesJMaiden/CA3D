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

        int nearestBiome, secondNearestBiome;
        float nearestWeight, secondNearestWeight;
        FindNearestBiomes(currentPoint, out nearestBiome, out secondNearestBiome, out nearestWeight, out secondNearestWeight);

        if (nearestBiome < 0) nearestBiome = 0;
        if (secondNearestBiome < 0) secondNearestBiome = nearestBiome;

        biomeIndices[index] = nearestBiome;

        float heightVal = heights[index];
        terrainLayerIndices[index] = DetermineBlendedLayer(
            heightVal,
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
        nearestBiome = -1;
        secondNearestBiome = -1;
        float nearestDistSq = float.MaxValue;
        float secondNearestDistSq = float.MaxValue;

        if (voronoiPoints.Length == 0)
        {
            nearestBiome = 0;
            secondNearestBiome = 0;
            nearestWeight = 1f;
            secondNearestWeight = 0f;
            return;
        }

        for (int i = 0; i < voronoiPoints.Length; i++)
        {
            float distSq = math.distancesq(currentPoint, voronoiPoints[i]);

            if (distSq < nearestDistSq)
            {
                secondNearestDistSq = nearestDistSq;
                secondNearestBiome = nearestBiome;

                nearestDistSq = distSq;
                nearestBiome = i;
            }
            else if (distSq < secondNearestDistSq)
            {
                secondNearestDistSq = distSq;
                secondNearestBiome = i;
            }
        }

        if (secondNearestBiome < 0) secondNearestBiome = nearestBiome;

        float total = nearestDistSq + secondNearestDistSq;
        if (total > 0f)
        {
            nearestWeight = 1f - (nearestDistSq / total);
            secondNearestWeight = 1f - nearestWeight;
        }
        else
        {
            nearestWeight = 0.5f;
            secondNearestWeight = 0.5f;
        }
    }

    private int DetermineBlendedLayer(
        float height,
        float3x3 nearestThresholds,
        float3x3 secondNearestThresholds,
        float nearestWeight,
        float secondNearestWeight)
    {
        float weightLayer0 = 0f;
        float weightLayer1 = 0f;
        float weightLayer2 = 0f;

        AccumulateLayerWeights(ref weightLayer0, ref weightLayer1, ref weightLayer2, height, nearestThresholds, nearestWeight);
        AccumulateLayerWeights(ref weightLayer0, ref weightLayer1, ref weightLayer2, height, secondNearestThresholds, secondNearestWeight);

        return FindDominantLayer(weightLayer0, weightLayer1, weightLayer2);
    }

    private void AccumulateLayerWeights(
        ref float w0,
        ref float w1,
        ref float w2,
        float height,
        float3x3 thresholds,
        float biomeWeight)
    {
        if (height >= thresholds.c0.x && height <= thresholds.c0.y)
            w0 += biomeWeight;
        if (height >= thresholds.c1.x && height <= thresholds.c1.y)
            w1 += biomeWeight;
        if (height >= thresholds.c2.x && height <= thresholds.c2.y)
            w2 += biomeWeight;
    }

    private int FindDominantLayer(float w0, float w1, float w2)
    {
        if (w0 >= w1 && w0 >= w2) return 0;
        if (w1 >= w0 && w1 >= w2) return 1;
        return 2;
    }
}
