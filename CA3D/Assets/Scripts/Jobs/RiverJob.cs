using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct RiverJob : IJobParallelFor
{
    [ReadOnly] public int width;
    [ReadOnly] public int length;
    [ReadOnly] public float startHeight; // Height to begin the river
    [ReadOnly] public float riverWidth;

    public NativeArray<float> heights;

    public void Execute(int index)
    {
        int x = index % width;
        int y = index / width;

        // Create a river-like path by carving a sine wave
        float normalizedY = y / (float)length;
        float riverCenterX = width / 2 + math.sin(normalizedY * math.PI * 4) * width / 4;

        float distanceFromRiver = math.abs(x - riverCenterX);
        if (distanceFromRiver < riverWidth)
        {
            float carveDepth = math.smoothstep(riverWidth, 0, distanceFromRiver) * startHeight;
            heights[index] -= carveDepth; // Lower the terrain to create a river
        }
    }
}
