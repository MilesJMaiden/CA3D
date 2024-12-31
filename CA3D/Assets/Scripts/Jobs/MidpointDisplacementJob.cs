using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct MidpointDisplacementJob : IJobParallelFor
{
    [ReadOnly] public int width;
    [ReadOnly] public int length;
    [ReadOnly] public float roughness;
    [ReadOnly] public float seed;
    [ReadOnly] public float displacementDecayRate;

    [ReadOnly] public NativeArray<float> inputHeights; // Read-only input buffer
    public NativeArray<float> outputHeights; // Output buffer

    public void Execute(int index)
    {
        // Convert 1D index to 2D coordinates
        int x = index % width;
        int y = index / width;

        // Initialize corners for the grid
        float topLeft = GetHeight(inputHeights, x - 1, y - 1);
        float topRight = GetHeight(inputHeights, x + 1, y - 1);
        float bottomLeft = GetHeight(inputHeights, x - 1, y + 1);
        float bottomRight = GetHeight(inputHeights, x + 1, y + 1);

        // Compute midpoint value
        float average = (topLeft + topRight + bottomLeft + bottomRight) * 0.25f;

        // Add displacement
        float displacement = GenerateDisplacement(x, y);
        outputHeights[index] = average + displacement;
    }

    private float GetHeight(NativeArray<float> heights, int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= length)
            return 0f; // Boundary condition
        return heights[x + y * width];
    }

    private float GenerateDisplacement(int x, int y)
    {
        float random = math.sin(seed + x * 73129 + y * 95121) * 43758.5453f; // Pseudo-random generator
        random = random - math.floor(random);

        // Calculate the distance from the center
        float distanceFromCenter = math.distance(new float2(x, y), new float2(width / 2f, length / 2f));

        return random * roughness * math.exp(-displacementDecayRate * distanceFromCenter);
    }
}
