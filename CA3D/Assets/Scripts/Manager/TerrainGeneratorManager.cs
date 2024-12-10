using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TerrainGeneratorManager : MonoBehaviour
{
    [Header("Terrain Dimensions")]
    public int width = 256; // Default dimension
    public int length = 256; // Default dimension
    public int height = 50; // Maximum height

    [Header("Settings")]
    public TerrainGenerationSettings terrainSettings;

    [Header("Optional UI Manager (for error reporting)")]
    public TerrainUIManager uiManager;

    private ITerrainGenerator terrainGenerator;
    private Terrain m_Terrain;
    private TerrainData m_TerrainData;

    private void Awake()
    {
        m_Terrain = GetComponent<Terrain>();
        m_TerrainData = m_Terrain.terrainData;

        ValidateAndAdjustDimensions();
        InitializeGenerator();
        GenerateTerrain();
    }

    public void GenerateTerrain()
    {
        if (terrainSettings == null)
        {
            ReportError("Terrain settings not assigned!");
            return;
        }

        // Validate and adjust terrain dimensions for Midpoint Displacement
        ValidateAndAdjustDimensions();

        // Ensure the generator uses the latest settings
        InitializeGenerator();

        // Generate terrain
        m_TerrainData.heightmapResolution = width + 1;
        m_TerrainData.size = new Vector3(width, height, length);
        float[,] heights = terrainGenerator.GenerateHeights(width, length);

        if (heights != null)
        {
            m_TerrainData.SetHeights(0, 0, heights);
        }
    }

    private void InitializeGenerator()
    {
        if (terrainSettings != null)
        {
            // Validate Voronoi settings before creating the generator
            if (terrainSettings.useVoronoiBiomes)
            {
                if (terrainSettings.voronoiFalloffCurve == null)
                {
                    ReportError("VoronoiFalloffCurve is null. Assign a valid AnimationCurve.");
                    return;
                }

                if (terrainSettings.voronoiDistributionMode == TerrainGenerationSettings.DistributionMode.Custom &&
                    (terrainSettings.customVoronoiPoints == null || terrainSettings.customVoronoiPoints.Count == 0))
                {
                    ReportError("Custom Voronoi Points are null or empty.");
                    return;
                }
            }

            terrainGenerator = new TerrainGenerator(terrainSettings);
        }
        else
        {
            ReportError("Terrain settings are null. Cannot initialize generator.");
        }
    }

    /// <summary>
    /// Validates the terrain dimensions to ensure they conform to 2^n + 1 for Midpoint Displacement.
    /// Adjusts them if necessary.
    /// </summary>
    private void ValidateAndAdjustDimensions()
    {
        if (terrainSettings != null && terrainSettings.useMidPointDisplacement)
        {
            width = AdjustToPowerOfTwoPlusOne(width);
            length = AdjustToPowerOfTwoPlusOne(length);
        }
    }

    /// <summary>
    /// Adjusts the given value to the nearest valid dimension (2^n + 1).
    /// </summary>
    /// <param name="value">The value to adjust.</param>
    /// <returns>The adjusted value.</returns>
    private int AdjustToPowerOfTwoPlusOne(int value)
    {
        int power = Mathf.CeilToInt(Mathf.Log(value - 1, 2));
        return (int)Mathf.Pow(2, power) + 1;
    }

    /// <summary>
    /// Reports errors to the Unity console and optionally to the UI manager.
    /// </summary>
    /// <param name="message">The error message to report.</param>
    private void ReportError(string message)
    {
        Debug.LogError(message);
        if (uiManager != null)
        {
            uiManager.DisplayError(message);
        }
    }
}
