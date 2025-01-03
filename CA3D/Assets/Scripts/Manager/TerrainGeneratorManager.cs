using System;
using System.Collections.Generic;
using System.Linq;
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

    private Dictionary<TerrainLayer, int> CachedLayerMappings;

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

    public void GenerateTerrain()
    {
        ValidateVoronoiCellCount();
        ValidateAndAdjustDimensions();
        InitializeGenerator();

        // Ensure biomes or fallback mappings exist
        if (terrainSettings.useVoronoiBiomes)
        {
            if (terrainSettings.biomes == null || terrainSettings.biomes.Length == 0)
            {
                Debug.LogWarning("Biomes are not defined in settings. Adding default biomes.");
                AddDefaultBiomes(terrainSettings);
            }

            SyncTerrainLayersWithBiomes();
        }
        else
        {
            if (terrainSettings.textureMappings == null || terrainSettings.textureMappings.Length == 0)
            {
                Debug.LogWarning("Texture mappings are not defined in settings. Adding default texture mappings.");
                AssignDefaultTextures();
            }

            SyncTerrainLayersWithMappings();
        }

        // Update terrain data dimensions
        m_TerrainData.heightmapResolution = width + 1;
        m_TerrainData.size = new Vector3(width, height, length);

        // Initialize native arrays
        NativeArray<float> heightsNative = default;
        NativeArray<int> biomeIndices = default;
        NativeArray<int> terrainLayerIndices = default;

        try
        {
            // Generate terrain heights
            float[,] heights = terrainGenerator.GenerateHeights(width, length);

            if (heights != null)
            {
                m_TerrainData.SetHeights(0, 0, heights);

                if (terrainSettings.useVoronoiBiomes)
                {
                    // Generate biome indices and terrain layer indices
                    biomeIndices = GenerateBiomeIndices(heights);
                    terrainLayerIndices = new NativeArray<int>(width * length, Allocator.TempJob);

                    if (biomeIndices.IsCreated && terrainLayerIndices.IsCreated)
                    {
                        // Apply textures with biomes using the terrain data
                        ApplyTexturesWithBiomes(heights, biomeIndices, terrainLayerIndices, terrainSettings, width, length, m_TerrainData);
                    }
                    else
                    {
                        Debug.LogError("Biome indices or terrain layer indices were not successfully created.");
                    }
                }
                else
                {
                    // Apply fallback textures
                    ApplyTextures(heights, terrainSettings, width, length, m_TerrainData);
                }
            }
            else
            {
                Debug.LogError("Heights array is null. Terrain generation aborted.");
            }
        }
        finally
        {
            // Dispose of native arrays if they were created
            if (heightsNative.IsCreated) heightsNative.Dispose();
            if (biomeIndices.IsCreated) biomeIndices.Dispose();
            if (terrainLayerIndices.IsCreated) terrainLayerIndices.Dispose();
        }
    }

    private void ValidateVoronoiCellCount()
    {
        if (terrainSettings.useVoronoiBiomes)
        {
            int maxCells = terrainSettings.biomes.Length;
            if (terrainSettings.voronoiCellCount > maxCells)
            {
                //Debug.LogWarning($"Voronoi cell count exceeds biome count ({maxCells}). Clamping to biome count.");
                terrainSettings.voronoiCellCount = maxCells;
            }
        }
    }

    /// <summary>
    /// Generates biome indices based on the heightmap.
    /// </summary>
    /// <param name="heights">The heightmap of the terrain.</param>
    /// <returns>A NativeArray containing biome indices for each point.</returns>
    private NativeArray<int> GenerateBiomeIndices(float[,] heights)
    {
        int resolution = heights.GetLength(0);
        NativeArray<int> biomeIndices = new NativeArray<int>(resolution * resolution, Allocator.TempJob);

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float height = heights[x, y];
                biomeIndices[x + y * resolution] = DetermineBiomeIndex(height);
            }
        }

        return biomeIndices;
    }

    /// <summary>
    /// Determines the biome index based on a given height.
    /// </summary>
    /// <param name="height">The height value.</param>
    /// <returns>The index of the biome.</returns>
    private int DetermineBiomeIndex(float height)
    {
        if (terrainSettings.biomes == null || terrainSettings.biomes.Length == 0) return -1;

        for (int i = 0; i < terrainSettings.biomes.Length; i++)
        {
            var thresholds = terrainSettings.biomes[i].thresholds;

            if ((height >= thresholds.minHeight1 && height <= thresholds.maxHeight1) ||
                (height >= thresholds.minHeight2 && height <= thresholds.maxHeight2) ||
                (height >= thresholds.minHeight3 && height <= thresholds.maxHeight3))
            {
                return i;
            }
        }

        return -1; // No matching biome found
    }

    /// <summary>
    /// Synchronizes terrain layers with the defined biomes in the settings.
    /// </summary>
    private void SyncTerrainLayersWithBiomes()
    {
        if (terrainSettings.biomes == null || terrainSettings.biomes.Length == 0)
        {
            Debug.LogError("Biomes are null or empty. Cannot sync terrain layers.");
            return;
        }

        var layers = new HashSet<TerrainLayer>();
        foreach (var biome in terrainSettings.biomes)
        {
            layers.Add(biome.thresholds.layer1);
            layers.Add(biome.thresholds.layer2);
            layers.Add(biome.thresholds.layer3);
        }

        m_TerrainData.terrainLayers = layers.Where(layer => layer != null).ToArray();
        CachedLayerMappings = CacheLayerMappings(m_TerrainData.terrainLayers);

        Debug.Log($"Synchronized {m_TerrainData.terrainLayers.Length} terrain layers from biomes.");
    }

    private Dictionary<TerrainLayer, int> CacheLayerMappings(TerrainLayer[] layers)
    {
        var layerMapping = new Dictionary<TerrainLayer, int>();

        for (int i = 0; i < layers.Length; i++)
        {
            if (!layerMapping.ContainsKey(layers[i]))
                layerMapping[layers[i]] = i;
        }

        return layerMapping;
    }


    private void AddDefaultBiomes(TerrainGenerationSettings settings)
    {
        settings.biomes = new TerrainGenerationSettings.Biome[]
        {
        new TerrainGenerationSettings.Biome
        {
            name = "Grassland",
            thresholds = new TerrainGenerationSettings.BiomeThresholds
            {
                layer1 = Resources.Load<TerrainLayer>("DefaultGrassLayer"),
                minHeight1 = 0f,
                maxHeight1 = 0.1f,

                layer2 = Resources.Load<TerrainLayer>("DefaultGrassLayerDense"),
                minHeight2 = 0.1f,
                maxHeight2 = 0.2f,

                layer3 = Resources.Load<TerrainLayer>("DefaultMeadowLayer"),
                minHeight3 = 0.2f,
                maxHeight3 = 0.3f,
            }
        },
        new TerrainGenerationSettings.Biome
        {
            name = "Rocky",
            thresholds = new TerrainGenerationSettings.BiomeThresholds
            {
                layer1 = Resources.Load<TerrainLayer>("DefaultRockLayer"),
                minHeight1 = 0.3f,
                maxHeight1 = 0.4f,

                layer2 = Resources.Load<TerrainLayer>("DefaultRockLayerDense"),
                minHeight2 = 0.4f,
                maxHeight2 = 0.5f,

                layer3 = Resources.Load<TerrainLayer>("DefaultCliffLayer"),
                minHeight3 = 0.5f,
                maxHeight3 = 0.6f,
            }
        },
        new TerrainGenerationSettings.Biome
        {
            name = "Snow",
            thresholds = new TerrainGenerationSettings.BiomeThresholds
            {
                layer1 = Resources.Load<TerrainLayer>("DefaultSnowLayer"),
                minHeight1 = 0.6f,
                maxHeight1 = 0.7f,

                layer2 = Resources.Load<TerrainLayer>("DefaultSnowLayerDense"),
                minHeight2 = 0.7f,
                maxHeight2 = 0.8f,

                layer3 = Resources.Load<TerrainLayer>("DefaultIceLayer"),
                minHeight3 = 0.8f,
                maxHeight3 = 1f,
            }
        }
        };

        Debug.Log("Default biomes added to settings with multiple thresholds.");
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
        Debug.Log($"Synchronized terrain layers from texture mappings. Total layers: {m_TerrainData.terrainLayers.Length}");
    }

    /// <summary>
    /// Applies textures to the terrain using Voronoi biomes.
    /// </summary>
    /// <summary>
    /// Applies textures to the terrain using Voronoi biomes.
    /// </summary>
    private void ApplyTexturesWithBiomes(
        float[,] heights,
        NativeArray<int> biomeIndices,
        NativeArray<int> terrainLayerIndices,
        TerrainGenerationSettings settings,
        int width,
        int length,
        TerrainData terrainData)
    {
        TerrainLayer[] layers = terrainData.terrainLayers;

        if (layers == null || layers.Length == 0)
        {
            Debug.LogError("No terrain layers assigned. Cannot apply textures.");
            return;
        }

        int layerCount = layers.Length;
        float[,,] splatmap = new float[width, length, layerCount];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                int index = x + y * width;
                int biomeIndex = biomeIndices[index];
                float height = heights[x, y];

                if (biomeIndex < 0 || biomeIndex >= settings.biomes.Length) continue;

                var thresholds = settings.biomes[biomeIndex].thresholds;

                // Assign weights for each layer based on thresholds
                AssignLayerWeights(splatmap, x, y, layers, thresholds, height);
            }
        }

        NormalizeSplatmap(splatmap);
        terrainData.SetAlphamaps(0, 0, splatmap);
        Debug.Log("Textures successfully applied with Voronoi biomes and blending.");
    }

    private void AssignLayerWeights(
        float[,,] splatmap,
        int x,
        int y,
        TerrainLayer[] layers,
        TerrainGenerationSettings.BiomeThresholds thresholds,
        float height)
    {
        // Iterate through all thresholds and assign weights
        AssignWeight(splatmap, x, y, layers, thresholds.layer1, thresholds.minHeight1, thresholds.maxHeight1, height);
        AssignWeight(splatmap, x, y, layers, thresholds.layer2, thresholds.minHeight2, thresholds.maxHeight2, height);
        AssignWeight(splatmap, x, y, layers, thresholds.layer3, thresholds.minHeight3, thresholds.maxHeight3, height);
    }

    private void AssignWeight(
    float[,,] splatmap,
    int x,
    int y,
    TerrainLayer[] layers,
    TerrainLayer layer,
    float minHeight,
    float maxHeight,
    float height)
    {
        if (layer != null && height >= minHeight && height <= maxHeight)
        {
            int layerIndex = Array.IndexOf(layers, layer);
            if (layerIndex >= 0)
            {
                float weight = CalculateWeight(height, minHeight, maxHeight);
                splatmap[x, y, layerIndex] += weight;
            }
        }
    }

    private float CalculateWeight(float height, float minHeight, float maxHeight)
    {
        float range = maxHeight - minHeight;
        float center = minHeight + range / 2f;

        // Higher weight closer to the center of the range
        return Mathf.Clamp01(1f - Mathf.Abs(height - center) / (range / 2f));
    }

    #endregion
}