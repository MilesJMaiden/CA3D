using UnityEngine;

/// <summary>
/// Manages terrain generation using specified settings and handles related validations and errors.
/// </summary>
[RequireComponent(typeof(Terrain))]
public class TerrainGeneratorManager : MonoBehaviour
{
    #region Fields and Properties

    [Header("Terrain Dimensions")]
    [Tooltip("The width of the terrain.")]
    public int width = 256;

    [Tooltip("The length of the terrain.")]
    public int length = 256;

    [Tooltip("The maximum height of the terrain.")]
    public int height = 50;

    [Header("Settings")]
    [Tooltip("The settings used for terrain generation.")]
    public TerrainGenerationSettings terrainSettings;

    [Header("Optional UI Manager (for error reporting)")]
    [Tooltip("The UI Manager for displaying errors (optional).")]
    public TerrainUIManager uiManager;

    private ITerrainGenerator terrainGenerator;
    private Terrain m_Terrain;
    private TerrainData m_TerrainData;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeTerrainComponents();
        ValidateAndAdjustDimensions();
        InitializeGenerator();
        GenerateTerrain();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Generates the terrain using the current settings and updates the Unity terrain component.
    /// </summary>
    public void GenerateTerrain()
    {
        if (terrainSettings == null)
        {
            ReportError("Terrain settings are not assigned.");
            return;
        }

        ValidateAndAdjustDimensions();

        if (!InitializeGenerator())
        {
            return; // Stop if generator initialization fails
        }

        m_TerrainData.heightmapResolution = width + 1;
        m_TerrainData.size = new Vector3(width, height, length);

        float[,] heights = terrainGenerator.GenerateHeights(width, length);

        if (heights != null)
        {
            Debug.Log($"Offsets: Perlin - {terrainSettings.perlinOffset}, fBm - {terrainSettings.fBmOffset}");
            m_TerrainData.SetHeights(0, 0, heights);
        }
    }


    #endregion

    #region Private Methods

    /// <summary>
    /// Initializes the terrain and its data components.
    /// </summary>
    private void InitializeTerrainComponents()
    {
        m_Terrain = GetComponent<Terrain>();
        m_TerrainData = m_Terrain.terrainData;
    }

    /// <summary>
    /// Initializes the terrain generator based on the current settings.
    /// </summary>
    /// <returns>True if the generator was successfully initialized; otherwise, false.</returns>
    private bool InitializeGenerator()
    {
        if (terrainSettings == null)
        {
            ReportError("Terrain settings are null. Cannot initialize generator.");
            return false;
        }

        if (terrainSettings.useVoronoiBiomes)
        {
            if (terrainSettings.voronoiFalloffCurve == null)
            {
                ReportError("Voronoi Falloff Curve is null. Assign a valid AnimationCurve.");
                return false;
            }

            if (terrainSettings.voronoiDistributionMode == TerrainGenerationSettings.DistributionMode.Custom &&
                (terrainSettings.customVoronoiPoints == null || terrainSettings.customVoronoiPoints.Count == 0))
            {
                ReportError("Custom Voronoi Points are null or empty.");
                return false;
            }
        }

        terrainGenerator = new TerrainGenerator(terrainSettings);
        return true;
    }


    /// <summary>
    /// Validates and adjusts the terrain dimensions to ensure compatibility with midpoint displacement.
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
    /// Adjusts a dimension value to the nearest valid size (2^n + 1).
    /// </summary>
    /// <param name="value">The dimension value to adjust.</param>
    /// <returns>The adjusted dimension value.</returns>
    private int AdjustToPowerOfTwoPlusOne(int value)
    {
        int power = Mathf.CeilToInt(Mathf.Log(value - 1, 2));
        return (int)Mathf.Pow(2, power) + 1;
    }

    /// <summary>
    /// Reports an error message to the Unity console and optionally to the UI manager.
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

    #endregion
}
