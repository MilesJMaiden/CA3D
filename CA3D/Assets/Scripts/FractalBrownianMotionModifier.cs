using UnityEngine;

public class FractalBrownianMotionModifier : IHeightModifier
{
    public void ModifyHeight(float[,] heights, TerrainGenerationSettings settings)
    {
        for (int x = 0; x < heights.GetLength(0); x++)
        {
            for (int y = 0; y < heights.GetLength(1); y++)
            {
                float heightValue = 0f;
                float amplitude = 1f;
                float frequency = 1f;

                for (int layer = 0; layer < settings.fBmLayers; layer++)
                {
                    float xCoord = (x / (float)heights.GetLength(0)) * settings.fBmBaseScale * frequency + settings.fBmOffset.x;
                    float yCoord = (y / (float)heights.GetLength(1)) * settings.fBmBaseScale * frequency + settings.fBmOffset.y;

                    heightValue += Mathf.PerlinNoise(xCoord, yCoord) * amplitude;
                    amplitude *= settings.fBmAmplitudeDecay;
                    frequency *= settings.fBmFrequencyGrowth;
                }

                heights[x, y] += heightValue;
            }
        }
    }
}
