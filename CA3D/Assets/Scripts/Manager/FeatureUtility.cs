using UnityEngine;

public static class FeatureUtility
{
    /// <summary>
    /// Gets a random position for a feature within the bounds of the terrain, considering feature placement rules.
    /// </summary>
    /// <param name="feature">The feature settings.</param>
    /// <param name="terrainSize">The size of the terrain.</param>
    /// <returns>A random valid position or Vector3.zero if no valid position is found.</returns>
    public static Vector3 GetRandomPosition(FeatureSettings feature, Vector3 terrainSize)
    {
        // Validate feature
        if (feature == null || feature.prefab == null)
        {
            Debug.LogWarning($"Feature {feature?.featureName} is invalid or has no prefab assigned.");
            return Vector3.zero;
        }

        for (int attempt = 0; attempt < 10; attempt++) // Retry logic for generating valid positions
        {
            Vector3 randomPosition = new Vector3(
                Random.Range(0, terrainSize.x),
                0,
                Random.Range(0, terrainSize.z)
            );

            // Sample terrain height
            float terrainHeight = Terrain.activeTerrain.SampleHeight(randomPosition);

            // Validate height range
            if (terrainHeight < feature.heightRange.x * terrainSize.y ||
                terrainHeight > feature.heightRange.y * terrainSize.y)
            {
                continue; // Invalid height
            }

            // Validate slope (if needed)
            float slope = CalculateSlope(randomPosition);
            if (slope < feature.slopeRange.x || slope > feature.slopeRange.y)
            {
                continue; // Invalid slope
            }

            // Apply terrain height to position
            randomPosition.y = terrainHeight;
            return randomPosition; // Return the first valid position
        }

        Debug.LogWarning($"Failed to find a valid position for feature {feature.featureName}.");
        return Vector3.zero;
    }

    /// <summary>
    /// Calculates the slope at a given position on the terrain.
    /// </summary>
    private static float CalculateSlope(Vector3 position)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogWarning("No active terrain found for slope calculation.");
            return 0f;
        }

        Vector3 normal = terrain.terrainData.GetInterpolatedNormal(
            position.x / terrain.terrainData.size.x,
            position.z / terrain.terrainData.size.z
        );

        return Vector3.Angle(normal, Vector3.up); // Angle between normal and up vector
    }
}
