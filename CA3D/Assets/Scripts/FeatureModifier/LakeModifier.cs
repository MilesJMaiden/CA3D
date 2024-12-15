using UnityEngine;

public class LakeModifier : IFeatureModifier
{
    public void ApplyFeature(float[,] heights, TerrainGenerationSettings settings, Vector2 location, float intensity, float radius)
    {
        int width = heights.GetLength(0);
        int length = heights.GetLength(1);

        int centerX = Mathf.RoundToInt(location.x * width);
        int centerY = Mathf.RoundToInt(location.y * length);
        float maxRadius = radius * Mathf.Min(width, length);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                if (distance < maxRadius)
                {
                    // Falloff blending
                    float falloff = Mathf.Clamp01(1 - (distance / maxRadius));
                    float adjustment = intensity * falloff;

                    // Subtractive adjustment for the lake
                    heights[x, y] -= adjustment;
                }
            }
        }
    }
}
