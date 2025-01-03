using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct ThermalErosionJob : IJobParallelFor
{
    public int width;
    public int length;
    public float talusAngle;

    [ReadOnly]
    public NativeArray<float> inputBuffer; // Heights to read from

    [WriteOnly]
    public NativeArray<float> outputBuffer; // Heights to write to

    public void Execute(int index)
    {
        int x = index % width;
        int y = index / width;

        // Boundary check: retain original height for edge cells
        if (x == 0 || x == width - 1 || y == 0 || y == length - 1)
        {
            outputBuffer[index] = inputBuffer[index];
            return;
        }

        float currentHeight = inputBuffer[index];
        float totalHeightChange = 0f;

        // Iterate over neighbors to simulate erosion
        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                if (offsetX == 0 && offsetY == 0) continue;

                int neighborX = x + offsetX;
                int neighborY = y + offsetY;
                int neighborIndex = neighborY * width + neighborX;

                // Validate neighbor index
                if (neighborIndex < 0 || neighborIndex >= inputBuffer.Length) continue;

                float neighborHeight = inputBuffer[neighborIndex];
                float heightDiff = currentHeight - neighborHeight;

                if (heightDiff > talusAngle)
                {
                    float heightChange = heightDiff / 2f; // Distribute height equally
                    totalHeightChange -= heightChange;

                    // Update neighbor height in output buffer
                    outputBuffer[neighborIndex] += heightChange;
                }
            }
        }

        // Apply total height change to the current cell
        outputBuffer[index] = currentHeight + totalHeightChange;
    }
}
