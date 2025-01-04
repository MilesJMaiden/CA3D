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
    [Tooltip("The width of the terrain in units.")]
    [SerializeField, Min(1)]
    private int width = 256;

    [Tooltip("The length of the terrain in units.")]
    [SerializeField, Min(1)]
    private int length = 256;

    [Tooltip("The maximum height of the terrain.")]
    [SerializeField, Min(1)]
    private int height = 50;

    [Header("Settings")]
    [Tooltip("The settings used for terrain generation.")]
    [SerializeField]
    public TerrainGenerationSettings terrainSettings;

    [Header("Default Materials")]
    [Tooltip("Assign default materials for terrain rendering (Optional).")]

    [Header("Optional UI Manager (for error reporting)")]
    [SerializeField]
    private TerrainUIManager uiManager;

    // Internal references for terrain generation and management
    private ITerrainGenerator terrainGenerator;
    private Terrain m_Terrain;
    private TerrainData m_TerrainData;

    // Cached mappings for terrain layers to improve performance during texture application
    private Dictionary<TerrainLayer, int> CachedLayerMappings;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Unity's Awake lifecycle method. Initializes terrain components, validates settings, and generates terrain.
    /// </summary>
    private void Awake()
    {
        if (terrainSettings == null)
        {
            Debug.LogError("Terrain settings are not assigned. Please assign a valid TerrainGenerationSettings ScriptableObject.");
            return;
        }

        // Initialize terrain components
        InitializeTerrainComponents();

        Debug.Log($"TerrainData initialized with resolutions: " +
                  $"Heightmap = {m_TerrainData.heightmapResolution}, " +
                  $"Alphamap = {m_TerrainData.alphamapResolution}, " +
                  $"Detail = {m_TerrainData.detailResolution}");

        // Validate and adjust dimensions based on the settings
        ValidateAndAdjustDimensions();

        // Assign default textures if texture mappings are empty
        if (terrainSettings.textureMappings == null || terrainSettings.textureMappings.Length == 0)
        {
            Debug.LogWarning("Texture mappings are empty in TerrainGenerationSettings. Assigning default mappings.");
            AssignDefaultTextures();
        }

        // Initialize the terrain generator and generate terrain
        if (InitializeGenerator())
        {
            GenerateTerrain();
        }
        else
        {
            Debug.LogError("Failed to initialize the terrain generator. Terrain generation aborted.");
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Generates the terrain based on the current settings and updates the Unity Terrain component.
    /// </summary>
    public void GenerateTerrain()
    {
        // Validate and adjust settings
        ValidateVoronoiCellCount();
        ValidateAndAdjustDimensions();

        // Initialize the terrain generator
        if (!InitializeGenerator())
        {
            Debug.LogError("Terrain generator initialization failed. Aborting terrain generation.");
            return;
        }

        // Ensure biomes or fallback texture mappings exist
        if (terrainSettings.useVoronoiBiomes)
        {
            if (terrainSettings.biomes == null || terrainSettings.biomes.Length == 0)
            {
                Debug.LogWarning("No biomes defined. Adding default biomes.");
                AddDefaultBiomes(terrainSettings);
            }

            SyncTerrainLayersWithBiomes();
        }
        else
        {
            if (terrainSettings.textureMappings == null || terrainSettings.textureMappings.Length == 0)
            {
                Debug.LogWarning("No texture mappings defined. Assigning default texture mappings.");
                AssignDefaultTextures();
            }

            SyncTerrainLayersWithMappings();
        }

        // Configure terrain dimensions
        m_TerrainData.heightmapResolution = width + 1;
        m_TerrainData.size = new Vector3(width, height, length);

        NativeArray<float> heightsNative = default;
        NativeArray<int> biomeIndices = default;
        NativeArray<int> terrainLayerIndices = default;

        try
        {
            // Generate terrain heights
            float[,] heights = terrainGenerator.GenerateHeights(width, length);

            if (heights == null)
            {
                Debug.LogError("Generated heightmap is null. Aborting terrain generation.");
                return;
            }

            m_TerrainData.SetHeights(0, 0, heights);

            if (terrainSettings.useVoronoiBiomes)
            {
                // Generate biome indices and terrain layers
                biomeIndices = GenerateBiomeIndices(heights);
                terrainLayerIndices = new NativeArray<int>(width * length, Allocator.TempJob);

                if (!biomeIndices.IsCreated || !terrainLayerIndices.IsCreated)
                {
                    Debug.LogError("Failed to create biome or terrain layer indices.");
                    return;
                }

                ApplyTexturesWithBiomes(heights, biomeIndices, terrainLayerIndices, terrainSettings, width, length, m_TerrainData);
            }
            else
            {
                ApplyTextures(heights, terrainSettings, width, length, m_TerrainData);
            }
        }
        finally
        {
            // Safely dispose of native arrays
            if (heightsNative.IsCreated) heightsNative.Dispose();
            if (biomeIndices.IsCreated) biomeIndices.Dispose();
            if (terrainLayerIndices.IsCreated) terrainLayerIndices.Dispose();
        }
    }

    #endregion


    #region Private Methods

    /// <summary>
    /// Validates and adjusts the Voronoi cell count based on the available biomes.
    /// Ensures that the cell count does not exceed the number of biomes.
    /// </summary>
    private void ValidateVoronoiCellCount()
    {
        if (terrainSettings.useVoronoiBiomes)
        {
            int maxCells = terrainSettings.biomes.Length;
            if (terrainSettings.voronoiCellCount > maxCells)
            {
                Debug.LogWarning($"Voronoi cell count ({terrainSettings.voronoiCellCount}) exceeds available biomes ({maxCells}). Clamping to biome count.");
                terrainSettings.voronoiCellCount = maxCells;
            }
        }
    }

    /// <summary>
    /// Generates biome indices for each terrain point based on its height.
    /// </summary>
    /// <param name="heights">The 2D array of terrain heights.</param>
    /// <returns>A NativeArray containing the biome index for each point on the terrain.</returns>
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
    /// Determines the appropriate biome index for a given height value.
    /// </summary>
    /// <param name="height">The terrain height at a specific point.</param>
    /// <returns>The index of the corresponding biome, or -1 if no match is found.</returns>
    private int DetermineBiomeIndex(float height)
    {
        if (terrainSettings.biomes == null || terrainSettings.biomes.Length == 0)
            return -1;

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
    /// Ensures each biome and its layers are properly mapped and applied.
    /// </summary>
    private void SyncTerrainLayersWithBiomes()
    {
        if (terrainSettings.biomes == null || terrainSettings.biomes.Length == 0)
        {
            Debug.LogError("Biomes are null or empty. Cannot sync terrain layers.");
            return;
        }

        var layers = new List<TerrainLayer>();

        // Collect all layers from all biomes
        foreach (var biome in terrainSettings.biomes)
        {
            if (biome.thresholds.layer1 != null) layers.Add(biome.thresholds.layer1);
            if (biome.thresholds.layer2 != null) layers.Add(biome.thresholds.layer2);
            if (biome.thresholds.layer3 != null) layers.Add(biome.thresholds.layer3);
        }

        // Ensure layers are unique
        m_TerrainData.terrainLayers = layers.Distinct().ToArray();
        CachedLayerMappings = CacheLayerMappings(m_TerrainData.terrainLayers);

        Debug.Log($"Synchronized {m_TerrainData.terrainLayers.Length} terrain layers from {terrainSettings.biomes.Length} biomes.");
    }

    /// <summary>
    /// Creates a dictionary to map terrain layers to their indices for efficient lookups.
    /// </summary>
    /// <param name="layers">The array of terrain layers.</param>
    /// <returns>A dictionary mapping each layer to its index.</returns>
    private Dictionary<TerrainLayer, int> CacheLayerMappings(TerrainLayer[] layers)
    {
        var layerMapping = new Dictionary<TerrainLayer, int>();

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] != null && !layerMapping.ContainsKey(layers[i]))
                layerMapping[layers[i]] = i;
        }

        return layerMapping;
    }

    /// <summary>
    /// Adds default biomes to the terrain settings if no biomes are defined.
    /// Ensures a minimum set of biomes for terrain generation.
    /// </summary>
    /// <param name="settings">The TerrainGenerationSettings to modify.</param>
    private void AddDefaultBiomes(TerrainGenerationSettings settings)
    {
        settings.biomes = new[]
        {
        CreateBiome("Grassland", "DefaultGrassLayer", 0f, 0.1f, "DefaultGrassLayerDense", 0.1f, 0.2f, "DefaultMeadowLayer", 0.2f, 0.3f),
        CreateBiome("Rocky", "DefaultRockLayer", 0.3f, 0.4f, "DefaultRockLayerDense", 0.4f, 0.5f, "DefaultCliffLayer", 0.5f, 0.6f),
        CreateBiome("Snow", "DefaultSnowLayer", 0.6f, 0.7f, "DefaultSnowLayerDense", 0.7f, 0.8f, "DefaultIceLayer", 0.8f, 1f)
    };

        Debug.Log("Default biomes added to terrain settings.");
    }

    /// <summary>
    /// Helper method to create a biome with defined layers and thresholds.
    /// </summary>
    private TerrainGenerationSettings.Biome CreateBiome(
        string name,
        string layer1, float minHeight1, float maxHeight1,
        string layer2, float minHeight2, float maxHeight2,
        string layer3, float minHeight3, float maxHeight3)
    {
        return new TerrainGenerationSettings.Biome
        {
            name = name,
            thresholds = new TerrainGenerationSettings.BiomeThresholds
            {
                layer1 = Resources.Load<TerrainLayer>(layer1),
                minHeight1 = minHeight1,
                maxHeight1 = maxHeight1,
                layer2 = Resources.Load<TerrainLayer>(layer2),
                minHeight2 = minHeight2,
                maxHeight2 = maxHeight2,
                layer3 = Resources.Load<TerrainLayer>(layer3),
                minHeight3 = minHeight3,
                maxHeight3 = maxHeight3
            }
        };
    }

    /// <summary>
    /// Assigns default texture mappings for the terrain, defining height ranges and layers.
    /// </summary>
    private void AssignDefaultTextures()
    {
        var defaultMappings = new List<TerrainGenerationSettings.TerrainTextureMapping>
    {
        CreateMapping("DefaultGrassLayer", 0.0f, 0.3f),
        CreateMapping("DefaultRockLayer", 0.3f, 0.6f),
        CreateMapping("DefaultSnowLayer", 0.6f, 1.0f)
    };

        terrainSettings.textureMappings = defaultMappings.ToArray();
        Debug.Log("Default texture mappings assigned.");
    }

    /// <summary>
    /// Creates a texture mapping for a terrain layer.
    /// </summary>
    /// <param name="resourcePath">The resource path to the terrain layer.</param>
    /// <param name="minHeight">The minimum height for this mapping.</param>
    /// <param name="maxHeight">The maximum height for this mapping.</param>
    /// <returns>A new texture mapping for the specified terrain layer and height range.</returns>
    private TerrainGenerationSettings.TerrainTextureMapping CreateMapping(string resourcePath, float minHeight, float maxHeight)
    {
        var terrainLayer = Resources.Load<TerrainLayer>(resourcePath);
        if (terrainLayer == null)
        {
            Debug.LogWarning($"Terrain layer not found at {resourcePath}");
        }

        return new TerrainGenerationSettings.TerrainTextureMapping
        {
            terrainLayer = terrainLayer,
            minHeight = minHeight,
            maxHeight = maxHeight
        };
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
    /// Ensures each Voronoi segment corresponds to its biome and applies all layers.
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

                if (biomeIndex >= 0 && biomeIndex < settings.biomes.Length)
                {
                    // Get the thresholds for the current biome
                    var thresholds = settings.biomes[biomeIndex].thresholds;

                    // Assign weights for each layer of the current biome
                    AssignLayerWeights(splatmap, x, y, layers, thresholds, height);
                }
            }
        }

        NormalizeSplatmap(splatmap);
        terrainData.SetAlphamaps(0, 0, splatmap);
        Debug.Log("Textures successfully applied with Voronoi biomes.");
    }


    /// <summary>
    /// Assigns weights to the splatmap based on biome thresholds and the height value.
    /// </summary>
    /// <param name="splatmap">The splatmap to modify.</param>
    /// <param name="x">The X coordinate in the splatmap.</param>
    /// <param name="y">The Y coordinate in the splatmap.</param>
    /// <param name="layers">Array of terrain layers.</param>
    /// <param name="thresholds">The biome thresholds for the current biome.</param>
    /// <param name="height">The height value at the current position.</param>
    private void AssignLayerWeights(
        float[,,] splatmap,
        int x,
        int y,
        TerrainLayer[] layers,
        TerrainGenerationSettings.BiomeThresholds thresholds,
        float height)
    {
        AssignWeight(splatmap, x, y, layers, thresholds.layer1, thresholds.minHeight1, thresholds.maxHeight1, height);
        AssignWeight(splatmap, x, y, layers, thresholds.layer2, thresholds.minHeight2, thresholds.maxHeight2, height);
        AssignWeight(splatmap, x, y, layers, thresholds.layer3, thresholds.minHeight3, thresholds.maxHeight3, height);
    }


    /// <summary>
    /// Assigns a weight to a specific terrain layer based on the height and priority factor.
    /// </summary>
    /// <param name="splatmap">The splatmap to modify.</param>
    /// <param name="x">The X coordinate in the splatmap.</param>
    /// <param name="y">The Y coordinate in the splatmap.</param>
    /// <param name="layers">Array of terrain layers.</param>
    /// <param name="layer">The specific terrain layer to assign weight to.</param>
    /// <param name="minHeight">The minimum height for this layer.</param>
    /// <param name="maxHeight">The maximum height for this layer.</param>
    /// <param name="height">The height value at the current position.</param>
    /// <param name="priorityFactor">Priority multiplier for the weight.</param>
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
            if (layerIndex >= 0 && layerIndex < splatmap.GetLength(2)) // Bounds check
            {
                float weight = CalculateWeight(height, minHeight, maxHeight);
                splatmap[x, y, layerIndex] += weight;
            }
        }
    }


    /// <summary>
    /// Calculates the weight for a terrain layer based on the height and its range.
    /// </summary>
    /// <param name="height">The height value at the current position.</param>
    /// <param name="minHeight">The minimum height for this layer.</param>
    /// <param name="maxHeight">The maximum height for this layer.</param>
    /// <returns>A normalized weight value between 0 and 1.</returns>
    private float CalculateWeight(float height, float minHeight, float maxHeight)
    {
        float range = maxHeight - minHeight;
        float center = minHeight + range / 2f;

        // Higher weight closer to the center of the range
        return Mathf.Clamp01(1f - Mathf.Abs(height - center) / (range / 2f));
    }

    #endregion

}

