using UnityEngine;

/// <summary>
/// A height modifier that applies Fractal Brownian Motion (fBm) to a terrain heightmap.
/// </summary>
public class FractalBrownianMotionModifier : IHeightModifier
{
    #region Public Methods

    /// <summary>
    /// Modifies the terrain heights using Fractal Brownian Motion (fBm) based on the specified settings.
    /// </summary>
    public void ModifyHeight(float[,] heights, TerrainGenerationSettings settings)
    {
        int width = heights.GetLength(0);
        int length = heights.GetLength(1);

        // Cache reusable values from settings
        float baseScale = settings.fBmBaseScale;
        float amplitudeDecay = Mathf.Clamp(settings.fBmAmplitudeDecay, 0f, 1f);
        float frequencyGrowth = Mathf.Clamp(settings.fBmFrequencyGrowth, 0.1f, 10f);
        Vector2 offset = settings.fBmOffset;

        float xNormalizer = 1f / width;
        float yNormalizer = 1f / length;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                float heightValue = GenerateFractalNoise(x, y, xNormalizer, yNormalizer, baseScale, amplitudeDecay, frequencyGrowth, offset, settings.fBmLayers);
                heights[x, y] += Mathf.Clamp(heightValue, 0f, 1f); // Clamp to ensure valid height range
            }
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Generates the height value for a single point using Fractal Brownian Motion (fBm).
    /// </summary>
    private float GenerateFractalNoise(int x, int y, float xNormalizer, float yNormalizer, float baseScale, float amplitudeDecay, float frequencyGrowth, Vector2 offset, int layers)
    {
        float heightValue = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        for (int layer = 0; layer < layers; layer++)
        {
            float xCoord = (x * xNormalizer) * baseScale * frequency + offset.x;
            float yCoord = (y * yNormalizer) * baseScale * frequency + offset.y;

            float noiseValue = Mathf.PerlinNoise(xCoord, yCoord);
            heightValue += noiseValue * amplitude;

            //Debug.Log($"Layer {layer}: xCoord={xCoord}, yCoord={yCoord}, noiseValue={noiseValue}, amplitude={amplitude}, frequency={frequency}");

            amplitude *= Mathf.Clamp(amplitudeDecay, 0f, 1f);
            frequency *= Mathf.Clamp(frequencyGrowth, 0.1f, 10f);
        }

        return heightValue;
    }

    #endregion
}
