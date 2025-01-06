// VoronoiBiomeJob.cs
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

    [WriteOnly] public NativeArray<int> biomeIndices;       // which biome each pixel belongs to
    [WriteOnly] public NativeArray<int> terrainLayerIndices; // optional single-layer selection

    // The existing terrain heights that can be modified if needed
    public NativeArray<float> heights;

    public void Execute(int index)
    {
        int x = index % width;
        int y = index / width;
        float2 current = new float2(x, y);

        int nearestBiome, secondNearestBiome;
        float nearestWeight, secondNearestWeight;
        FindNearestBiomes(current, out nearestBiome, out secondNearestBiome, out nearestWeight, out secondNearestWeight);

        // Safeguards
        if (nearestBiome < 0) nearestBiome = 0;
        if (secondNearestBiome < 0) secondNearestBiome = nearestBiome;

        // Assign the final biome index
        biomeIndices[index] = nearestBiome;

        // Single-layer logic:
        float h = heights[index];
        int layer = DetermineBlendedLayer(
            h,
            biomeThresholds[nearestBiome],
            biomeThresholds[secondNearestBiome],
            nearestWeight,
            secondNearestWeight
        );
        terrainLayerIndices[index] = layer;
    }

    private void FindNearestBiomes(
        float2 current,
        out int nearestBiome,
        out int secondNearestBiome,
        out float nearestWeight,
        out float secondNearestWeight)
    {
        nearestBiome = -1;
        secondNearestBiome = -1;
        float bestDistSq = float.MaxValue;
        float secondDistSq = float.MaxValue;

        if (voronoiPoints.Length == 0)
        {
            // fallback: everything is biome 0
            nearestBiome = 0;
            secondNearestBiome = 0;
            nearestWeight = 1f;
            secondNearestWeight = 0f;
            return;
        }

        for (int i = 0; i < voronoiPoints.Length; i++)
        {
            float distSq = math.distancesq(current, voronoiPoints[i]);

            if (distSq < bestDistSq)
            {
                secondDistSq = bestDistSq;
                secondNearestBiome = nearestBiome;

                bestDistSq = distSq;
                nearestBiome = i;
            }
            else if (distSq < secondDistSq)
            {
                secondDistSq = distSq;
                secondNearestBiome = i;
            }
        }

        // If we never found a second, set them equal
        if (secondNearestBiome < 0) secondNearestBiome = nearestBiome;

        float sumDist = bestDistSq + secondDistSq;
        if (sumDist > 0f)
        {
            nearestWeight = 1f - (bestDistSq / sumDist);
            secondNearestWeight = 1f - nearestWeight;
        }
        else
        {
            nearestWeight = 0.5f;
            secondNearestWeight = 0.5f;
        }
    }

    private int DetermineBlendedLayer(
        float heightVal,
        float3x3 nearestThresh,
        float3x3 secondThresh,
        float wNearest,
        float wSecond)
    {
        float wLayer0 = 0f;
        float wLayer1 = 0f;
        float wLayer2 = 0f;

        AccumulateLayerWeights(ref wLayer0, ref wLayer1, ref wLayer2, heightVal, nearestThresh, wNearest);
        AccumulateLayerWeights(ref wLayer0, ref wLayer1, ref wLayer2, heightVal, secondThresh, wSecond);

        return FindDominantLayer(wLayer0, wLayer1, wLayer2);
    }

    private void AccumulateLayerWeights(
        ref float w0,
        ref float w1,
        ref float w2,
        float h,
        float3x3 thresh,
        float biomeWeight)
    {
        // thresh.c0 => (minHeight1, maxHeight1, unused)
        // thresh.c1 => ...
        // thresh.c2 => ...
        if (h >= thresh.c0.x && h <= thresh.c0.y) w0 += biomeWeight;
        if (h >= thresh.c1.x && h <= thresh.c1.y) w1 += biomeWeight;
        if (h >= thresh.c2.x && h <= thresh.c2.y) w2 += biomeWeight;
    }

    private int FindDominantLayer(float w0, float w1, float w2)
    {
        if (w0 >= w1 && w0 >= w2) return 0;
        if (w1 >= w0 && w1 >= w2) return 1;
        return 2;
    }
}
