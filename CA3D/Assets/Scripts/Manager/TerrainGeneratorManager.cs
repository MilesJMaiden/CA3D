using System.Collections.Generic;
using Unity.Collections;
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
        ValidateAndAdjustDimensions();
        InitializeGenerator();

        m_TerrainData.heightmapResolution = width + 1;
        m_TerrainData.size = new Vector3(width, height, length);

        SyncTerrainLayersWithMappings();

        float[,] heights = terrainGenerator.GenerateHeights(width, length);

        if (heights != null)
        {
            m_TerrainData.SetHeights(0, 0, heights);

            // Apply textures with biomes
            if (terrainGenerator is TerrainGenerator generator && generator.BiomeIndices.IsCreated)
            {
                ApplyTexturesWithBiomes(heights, generator.BiomeIndices, terrainSettings, width, length, m_TerrainData);
                generator.BiomeIndices.Dispose();
            }
            else
            {
                ApplyTextures(heights, terrainSettings, width, length, m_TerrainData);
            }

            // Notify FeatureManager to re-place features
            FeatureManager featureManager = GetComponent<FeatureManager>();
            if (featureManager != null)
            {
                featureManager.PlaceFeatures();
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

        terrainSettings.textureMappings = defaultMappings.ToArray();
        Debug.Log("Default texture mappings assigned.");
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

        if (m_Terrain.terrainData == null)
        {
            m_TerrainData = new TerrainData
            {
                heightmapResolution = width + 1,
                size = new Vector3(width, height, length)
            };
            m_Terrain.terrainData = m_TerrainData;
            Debug.Log("TerrainData dynamically created.");
        }
        else
        {
            m_TerrainData = m_Terrain.terrainData;
        }

        // Reset terrain layers to avoid layer mismatches
        m_TerrainData.terrainLayers = null;

        // Adjust width and length to match alphamap resolution
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
            Debug.LogWarning($"Mismatch between terrain layers ({layers.Length}) and texture mappings ({settings.textureMappings?.Length ?? 0}). Synchronizing layers...");
            SyncTerrainLayersWithMappings();
            layers = terrainData.terrainLayers; // Refresh layers after synchronization
        }

        int layerCount = layers.Length;
        float[,,] splatmap = new float[width, length, layerCount];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                float height = heights[x, y];
                float slope = Mathf.Abs(terrainData.GetSteepness(x / (float)width, y / (float)length) / 90f);

                for (int i = 0; i < layerCount; i++)
                {
                    var mapping = settings.textureMappings[i];
                    float heightBlend = Mathf.InverseLerp(mapping.minHeight, mapping.maxHeight, height);
                    float slopeBlend = Mathf.InverseLerp(0f, 1f, slope);

                    splatmap[x, y, i] = heightBlend * (1f - slopeBlend); // Combine height and slope influence
                }
            }
        }

        NormalizeSplatmap(splatmap);
        terrainData.SetAlphamaps(0, 0, splatmap);
        Debug.Log("Textures successfully applied to terrain.");
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

    /// <summary>
    /// Synchronizes terrain layers with the current texture mappings.
    /// </summary>
    private void SyncTerrainLayersWithMappings()
    {
        if (terrainSettings.textureMappings == null || terrainSettings.textureMappings.Length == 0)
        {
            Debug.LogError("Texture mappings are null or empty. Cannot sync terrain layers.");
            return;
        }

        var layers = new List<TerrainLayer>();
        foreach (var mapping in terrainSettings.textureMappings)
        {
            if (mapping.terrainLayer != null)
            {
                layers.Add(mapping.terrainLayer);
            }
            else
            {
                Debug.LogError($"Texture mapping contains a null TerrainLayer. Skipping mapping: {mapping.minHeight} to {mapping.maxHeight}");
            }
        }

        if (layers.Count == 0)
        {
            Debug.LogError("No valid terrain layers could be created from the texture mappings.");
            return;
        }

        m_TerrainData.terrainLayers = layers.ToArray();
        Debug.Log($"Synchronized terrain layers. Total layers: {m_TerrainData.terrainLayers.Length}");
    }

    private void ApplyTexturesWithBiomes(float[,] heights, NativeArray<int> biomeIndices, TerrainGenerationSettings settings, int width, int length, TerrainData terrainData)
    {
        TerrainLayer[] layers = terrainData.terrainLayers;

        if (layers == null || layers.Length == 0)
        {
            Debug.LogError("No terrain layers assigned. Cannot apply textures.");
            return;
        }

        if (settings.textureMappings == null || settings.textureMappings.Length != layers.Length)
        {
            Debug.LogWarning($"Mismatch between terrain layers ({layers.Length}) and texture mappings ({settings.textureMappings?.Length ?? 0}). Synchronizing layers...");
            SyncTerrainLayersWithMappings();
            layers = terrainData.terrainLayers; // Refresh layers after synchronization
        }

        int layerCount = layers.Length;
        float[,,] splatmap = new float[width, length, layerCount];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                float height = heights[x, y];
                float slope = Mathf.Abs(terrainData.GetSteepness(x / (float)width, y / (float)length) / 90f);
                int biomeIndex = biomeIndices[x + y * width];

                for (int i = 0; i < layerCount; i++)
                {
                    var mapping = settings.textureMappings[i];

                    // Height-based blending
                    float heightBlend = Mathf.InverseLerp(mapping.minHeight, mapping.maxHeight, height);

                    // Slope influence
                    float slopeBlend = Mathf.InverseLerp(0f, 1f, slope);

                    // Biome-based blending
                    float biomeBlend = (i == biomeIndex) ? 1f : 0.2f; // Stronger weight for matching biome

                    // Final weight calculation
                    splatmap[x, y, i] = heightBlend * (1f - slopeBlend) * biomeBlend;
                }
            }
        }

        NormalizeSplatmap(splatmap);
        terrainData.SetAlphamaps(0, 0, splatmap);
        Debug.Log("Textures successfully applied with biomes and height blending.");
    }



    #endregion
}