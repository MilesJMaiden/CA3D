using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : ITerrainGenerator
{
    #region Fields

    private readonly TerrainGenerationSettings settings;
    private readonly List<IHeightModifier> heightModifiers;

    #endregion

    #region Constructor

    public TerrainGenerator(TerrainGenerationSettings settings, List<IHeightModifier> heightModifiers = null)
    {
        this.settings = settings;
        this.heightModifiers = heightModifiers ?? HeightModifierFactory.CreateModifiers(settings);

        Debug.Log($"TerrainGenerator initialized with {this.heightModifiers.Count} modifiers");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Generates terrain heights based on the provided width and length.
    /// </summary>
    /// <param name="width">The width of the terrain.</param>
    /// <param name="length">The length of the terrain.</param>
    /// <returns>A 2D array of normalized height values.</returns>
    public float[,] GenerateHeights(int width, int length)
    {
        float[,] heights = new float[width, length];

        // Apply base height modifiers
        foreach (var modifier in heightModifiers)
        {
            modifier.ModifyHeight(heights, settings);
        }

        // Apply feature-specific modifiers
        ApplyFeatureModifiers(heights, width, length);

        NormalizeHeights(heights);
        return heights;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Applies feature-specific modifiers like lakes, rivers, and trails.
    /// </summary>
    /// <param name="heights">The terrain height array to modify.</param>
    /// <param name="width">The width of the terrain.</param>
    /// <param name="length">The length of the terrain.</param>
    private void ApplyFeatureModifiers(float[,] heights, int width, int length)
    {
        // Apply trail modifier
        if (settings.useTrails)
        {
            Debug.Log("Applying Trail Modifier...");
            var trailModifier = new TrailModifier();
            trailModifier.ApplyFeature(heights, settings, new Vector2(0.2f, 0.3f), settings.trailIntensity, settings.trailWidth);
        }

        // Additional feature modifiers (e.g., waterfalls, lava) can be added here in a similar manner.
    }

    /// <summary>
    /// Normalizes the height values to ensure they are within the range [0, 1].
    /// </summary>
    /// <param name="heights">The height array to normalize.</param>
    private void NormalizeHeights(float[,] heights)
    {
        float maxHeight = float.MinValue;
        float minHeight = float.MaxValue;

        // Find min, max, and normalize in one pass
        for (int x = 0; x < heights.GetLength(0); x++)
        {
            for (int y = 0; y < heights.GetLength(1); y++)
            {
                float height = heights[x, y];
                if (height > maxHeight) maxHeight = height;
                if (height < minHeight) minHeight = height;
            }
        }

        float range = maxHeight - minHeight;
        if (range > Mathf.Epsilon) // Avoid division by zero
        {
            for (int x = 0; x < heights.GetLength(0); x++)
            {
                for (int y = 0; y < heights.GetLength(1); y++)
                {
                    heights[x, y] = (heights[x, y] - minHeight) / range;
                }
            }
        }
    }

    #endregion
}
