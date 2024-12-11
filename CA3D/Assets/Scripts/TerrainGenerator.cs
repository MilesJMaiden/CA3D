using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : ITerrainGenerator
{
    private readonly TerrainGenerationSettings settings;
    private readonly List<IHeightModifier> heightModifiers;

    public TerrainGenerator(TerrainGenerationSettings settings)
    {
        this.settings = settings;

        heightModifiers = new List<IHeightModifier>();
        if (settings.usePerlinNoise) heightModifiers.Add(new PerlinNoiseModifier());
        if (settings.useFractalBrownianMotion) heightModifiers.Add(new FractalBrownianMotionModifier());
        if (settings.useMidPointDisplacement) heightModifiers.Add(new MidpointDisplacementModifier());
        if (settings.useVoronoiBiomes) heightModifiers.Add(new VoronoiBiomesModifier());
    }

    public float[,] GenerateHeights(int width, int length)
    {
        float[,] heights = new float[width, length];

        // Apply each modifier additively
        foreach (var modifier in heightModifiers)
        {
            float[,] modifierHeights = new float[width, length];

            // Initialize the modifier height array
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < length; y++)
                {
                    modifierHeights[x, y] = 0f;
                }
            }

            // Apply the modifier
            modifier.ModifyHeight(modifierHeights, settings);

            // Add the modifier heights to the final heightmap
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < length; y++)
                {
                    heights[x, y] += modifierHeights[x, y];
                }
            }
        }

        // Normalize heights to ensure they stay within the range [0, 1]
        NormalizeHeights(heights);
        return heights;
    }

    private void NormalizeHeights(float[,] heights)
    {
        float maxHeight = float.MinValue;
        float minHeight = float.MaxValue;

        // Find the min and max height values
        foreach (var height in heights)
        {
            if (height > maxHeight) maxHeight = height;
            if (height < minHeight) minHeight = height;
        }

        // Normalize the heights
        for (int x = 0; x < heights.GetLength(0); x++)
        {
            for (int y = 0; y < heights.GetLength(1); y++)
            {
                heights[x, y] = Mathf.InverseLerp(minHeight, maxHeight, heights[x, y]);
            }
        }
    }
}
