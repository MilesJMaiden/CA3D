using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
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

            // Sync layers based on all biomes
            SyncTerrainLayersWithBiomes();
        }
        else
        {
            if (terrainSettings.textureMappings == null || terrainSettings.textureMappings.Length == 0)
            {
                Debug.LogWarning("No texture mappings defined. Assigning default texture mappings.");
                AssignDefaultTextures();
            }

            // Sync layers based on fallback mappings
            SyncTerrainLayersWithMappings();
        }

        // Configure terrain dimensions
        m_TerrainData.heightmapResolution = width + 1;
        m_TerrainData.size = new Vector3(width, height, length);

        // Generate the heightmap
        float[,] heights = terrainGenerator.GenerateHeights(width, length);
        if (heights == null)
        {
            Debug.LogError("Generated heightmap is null. Aborting terrain generation.");
            return;
        }

        // Set the Unity Terrain's heightmap
        m_TerrainData.SetHeights(0, 0, heights);

        // If Voronoi is enabled, use the job to compute "biomeIndices" per pixel.
        if (terrainSettings.useVoronoiBiomes)
        {
            // 1) Create the NativeArrays (biomeIndices, terrainLayerIndices)
            //    by scheduling our VoronoiBiomeJob
            NativeArray<int> biomeIndices;
            NativeArray<int> terrainLayerIndices;
            (biomeIndices, terrainLayerIndices) =
                ComputeVoronoiIndicesWithJob(heights, width, length);

            if (!biomeIndices.IsCreated || !terrainLayerIndices.IsCreated)
            {
                Debug.LogError("Failed to create or populate biome/terrainLayer indices from Voronoi job.");
                return;
            }

            // 2) Use the final indices to apply the correct biome-based textures
            ApplyTexturesWithBiomes(heights, biomeIndices, terrainLayerIndices,
                                    terrainSettings, width, length, m_TerrainData);

            // 3) Dispose
            biomeIndices.Dispose();
            terrainLayerIndices.Dispose();
        }
        else
        {
            // Regular texture mapping
            ApplyTextures(heights, terrainSettings, width, length, m_TerrainData);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Initializes the Terrain and TerrainData components.
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

        // Optionally, match width/length to the alphamap resolution if needed
        int alphamapResolution = m_TerrainData.alphamapResolution;
        width = alphamapResolution;
        length = alphamapResolution;
    }

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
    /// Schedules the VoronoiBiomeJob to compute "biomeIndices" (which cell each pixel belongs to)
    /// and a single dominant "terrainLayerIndices" if you want to incorporate second-nearest blending.
    /// </summary>
    private (NativeArray<int> biomeIndices, NativeArray<int> terrainLayerIndices)
        ComputeVoronoiIndicesWithJob(float[,] heights2D, int w, int l)
    {
        int totalPixels = w * l;

        // Flatten the 2D heights into a NativeArray
        NativeArray<float> heightsNative = new NativeArray<float>(totalPixels, Allocator.TempJob);
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < l; y++)
            {
                heightsNative[x + y * w] = heights2D[x, y];
            }
        }

        // Arrays for the results
        NativeArray<int> biomeIndices = new NativeArray<int>(totalPixels, Allocator.TempJob);
        NativeArray<int> layerIndices = new NativeArray<int>(totalPixels, Allocator.TempJob);

        try
        {
            // We'll build the Voronoi points & threshold arrays ourselves.
            // (This is basically a smaller version of your VoronoiBiomesModifier logic.)

            int cellCount = Math.Min(terrainSettings.voronoiCellCount,
                                     terrainSettings.biomes.Length);

            // Create Voronoi center points
            NativeArray<float2> voronoiPoints = new NativeArray<float2>(cellCount, Allocator.TempJob);
            switch (terrainSettings.voronoiDistributionMode)
            {
                case TerrainGenerationSettings.DistributionMode.Grid:
                    {
                        int gridSize = (int)Mathf.Ceil(Mathf.Sqrt(cellCount));
                        float cellW = w / (float)gridSize;
                        float cellH = l / (float)gridSize;
                        int i = 0;
                        for (int gx = 0; gx < gridSize; gx++)
                        {
                            for (int gy = 0; gy < gridSize; gy++)
                            {
                                if (i >= cellCount) break;
                                voronoiPoints[i++] = new Unity.Mathematics.float2(
                                    (gx + 0.5f) * cellW,
                                    (gy + 0.5f) * cellH
                                );
                            }
                        }
                        break;
                    }
                case TerrainGenerationSettings.DistributionMode.Random:
                    {
                        Unity.Mathematics.Random rng = new Unity.Mathematics.Random((uint)terrainSettings.randomSeed);
                        for (int i = 0; i < cellCount; i++)
                        {
                            voronoiPoints[i] = new Unity.Mathematics.float2(
                                rng.NextFloat(0, w),
                                rng.NextFloat(0, l)
                            );
                        }
                        break;
                    }
                default:
                    Debug.LogWarning($"Unsupported distribution mode: {terrainSettings.voronoiDistributionMode}");
                    break;
            }

            // Build the biome-threshold data
            NativeArray<Unity.Mathematics.float3x3> biomeThresholds =
                new NativeArray<Unity.Mathematics.float3x3>(terrainSettings.biomes.Length, Allocator.TempJob);

            for (int i = 0; i < terrainSettings.biomes.Length; i++)
            {
                var b = terrainSettings.biomes[i].thresholds;
                biomeThresholds[i] = new Unity.Mathematics.float3x3(
                    new Unity.Mathematics.float3(b.minHeight1, b.maxHeight1, 0),
                    new Unity.Mathematics.float3(b.minHeight2, b.maxHeight2, 0),
                    new Unity.Mathematics.float3(b.minHeight3, b.maxHeight3, 0)
                );
            }

            // Setup and schedule the Voronoi job
            var job = new VoronoiBiomeJob
            {
                width = w,
                length = l,
                voronoiPoints = voronoiPoints,
                biomeThresholds = biomeThresholds,
                biomeIndices = biomeIndices,
                terrainLayerIndices = layerIndices,
                heights = heightsNative
            };

            var handle = job.Schedule(totalPixels, 64);
            handle.Complete();

            // Dispose intermediate arrays
            voronoiPoints.Dispose();
            biomeThresholds.Dispose();

            // Return the arrays for later usage in ApplyTexturesWithBiomes.
            return (biomeIndices, layerIndices);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in ComputeVoronoiIndicesWithJob: {ex.Message}");
            if (biomeIndices.IsCreated) biomeIndices.Dispose();
            if (layerIndices.IsCreated) layerIndices.Dispose();
            heightsNative.Dispose();
            return (default, default);
        }
        finally
        {
            // We no longer need heightsNative after the job
            if (heightsNative.IsCreated) heightsNative.Dispose();
        }
    }

    /// <summary>
    /// Applies the terrain layers from the current texture mappings to the TerrainData.
    /// (Restores the method that was removed.)
    /// </summary>
    public void ApplyTerrainLayers()
    {
        // If we have no valid settings or texture mappings, skip
        if (terrainSettings == null || terrainSettings.textureMappings == null || terrainSettings.textureMappings.Length == 0)
        {
            Debug.LogWarning("No valid texture mappings available to apply. Skipping layer application.");
            return;
        }

        // Convert the texture mappings into an array of TerrainLayer objects
        TerrainLayer[] layers = GetTerrainLayers(terrainSettings.textureMappings);
        if (layers == null || layers.Length == 0)
        {
            Debug.LogError("Failed to retrieve valid TerrainLayers. Cannot apply layers.");
            return;
        }

        // Assign them to our TerrainData
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
                Debug.LogError(
                    $"Missing TerrainLayer in texture mapping with range {mapping.minHeight}-{mapping.maxHeight}. " +
                    "Ensure all mappings have valid TerrainLayers."
                );
            }
        }

        return layers.ToArray();
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

        // For each pixel, pick the biome from biomeIndices
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                int idx = x + y * width;
                int bIndex = biomeIndices[idx]; // which Voronoi cell we belong to
                float hVal = heights[x, y];

                if (bIndex >= 0 && bIndex < settings.biomes.Length)
                {
                    // The thresholds for this biome
                    var thresholds = settings.biomes[bIndex].thresholds;

                    // Apply up to 3 layers from that biome if hVal is in [min, max]
                    AssignLayerWeights(splatmap, x, y, layers, thresholds, hVal);
                }
            }
        }

        // Normalize so each pixel sums to 1.0 across layers
        NormalizeSplatmap(splatmap);

        // Apply the result
        terrainData.SetAlphamaps(0, 0, splatmap);
        Debug.Log("Textures successfully applied with Voronoi biomes.");
    }

    /// <summary>
    /// Assigns weights to the splatmap based on the given biome's threshold ranges.
    /// </summary>
    private void AssignLayerWeights(
        float[,,] splatmap,
        int x,
        int y,
        TerrainLayer[] layers,
        TerrainGenerationSettings.BiomeThresholds thresholds,
        float hVal)
    {
        AddWeightIfInRange(splatmap, x, y, layers, thresholds.layer1, thresholds.minHeight1, thresholds.maxHeight1, hVal);
        AddWeightIfInRange(splatmap, x, y, layers, thresholds.layer2, thresholds.minHeight2, thresholds.maxHeight2, hVal);
        AddWeightIfInRange(splatmap, x, y, layers, thresholds.layer3, thresholds.minHeight3, thresholds.maxHeight3, hVal);
    }

    /// <summary>
    /// If hVal is between minH and maxH, add a weight to that layer index.
    /// </summary>
    private void AddWeightIfInRange(
        float[,,] splatmap,
        int x,
        int y,
        TerrainLayer[] layers,
        TerrainLayer layer,
        float minH,
        float maxH,
        float hVal)
    {
        if (layer == null) return;
        if (hVal >= minH && hVal <= maxH)
        {
            int layerIdx = Array.IndexOf(layers, layer);
            if (layerIdx >= 0 && layerIdx < splatmap.GetLength(2))
            {
                // You can do "splatmap[x, y, layerIdx] += 1f;" or a more nuanced approach
                splatmap[x, y, layerIdx] += 1f;
            }
        }
    }

    /// <summary>
    /// Normal "non-Voronoi" texture application (if Voronoi is disabled).
    /// </summary>
    private void ApplyTextures(
        float[,] heights,
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

        if (settings.textureMappings == null || settings.textureMappings.Length != layers.Length)
        {
            Debug.LogWarning($"Mismatch between terrain layers ({layers.Length}) and texture mappings ({settings.textureMappings?.Length ?? 0}). Synchronizing layers...");
            SyncTerrainLayersWithMappings();
            layers = terrainData.terrainLayers; // Refresh layers after synchronization
        }

        int layerCount = layers.Length;
        float[,,] splatmap = new float[width, length, layerCount];

        // Example approach: blend by height and slope
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                float hVal = heights[x, y];
                float slope = Mathf.Abs(terrainData.GetSteepness(
                    x / (float)width,
                    y / (float)length) / 90f);

                for (int i = 0; i < layerCount; i++)
                {
                    var mapping = settings.textureMappings[i];
                    float heightBlend = Mathf.InverseLerp(mapping.minHeight, mapping.maxHeight, hVal);
                    float slopeBlend = Mathf.InverseLerp(0f, 1f, slope);

                    // Combine
                    splatmap[x, y, i] = heightBlend * (1f - slopeBlend);
                }
            }
        }

        NormalizeSplatmap(splatmap);
        terrainData.SetAlphamaps(0, 0, splatmap);
        Debug.Log("Textures successfully applied (non-Voronoi).");
    }

    /// <summary>
    /// Calculates and enforces that each pixel's total weights sum to 1.0.
    /// </summary>
    private void NormalizeSplatmap(float[,,] splatmap)
    {
        int w = splatmap.GetLength(0);
        int h = splatmap.GetLength(1);
        int layers = splatmap.GetLength(2);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float sum = 0f;
                for (int l = 0; l < layers; l++)
                {
                    sum += splatmap[x, y, l];
                }

                if (sum > 0.0001f)
                {
                    for (int l = 0; l < layers; l++)
                    {
                        splatmap[x, y, l] /= sum;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Creates a dictionary to map terrain layers to their indices for efficient lookups.
    /// </summary>
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
    private void AddDefaultBiomes(TerrainGenerationSettings settings)
    {
        settings.biomes = new[]
        {
            CreateBiome("Grassland",  "DefaultGrassLayer",       0f,  0.3f,
                                       "DefaultGrassLayerDense", 0.3f,0.6f,
                                       "DefaultMeadowLayer",     0.6f,0.9f),
            CreateBiome("Rocky",      "DefaultRockLayer",        0f,  0.4f,
                                       "DefaultRockLayerDense",  0.4f,0.7f,
                                       "DefaultCliffLayer",      0.7f,1.0f),
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
            CreateMapping("DefaultRockLayer",  0.3f, 0.7f),
            CreateMapping("DefaultSnowLayer",  0.7f, 1.0f)
        };

        terrainSettings.textureMappings = defaultMappings.ToArray();
        Debug.Log("Default texture mappings assigned.");
    }

    /// <summary>
    /// Creates a texture mapping for a terrain layer.
    /// </summary>
    private TerrainGenerationSettings.TerrainTextureMapping CreateMapping(string resourcePath, float minHeight, float maxHeight)
    {
        var terrainLayer = Resources.Load<TerrainLayer>(resourcePath);
        if (terrainLayer == null)
            Debug.LogWarning($"Terrain layer not found at {resourcePath}");

        return new TerrainGenerationSettings.TerrainTextureMapping
        {
            terrainLayer = terrainLayer,
            minHeight = minHeight,
            maxHeight = maxHeight
        };
    }

    /// <summary>
    /// Normal "non-Voronoi" texture application (if Voronoi is disabled).
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

        m_TerrainData.terrainLayers = layers.Distinct().ToArray();
        CachedLayerMappings = CacheLayerMappings(m_TerrainData.terrainLayers);

        Debug.Log($"Synchronized {m_TerrainData.terrainLayers.Length} terrain layers from {terrainSettings.biomes.Length} biomes.");
    }

    /// <summary>
    /// Initializes the terrain generator based on the current settings.
    /// </summary>
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
    private int AdjustToPowerOfTwoPlusOne(int value)
    {
        int power = Mathf.CeilToInt(Mathf.Log(value - 1, 2));
        return (int)Mathf.Pow(2, power) + 1;
    }

    /// <summary>
    /// Reports an error message to the Unity console and optionally to the UI manager.
    /// </summary>
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
