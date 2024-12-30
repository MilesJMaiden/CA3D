using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct ThermalErosionJob : IJobParallelFor
{
    public int width;
    public int length;
    public float talusAngle;
    public int iterations;

    [ReadOnly]
    public NativeArray<float> inputHeights;

    [WriteOnly]
    public NativeArray<float> outputHeights;

    public void Execute(int index)
    {
        int x = index % width;
        int y = index / width;

        // Copy input heights to output heights (double buffering)
        outputHeights[index] = inputHeights[index];

        // Skip boundary cells
        if (x == 0 || x == width - 1 || y == 0 || y == length - 1)
            return;

        for (int iter = 0; iter < iterations; iter++)
        {
            float currentHeight = inputHeights[index];

            // Process neighbors to simulate thermal erosion
            float totalHeightChange = 0f;
            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    if (offsetX == 0 && offsetY == 0) continue;

                    int neighborX = x + offsetX;
                    int neighborY = y + offsetY;
                    int neighborIndex = neighborY * width + neighborX;

                    if (neighborIndex < 0 || neighborIndex >= inputHeights.Length) continue;

                    float neighborHeight = inputHeights[neighborIndex];
                    float heightDiff = currentHeight - neighborHeight;

                    if (heightDiff > talusAngle)
                    {
                        float heightChange = heightDiff / 2f;
                        totalHeightChange -= heightChange;
                        outputHeights[neighborIndex] += heightChange;
                    }
                }
            }

            outputHeights[index] += totalHeightChange;
        }
    }
}
