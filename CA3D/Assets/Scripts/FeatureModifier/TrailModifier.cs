using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Modifies terrain to create trails by adjusting heightmaps and applying a terrain layer.
/// </summary>
public class TrailModifier : IFeatureModifier
{
    public void ApplyFeature(float[,] heights, TerrainGenerationSettings settings, Vector2 startPoint, float intensity, float width)
    {
        if (heights == null)
        {
            Debug.LogError("Heights array is null. Cannot apply trail modifier.");
            return;
        }

        if (!settings.useTrails)
        {
            Debug.LogWarning("Trail generation is disabled in the settings.");
            return;
        }

        Debug.Log("Applying trail modifier...");

        // Get the terrain object
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogError("No active terrain found. Cannot apply trails.");
            return;
        }

        // Load the trail terrain layer
        TerrainLayer trailLayer = Resources.Load<TerrainLayer>("TrailLayer");
        if (trailLayer == null)
        {
            Debug.LogError("TrailLayer not found in Resources. Ensure it exists.");
            return;
        }

        // Ensure the trail layer is added to the terrain
        AddTerrainLayer(terrain, trailLayer);

        // Modify heightmap for trails
        Vector2[] trailPoints = GenerateTrailPoints(settings, startPoint);
        ApplyTrailHeight(heights, settings, trailPoints);

        // Modify splatmap to apply the trail layer
        ApplyTrailLayer(terrain, settings, trailPoints);

        Debug.Log("Trail modifier applied successfully.");
    }

    private void ApplyTrailHeight(float[,] heights, TerrainGenerationSettings settings, Vector2[] trailPoints)
    {
        foreach (var point in trailPoints)
        {
            int centerX = Mathf.RoundToInt(point.x * heights.GetLength(0));
            int centerY = Mathf.RoundToInt(point.y * heights.GetLength(1));

            for (int x = -Mathf.CeilToInt(settings.trailWidth); x <= Mathf.CeilToInt(settings.trailWidth); x++)
            {
                for (int y = -Mathf.CeilToInt(settings.trailWidth); y <= Mathf.CeilToInt(settings.trailWidth); y++)
                {
                    int targetX = Mathf.Clamp(centerX + x, 0, heights.GetLength(0) - 1);
                    int targetY = Mathf.Clamp(centerY + y, 0, heights.GetLength(1) - 1);

                    float distance = Vector2.Distance(new Vector2(centerX, centerY), new Vector2(targetX, targetY));
                    float falloff = Mathf.Clamp01(1 - (distance / settings.trailWidth));

                    // Apply the height modification
                    heights[targetX, targetY] -= settings.trailIntensity * falloff;
                }
            }
        }
    }

    private void ApplyTrailLayer(Terrain terrain, TerrainGenerationSettings settings, Vector2[] trailPoints)
    {
        TerrainData terrainData = terrain.terrainData;
        int resolution = terrainData.alphamapResolution;
        float[,,] splatmap = terrainData.GetAlphamaps(0, 0, resolution, resolution);

        foreach (var point in trailPoints)
        {
            int centerX = Mathf.RoundToInt(point.x * resolution);
            int centerY = Mathf.RoundToInt(point.y * resolution);

            for (int x = -Mathf.CeilToInt(settings.trailWidth); x <= Mathf.CeilToInt(settings.trailWidth); x++)
            {
                for (int y = -Mathf.CeilToInt(settings.trailWidth); y <= Mathf.CeilToInt(settings.trailWidth); y++)
                {
                    int targetX = Mathf.Clamp(centerX + x, 0, resolution - 1);
                    int targetY = Mathf.Clamp(centerY + y, 0, resolution - 1);

                    float distance = Vector2.Distance(new Vector2(centerX, centerY), new Vector2(targetX, targetY));
                    float falloff = Mathf.Clamp01(1 - (distance / settings.trailWidth));

                    // Assign trail layer weight
                    for (int layer = 0; layer < splatmap.GetLength(2); layer++)
                    {
                        splatmap[targetX, targetY, layer] *= 1 - falloff; // Reduce other layers
                    }

                    int trailLayerIndex = GetLayerIndex(terrain, "TrailLayer");
                    if (trailLayerIndex >= 0)
                    {
                        splatmap[targetX, targetY, trailLayerIndex] += falloff; // Increase trail layer weight
                    }
                }
            }
        }

        terrainData.SetAlphamaps(0, 0, splatmap);
    }

    private Vector2[] GenerateTrailPoints(TerrainGenerationSettings settings, Vector2 startPoint)
    {
        Vector2[] points = new Vector2[settings.trailResolution];
        Vector2 currentPoint = startPoint;
        Vector2 endPoint = settings.trailEndPoint;

        for (int i = 0; i < settings.trailResolution; i++)
        {
            float t = i / (float)(settings.trailResolution - 1);

            Vector2 randomOffset = settings.useTrailRandomness
                ? new Vector2(
                    Random.Range(-settings.trailRandomness, settings.trailRandomness),
                    Random.Range(-settings.trailRandomness, settings.trailRandomness))
                : Vector2.zero;

            Vector2 targetPoint = Vector2.Lerp(startPoint, endPoint, t);
            currentPoint = Vector2.Lerp(currentPoint, targetPoint + randomOffset, settings.trailSmoothness);

            points[i] = currentPoint;
        }

        return points;
    }

    private void AddTerrainLayer(Terrain terrain, TerrainLayer layer)
    {
        TerrainData terrainData = terrain.terrainData;
        List<TerrainLayer> layers = new List<TerrainLayer>(terrainData.terrainLayers);

        if (!layers.Contains(layer))
        {
            layers.Add(layer);
            terrainData.terrainLayers = layers.ToArray();
        }
    }

    private int GetLayerIndex(Terrain terrain, string layerName)
    {
        TerrainData terrainData = terrain.terrainData;
        for (int i = 0; i < terrainData.terrainLayers.Length; i++)
        {
            if (terrainData.terrainLayers[i].name == layerName)
                return i;
        }
        return -1; // Layer not found
    }
}
