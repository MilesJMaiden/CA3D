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
        var defaultMappings = new List<TerrainGenerationSettings.TerrainTextureMapping>
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

        // Add the trail layer mapping dynamically if trails are enabled
        if (terrainSettings.useTrails)
        {
            var trailLayer = Resources.Load<TerrainLayer>("DefaultTrailLayer");
            if (trailLayer != null)
            {
                terrainSettings.trailMapping = new TerrainGenerationSettings.TerrainTextureMapping
                {
                    terrainLayer = trailLayer,
                    minHeight = 0f, // Full coverage
                    maxHeight = 1f
                };
                defaultMappings.Add(terrainSettings.trailMapping);
                Debug.Log("Default trail mapping added successfully.");
            }
            else
            {
                Debug.LogWarning("Trail layer could not be loaded from resources.");
            }
        }

        terrainSettings.textureMappings = defaultMappings.ToArray();
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
        TerrainLayer[] layers = terrainData.terrainLayers;
        if (layers == null || layers.Length == 0)
        {
            Debug.LogError("No terrain layers assigned. Cannot apply textures.");
            return;
        }

        if (settings.textureMappings == null || settings.textureMappings.Length != layers.Length)
        {
            Debug.LogError($"Mismatch between terrain layers ({layers.Length}) and texture mappings ({settings.textureMappings?.Length ?? 0}). Cannot proceed.");
            return;
        }

        int layerCount = layers.Length;
        float[,,] splatmap = new float[width, length, layerCount];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                float height = heights[x, y];
                for (int i = 0; i < layerCount; i++)
                {
                    var mapping = settings.textureMappings[i];
                    splatmap[x, y, i] = (height >= mapping.minHeight && height <= mapping.maxHeight) ? 1.0f : 0.0f;
                }
            }
        }

        NormalizeSplatmap(splatmap);
        terrainData.SetAlphamaps(0, 0, splatmap);
    }

    /// <summary>
    /// Determines if the given position corresponds to a trail location.
    /// </summary>
    private bool IsTrailPosition(TerrainGenerationSettings settings, int x, int y, int width, int length)
    {
        if (!settings.useTrails)
            return false;

        Vector2 normalizedPosition = new Vector2((float)x / width, (float)y / length);
        float distanceToTrail = Vector2.Distance(normalizedPosition, settings.trailEndPoint);

        // Check if the position falls within the trail width
        return distanceToTrail <= settings.trailWidth / Mathf.Max(width, length);
    }


    /// <summary>
    /// Applies the terrain layers from the current texture mappings to the TerrainData.
    /// </summary>
    public void ApplyTerrainLayers()
    {
        if (terrainSettings == null || terrainSettings.textureMappings == null || terrainSettings.textureMappings.Length == 0)
        {
            Debug.LogWarning("No valid texture mappings available to apply. Skipping layer application.");
            return;
        }

        TerrainLayer[] layers = GetTerrainLayers(terrainSettings.textureMappings);
        if (layers == null || layers.Length == 0)
        {
            Debug.LogError("Failed to retrieve valid TerrainLayers. Cannot apply layers.");
            return;
        }

        m_TerrainData.terrainLayers = layers;
        Debug.Log("Terrain layers successfully applied.");
    }


    /// <summary>
    /// Converts texture mappings from the scriptable object into an array of TerrainLayer objects.
    /// </summary>
    /// <param name="mappings">Array of texture mappings from the TerrainGenerationSettings.</param>
    /// <returns>An array of TerrainLayer objects.</returns>
    private TerrainLayer[] GetTerrainLayers(TerrainGenerationSettings.TerrainTextureMapping[] mappings)
    {
        if (mappings == null || mappings.Length == 0)
        {
            Debug.LogError("No texture mappings provided.");
            return null;
        }

        List<TerrainLayer> layers = new List<TerrainLayer>();

        foreach (var mapping in mappings)
        {
            if (mapping.terrainLayer != null)
            {
                layers.Add(mapping.terrainLayer);
            }
            else
            {
                Debug.LogError($"Missing TerrainLayer in texture mapping with range {mapping.minHeight}-{mapping.maxHeight}. Ensure all mappings have valid TerrainLayers.");
            }
        }

        return layers.ToArray();
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
