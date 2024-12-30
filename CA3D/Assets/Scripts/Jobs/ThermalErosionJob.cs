using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;

[BurstCompile]
public struct ThermalErosionJob : IJobParallelFor
{
    [ReadOnly] public int width;
    [ReadOnly] public int length;
    [ReadOnly] public float talusAngle; // Threshold slope angle for erosion
    [ReadOnly] public int iterations;  // Number of erosion steps

    public NativeArray<float> heights;

    public void Execute(int index)
    {
        for (int i = 0; i < iterations; i++)
        {
            int x = index % width;
            int y = index / width;

            float currentHeight = heights[index];
            float avgNeighborHeight = 0f;
            int validNeighbors = 0;

            // Iterate through neighbors
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue; // Skip the current cell

                    int nx = x + dx;
                    int ny = y + dy;

                    if (nx >= 0 && nx < width && ny >= 0 && ny < length)
                    {
                        int neighborIndex = nx + ny * width;
                        avgNeighborHeight += heights[neighborIndex];
                        validNeighbors++;
                    }
                }
            }

            if (validNeighbors > 0)
            {
                avgNeighborHeight /= validNeighbors;

                // Erode if the slope exceeds the talus angle
                float slope = currentHeight - avgNeighborHeight;
                if (slope > talusAngle)
                {
                    float erosionAmount = slope * 0.5f; // Redistribute half the slope difference
                    heights[index] -= erosionAmount;
                    // Spread to neighbors (simplified approach)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = x + dx;
                            int ny = y + dy;

                            if (nx >= 0 && nx < width && ny >= 0 && ny < length)
                            {
                                int neighborIndex = nx + ny * width;
                                heights[neighborIndex] += erosionAmount / validNeighbors;
                            }
                        }
                    }
                }
            }
        }
    }
}
