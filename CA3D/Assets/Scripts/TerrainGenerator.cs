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
        // Add additional modifiers here
    }

    public float[,] GenerateHeights(int width, int length)
    {
        float[,] heights = new float[width, length];

        foreach (var modifier in heightModifiers)
        {
            modifier.ModifyHeight(heights, settings);
        }

        NormalizeHeights(heights);
        return heights;
    }

    private void NormalizeHeights(float[,] heights)
    {
        float maxHeight = float.MinValue;
        float minHeight = float.MaxValue;

        foreach (var height in heights)
        {
            if (height > maxHeight) maxHeight = height;
            if (height < minHeight) minHeight = height;
        }

        for (int x = 0; x < heights.GetLength(0); x++)
        {
            for (int y = 0; y < heights.GetLength(1); y++)
            {
                heights[x, y] = Mathf.InverseLerp(minHeight, maxHeight, heights[x, y]);
            }
        }
    }
}
