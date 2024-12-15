using UnityEngine;

public class RiverModifier : IFeatureModifier
{
    public void ApplyFeature(float[,] heights, TerrainGenerationSettings settings, Vector2 startPoint, float intensity, float width)
    {
        int terrainWidth = heights.GetLength(0);
        int terrainLength = heights.GetLength(1);
        int startX = Mathf.RoundToInt(startPoint.x * terrainWidth);
        int startY = Mathf.RoundToInt(startPoint.y * terrainLength);

        Vector2 currentPoint = new Vector2(startX, startY);

        for (int i = 0; i < 100; i++) // Limit iterations for performance
        {
            Vector2 nextPoint = FindLowestNeighbor(heights, currentPoint, terrainWidth, terrainLength);
            if (nextPoint == currentPoint) break;

            CreateRiverSection(heights, currentPoint, nextPoint, intensity, width);
            currentPoint = nextPoint;
        }
    }

    private Vector2 FindLowestNeighbor(float[,] heights, Vector2 point, int width, int length)
    {
        float minHeight = heights[(int)point.x, (int)point.y];
        Vector2 lowestPoint = point;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                int nx = Mathf.Clamp((int)point.x + x, 0, width - 1);
                int ny = Mathf.Clamp((int)point.y + y, 0, length - 1);

                if (heights[nx, ny] < minHeight)
                {
                    minHeight = heights[nx, ny];
                    lowestPoint = new Vector2(nx, ny);
                }
            }
        }

        return lowestPoint;
    }

    private void CreateRiverSection(float[,] heights, Vector2 start, Vector2 end, float intensity, float width)
    {
        int terrainWidth = heights.GetLength(0);
        int terrainLength = heights.GetLength(1);

        int startX = Mathf.RoundToInt(start.x);
        int startY = Mathf.RoundToInt(start.y);
        int endX = Mathf.RoundToInt(end.x);
        int endY = Mathf.RoundToInt(end.y);

        // Linear interpolation between points
        int steps = Mathf.Max(Mathf.Abs(endX - startX), Mathf.Abs(endY - startY));
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            int currentX = Mathf.RoundToInt(Mathf.Lerp(startX, endX, t));
            int currentY = Mathf.RoundToInt(Mathf.Lerp(startY, endY, t));

            for (int x = -Mathf.CeilToInt(width); x <= Mathf.CeilToInt(width); x++)
            {
                for (int y = -Mathf.CeilToInt(width); y <= Mathf.CeilToInt(width); y++)
                {
                    int targetX = Mathf.Clamp(currentX + x, 0, terrainWidth - 1);
                    int targetY = Mathf.Clamp(currentY + y, 0, terrainLength - 1);

                    float distance = Vector2.Distance(new Vector2(currentX, currentY), new Vector2(targetX, targetY));
                    float falloff = Mathf.Clamp01(1 - (distance / width));

                    heights[targetX, targetY] -= intensity * falloff;
                }
            }
        }
    }
}
