using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct FeaturePlacementJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> heightMap;
    [WriteOnly] public NativeArray<int> placementMap;
    [ReadOnly] public NativeArray<int> biomeIndices;
    [ReadOnly] public int2 terrainSize;

    [ReadOnly] public Vector2 heightRange;
    [ReadOnly] public Vector2 slopeRange;
    [ReadOnly] public float spawnProbability;
    [ReadOnly] public int biomeIndex; // -1 => no biome restriction

    public Unity.Mathematics.Random random;

    public void Execute(int index)
    {
        int x = index % terrainSize.x;
        int y = index / terrainSize.x;

        float currentHeight = heightMap[index];
        float slope = CalculateSlope(index);

        bool biomeMatches = (biomeIndex == -1 || biomeIndices[index] == biomeIndex);

        if (biomeMatches &&
            currentHeight >= heightRange.x && currentHeight <= heightRange.y &&
            slope >= slopeRange.x && slope <= slopeRange.y &&
            random.NextFloat(0f, 1f) <= spawnProbability)
        {
            placementMap[index] = 1;
        }
    }

    private float CalculateSlope(int index)
    {
        int x = index % terrainSize.x;
        int y = index / terrainSize.x;

        int left = math.max(0, x - 1);
        int right = math.min(terrainSize.x - 1, x + 1);
        int top = math.max(0, y - 1);
        int bottom = math.min(terrainSize.y - 1, y + 1);

        float dx = heightMap[right + y * terrainSize.x] - heightMap[left + y * terrainSize.x];
        float dy = heightMap[x + bottom * terrainSize.x] - heightMap[x + top * terrainSize.x];

        // Convert slope to a steeper scale for simpler threshold checks
        return math.sqrt(dx * dx + dy * dy) * 100f;
    }
}
