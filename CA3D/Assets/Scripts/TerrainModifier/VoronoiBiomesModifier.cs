using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A height modifier that applies Voronoi-based biome generation to a terrain heightmap.
/// </summary>
public class VoronoiBiomesModifier : IHeightModifier
{
    #region Public Methods

    /// <summary>
    /// Modifies the terrain heights based on Voronoi biome generation.
    /// </summary>
    /// <param name="heights">The 2D array of terrain heights to modify.</param>
    /// <param name="settings">The terrain generation settings used for Voronoi generation.</param>
    public void ModifyHeight(float[,] heights, TerrainGenerationSettings settings)
    {
        int width = heights.GetLength(0);
        int length = heights.GetLength(1);

        // Generate Voronoi points
        List<Vector2> points = GenerateVoronoiPoints(settings, width, length);

        float maxDistance = Mathf.Max(width, length);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                float minDistSquared = float.MaxValue;
                int closestPointIndex = -1;

                Vector2 currentPoint = new Vector2(x, y);

                // Find the closest Voronoi point
                for (int i = 0; i < points.Count; i++)
                {
                    float distSquared = (points[i] - currentPoint).sqrMagnitude;
                    if (distSquared < minDistSquared)
                    {
                        minDistSquared = distSquared;
                        closestPointIndex = i;
                    }
                }

                // Apply the influence of the closest Voronoi point
                if (closestPointIndex != -1)
                {
                    float normalizedDistance = Mathf.Sqrt(minDistSquared) / maxDistance;
                    float falloffValue = settings.voronoiFalloffCurve.Evaluate(1 - normalizedDistance);
                    heights[x, y] += Mathf.Lerp(settings.voronoiHeightRange.x, settings.voronoiHeightRange.y, falloffValue);
                }
            }
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Generates Voronoi points based on the selected distribution mode.
    /// </summary>
    /// <param name="settings">The terrain generation settings.</param>
    /// <param name="width">The width of the terrain.</param>
    /// <param name="length">The length of the terrain.</param>
    /// <returns>A list of Voronoi points.</returns>
    private List<Vector2> GenerateVoronoiPoints(TerrainGenerationSettings settings, int width, int length)
    {
        HashSet<Vector2> points = new HashSet<Vector2>();

        switch (settings.voronoiDistributionMode)
        {
            case TerrainGenerationSettings.DistributionMode.Random:
                while (points.Count < settings.voronoiCellCount)
                {
                    Vector2 randomPoint = new Vector2(Random.Range(0, width), Random.Range(0, length));
                    points.Add(randomPoint); // Avoid duplicates with HashSet
                }
                break;

            case TerrainGenerationSettings.DistributionMode.Grid:
                int gridSize = Mathf.CeilToInt(Mathf.Sqrt(settings.voronoiCellCount));
                float cellWidth = (float)width / gridSize;
                float cellHeight = (float)length / gridSize;

                for (int x = 0; x < gridSize; x++)
                {
                    for (int y = 0; y < gridSize; y++)
                    {
                        if (points.Count >= settings.voronoiCellCount)
                            break;

                        float px = x * cellWidth + cellWidth / 2f;
                        float py = y * cellHeight + cellHeight / 2f;
                        points.Add(new Vector2(px, py));
                    }
                }
                break;

            case TerrainGenerationSettings.DistributionMode.Custom:
                foreach (var point in settings.customVoronoiPoints)
                {
                    if (!points.Contains(point))
                        points.Add(point);
                }
                break;
        }

        return new List<Vector2>(points);
    }

    #endregion
}
