using System.Collections.Generic;
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

    [Header("Default Materials")]

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
        if (terrainSettings == null)
        {
            Debug.LogError("Terrain settings are not assigned to TerrainGeneratorManager. Please assign a valid ScriptableObject.");
            return;
        }

        InitializeTerrainComponents();

        Debug.Log($"TerrainData resolutions: " +
              $"heightmapResolution = {m_TerrainData.heightmapResolution}, " +
              $"alphamapResolution = {m_TerrainData.alphamapResolution}, " +
              $"detailResolution = {m_TerrainData.detailResolution}");

        ValidateAndAdjustDimensions();

        if (terrainSettings.textureMappings == null || terrainSettings.textureMappings.Length == 0)
        {
            Debug.LogWarning("Texture mappings in the ScriptableObject are empty. Assigning default texture mappings.");
            AssignDefaultTextures();
        }

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
            Debug.LogError("Terrain settings not assigned!");
            return;
        }

        ValidateAndAdjustDimensions();
        InitializeGenerator();

        int alphamapResolution = m_TerrainData.alphamapResolution;

        // Validate and synchronize dimensions
        if (width != alphamapResolution || length != alphamapResolution)
        {
            Debug.LogWarning($"Adjusting width and length to match alphamapResolution: {alphamapResolution}");
            width = alphamapResolution;
            length = alphamapResolution;
        }

        m_TerrainData.heightmapResolution = width + 1;
        m_TerrainData.size = new Vector3(width, height, length);

        float[,] heights = terrainGenerator.GenerateHeights(width, length);

        if (heights != null)
        {
            m_TerrainData.SetHeights(0, 0, heights);

            if (terrainSettings.textureMappings == null || terrainSettings.textureMappings.Length == 0)
            {
                Debug.LogWarning("Texture mappings in the ScriptableObject are empty. Falling back to default texture mappings.");
                AssignDefaultTextures();
            }

            if (terrainSettings.textureMappings != null && terrainSettings.textureMappings.Length > 0)
            {
                ApplyTextures(heights, terrainSettings, width, length, m_TerrainData);
            }
            else
            {
                Debug.LogError("Fallback to default texture mappings failed. Aborting texture application.");
            }
        }
        else
        {
            Debug.LogError("Heights array is null. Terrain generation aborted.");
        }
    }

    private void AssignDefaultTextures()
    {
        if (terrainSettings.textureMappings == null || terrainSettings.textureMappings.Length == 0)
        {
            Debug.LogWarning("No texture mappings defined in the settings. Assigning default TerrainLayers.");
            terrainSettings.textureMappings = new TerrainGenerationSettings.TerrainTextureMapping[]
            {
            new TerrainGenerationSettings.TerrainTextureMapping
            {
                terrainLayer = Resources.Load<TerrainLayer>("DefaultGrassLayer"),
                minHeight = 0.0f,
                maxHeight = 0.3f
            },
            new TerrainGenerationSettings.TerrainTextureMapping
            {
                terrainLayer = Resources.Load<TerrainLayer>("DefaultRockLayer"),
                minHeight = 0.3f,
                maxHeight = 0.6f
            },
            new TerrainGenerationSettings.TerrainTextureMapping
            {
                terrainLayer = Resources.Load<TerrainLayer>("DefaultSnowLayer"),
                minHeight = 0.6f,
                maxHeight = 1.0f
            }
            };

            Debug.Log("Default TerrainLayers assigned successfully.");
        }
        else
        {
            Debug.Log("Texture mappings are already defined in the ScriptableObject. Skipping default assignment.");
        }
    }


    /// <summary>
    /// Normalizes the splatmap to ensure the sum of weights for all texture layers is 1.0.
    /// </summary>
    private void NormalizeSplatmap(float[,,] splatmap)
    {
        int width = splatmap.GetLength(0);
        int length = splatmap.GetLength(1);
        int layers = splatmap.GetLength(2);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                float total = 0f;

                for (int i = 0; i < layers; i++)
                {
                    total += splatmap[x, y, i];
                }

                if (total > 0f)
                {
                    for (int i = 0; i < layers; i++)
                    {
                        splatmap[x, y, i] /= total;
                    }
                }
            }
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

        // Adjust width and length to match alphamapResolution
        int alphamapResolution = m_TerrainData.alphamapResolution;
        width = alphamapResolution;
        length = alphamapResolution;
    }


    /// <summary>
    /// Applies textures to the terrain using the provided settings and heightmap.
    /// </summary>
    private void ApplyTextures(float[,] heights, TerrainGenerationSettings settings, int width, int length, TerrainData terrainData)
    {
        if (settings.textureMappings == null || settings.textureMappings.Length == 0)
        {
            Debug.LogError("Texture mappings are empty. Cannot apply textures.");
            return;
        }

        Debug.Log($"Applying {settings.textureMappings.Length} texture mappings...");

        // Validate alphamap resolution
        int alphamapResolution = terrainData.alphamapResolution;
        if (width != alphamapResolution || length != alphamapResolution)
        {
            Debug.LogError($"Splatmap dimensions do not match alphamapResolution. Width: {width}, Length: {length}, AlphamapResolution: {alphamapResolution}");
            return;
        }

        // Prepare the splatmap array
        float[,,] splatmap = new float[alphamapResolution, alphamapResolution, settings.textureMappings.Length];

        for (int x = 0; x < alphamapResolution; x++)
        {
            for (int y = 0; y < alphamapResolution; y++)
            {
                float height = heights[x, y];

                for (int i = 0; i < settings.textureMappings.Length; i++)
                {
                    var mapping = settings.textureMappings[i];

                    if (mapping.terrainLayer == null)
                    {
                        Debug.LogError($"Texture mapping for layer {i} is null. Please assign a valid TerrainLayer.");
                        return;
                    }

                    if (height >= mapping.minHeight && height <= mapping.maxHeight)
                    {
                        splatmap[x, y, i] = 1.0f; // Assign full weight to the corresponding texture layer
                    }
                    else
                    {
                        splatmap[x, y, i] = 0.0f;
                    }
                }
            }
        }

        // Normalize splatmap values
        NormalizeSplatmap(splatmap);

        // Apply the splatmap
        terrainData.SetAlphamaps(0, 0, splatmap);
        Debug.Log("Textures successfully applied to terrain.");
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
