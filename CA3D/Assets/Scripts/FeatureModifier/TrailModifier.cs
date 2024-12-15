using UnityEngine;

public class TrailModifier : IFeatureModifier
{
    public void ApplyFeature(float[,] heights, TerrainGenerationSettings settings, Vector2 startPoint, float intensity, float width)
    {
        Vector2[] trailPoints = GenerateTrailPoints(startPoint, new Vector2(0.9f, 0.9f), settings.trailResolution);

        foreach (var point in trailPoints)
        {
            int centerX = Mathf.RoundToInt(point.x * heights.GetLength(0));
            int centerY = Mathf.RoundToInt(point.y * heights.GetLength(1));

            for (int x = -Mathf.CeilToInt(width); x <= Mathf.CeilToInt(width); x++)
            {
                for (int y = -Mathf.CeilToInt(width); y <= Mathf.CeilToInt(width); y++)
                {
                    int targetX = Mathf.Clamp(centerX + x, 0, heights.GetLength(0) - 1);
                    int targetY = Mathf.Clamp(centerY + y, 0, heights.GetLength(1) - 1);

                    float distance = Vector2.Distance(new Vector2(centerX, centerY), new Vector2(targetX, targetY));
                    float falloff = Mathf.Clamp01(1 - (distance / width));

                    heights[targetX, targetY] -= intensity * falloff;
                }
            }
        }
    }

    private Vector2[] GenerateTrailPoints(Vector2 start, Vector2 end, int resolution)
    {
        Vector2[] points = new Vector2[resolution];
        for (int i = 0; i < resolution; i++)
        {
            float t = i / (float)(resolution - 1);
            points[i] = Vector2.Lerp(start, end, t);
        }
        return points;
    }
}
