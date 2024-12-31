using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct CellularAutomataFeaturePlacementJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> heightMap;
    [ReadOnly] public NativeArray<int> biomeIndices;
    [WriteOnly] public NativeArray<int> caGrid;

    [ReadOnly] public int2 terrainSize;
    public Unity.Mathematics.Random random;

    public void Execute(int index)
    {
        int x = index % terrainSize.x;
        int z = index / terrainSize.x;

        float height = heightMap[index];
        int biome = biomeIndices[index];

        // Example CA rules
        if (biome == 0 && height > 0.5f && random.NextFloat() < 0.2f)
        {
            caGrid[index] = 0; // Tree
        }
        else if (biome == 1 && height < 0.3f && random.NextFloat() < 0.1f)
        {
            caGrid[index] = 1; // Rock
        }
        else
        {
            caGrid[index] = -1; // Empty
        }
    }
}
