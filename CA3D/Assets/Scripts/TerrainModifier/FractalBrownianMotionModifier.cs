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
    /// <param name="heights">The 2D array of terrain heights to modify.</param>
    /// <param name="settings">The terrain generation settings used for fBm generation.</param>
    public void ModifyHeight(float[,] heights, TerrainGenerationSettings settings)
    {
        int width = heights.GetLength(0);
        int length = heights.GetLength(1);

        // Cache reusable values from settings
        float baseScale = settings.fBmBaseScale;
        float amplitudeDecay = settings.fBmAmplitudeDecay;
        float frequencyGrowth = settings.fBmFrequencyGrowth;
        Vector2 offset = settings.fBmOffset;

        // Precompute normalization factors for coordinates
        float xNormalizer = 1f / width;
        float yNormalizer = 1f / length;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                float heightValue = GenerateFractalNoise(x, y, xNormalizer, yNormalizer, baseScale, amplitudeDecay, frequencyGrowth, offset, settings.fBmLayers);
                heights[x, y] += heightValue;
            }
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Generates the height value for a single point using Fractal Brownian Motion (fBm).
    /// </summary>
    /// <param name="x">The x-coordinate of the point in the heightmap.</param>
    /// <param name="y">The y-coordinate of the point in the heightmap.</param>
    /// <param name="xNormalizer">Normalization factor for the x-coordinate.</param>
    /// <param name="yNormalizer">Normalization factor for the y-coordinate.</param>
    /// <param name="baseScale">The base scale of the noise.</param>
    /// <param name="amplitudeDecay">The amplitude decay factor per layer.</param>
    /// <param name="frequencyGrowth">The frequency growth factor per layer.</param>
    /// <param name="offset">The offset for noise generation.</param>
    /// <param name="layers">The number of fBm layers to apply.</param>
    /// <returns>The generated height value for the point.</returns>
    private float GenerateFractalNoise(int x, int y, float xNormalizer, float yNormalizer, float baseScale, float amplitudeDecay, float frequencyGrowth, Vector2 offset, int layers)
    {
        float heightValue = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        for (int layer = 0; layer < layers; layer++)
        {
            float xCoord = (x * xNormalizer) * baseScale * frequency + offset.x;
            float yCoord = (y * yNormalizer) * baseScale * frequency + offset.y;

            heightValue += Mathf.PerlinNoise(xCoord, yCoord) * amplitude;
            amplitude *= amplitudeDecay;
            frequency *= frequencyGrowth;
        }

        return heightValue;
    }

    #endregion
}
