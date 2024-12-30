using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct FractalBrownianMotionJob : IJobParallelFor
{
    [ReadOnly] public int width;
    [ReadOnly] public int length;
    [ReadOnly] public float baseScale;
    [ReadOnly] public float amplitudeDecay;
    [ReadOnly] public float frequencyGrowth;
    [ReadOnly] public float2 offset;
    [ReadOnly] public int layers;

    public NativeArray<float> heights;

    public void Execute(int index)
    {
        int x = index % width;
        int y = index / width;

        float normalizedX = x / (float)width;
        float normalizedY = y / (float)length;

        float heightValue = GenerateFractalNoise(normalizedX, normalizedY);
        heights[index] = math.clamp(heights[index] + heightValue, 0f, 1f);
    }

    private float GenerateFractalNoise(float normalizedX, float normalizedY)
    {
        float heightValue = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        for (int i = 0; i < layers; i++)
        {
            float xCoord = (normalizedX * baseScale * frequency) + offset.x;
            float yCoord = (normalizedY * baseScale * frequency) + offset.y;

            heightValue += noise.cnoise(new float2(xCoord, yCoord)) * amplitude;
            amplitude *= amplitudeDecay;
            frequency *= frequencyGrowth;
        }

        return heightValue;
    }
}
