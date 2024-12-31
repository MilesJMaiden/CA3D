using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct FeaturePlacementJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> heightMap;
    [ReadOnly] public NativeArray<float> featureData;
    [WriteOnly] public NativeArray<int> layerMap;
    [ReadOnly] public int2 terrainSize;
    public Unity.Mathematics.Random random;

    public void Execute(int index)
    {
        int x = index % terrainSize.x;
        int z = index / terrainSize.x;

        float height = heightMap[index];
        int selectedFeature = -1;

        for (int i = 0; i < featureData.Length / 3; i++)
        {
            float minHeight = featureData[i * 3];
            float maxHeight = featureData[i * 3 + 1];
            float probability = featureData[i * 3 + 2];

            if (height >= minHeight && height <= maxHeight)
            {
                if (random.NextFloat() <= probability)
                {
                    selectedFeature = i;
                    break;
                }
            }
        }

        layerMap[index] = selectedFeature;
    }
}
