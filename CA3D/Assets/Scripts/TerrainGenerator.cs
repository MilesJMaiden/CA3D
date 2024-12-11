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

        // Apply each modifier directly to the heights array
        foreach (var modifier in heightModifiers)
        {
            modifier.ModifyHeight(heights, settings);
        }

        NormalizeHeights(heights);
        return heights;
    }

    #endregion

    #region Private Methods

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