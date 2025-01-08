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
        ValidateAndAdjustDimensions();

        if (!InitializeGenerator())
        {
            Debug.LogError("Terrain generator initialization failed. Aborting terrain generation.");
            return;
        }

        SyncTerrainLayersWithBiomes();

        if (m_TerrainData.terrainLayers == null || m_TerrainData.terrainLayers.Length == 0)
        {
            AssignDefaultTerrainLayers();
        }

        m_TerrainData.heightmapResolution = width + 1;
        m_TerrainData.size = new Vector3(width, height, length);

        float[,] heights = terrainGenerator.GenerateHeights(width, length);
        if (heights == null)
        {
            Debug.LogError("Generated heightmap is null. Aborting terrain generation.");
            return;
        }

        m_TerrainData.SetHeights(0, 0, heights);

        // Initialize NativeArrays with default values
        NativeArray<int> biomeIndices = default;
        NativeArray<int> terrainLayerIndices = default;

        try
        {
            // Assign values to NativeArrays by computing Voronoi indices
            (biomeIndices, terrainLayerIndices) = ComputeVoronoiIndicesWithJob(heights, width, length);

            // Apply textures using the computed indices
            GenerateAndApplySplatmap(heights, biomeIndices, terrainLayerIndices, width, length);
        }
        finally
        {
            // Ensure disposal of NativeArrays
            if (biomeIndices.IsCreated) biomeIndices.Dispose();
            if (terrainLayerIndices.IsCreated) terrainLayerIndices.Dispose();
        }
    }



    private void AssignDefaultTerrainLayers()
    {
        var defaultLayer = Resources.Load<TerrainLayer>("DefaultTerrainLayer"); // Replace with your default layer
        if (defaultLayer == null)
        {
            Debug.LogError("Default terrain layer not found. Please create a 'DefaultTerrainLayer' asset in Resources.");
            return;
        }

        m_TerrainData.terrainLayers = new TerrainLayer[] { defaultLayer };
        Debug.Log("Assigned default terrain layer.");
    }


    private void ApplyBiomeTerrainLayers(float[,] heights, int width, int length)
    {
        NativeArray<int> biomeIndices = default;
        NativeArray<int> terrainLayerIndices = default;

        try
        {
            // Compute Voronoi indices
            (biomeIndices, terrainLayerIndices) = ComputeVoronoiIndicesWithJob(heights, width, length);

            if (!biomeIndices.IsCreated || !terrainLayerIndices.IsCreated)
            {
                Debug.LogError("Failed to compute biome or terrain layer indices.");
                return;
            }

            // Generate and apply the splatmap
            GenerateAndApplySplatmap(heights, biomeIndices, terrainLayerIndices, width, length);
        }
        finally
        {
            // Dispose NativeArrays to avoid memory leaks
            if (biomeIndices.IsCreated) biomeIndices.Dispose();
            if (terrainLayerIndices.IsCreated) terrainLayerIndices.Dispose();
        }
    }

    private void GenerateAndApplySplatmap(
        float[,] heights,
        NativeArray<int> biomeIndices,
        NativeArray<int> terrainLayerIndices,
        int width,
        int length)
    {
        TerrainLayer[] layers = m_TerrainData.terrainLayers;
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
                float heightValue = heights[x, y];

                if (biomeIndex >= 0 && biomeIndex < terrainSettings.biomes.Length)
                {
                    var thresholds = terrainSettings.biomes[biomeIndex].thresholds;
                    AssignLayerWeights(splatmap, x, y, layers, thresholds, heightValue);
                }
            }
        }

        NormalizeSplatmap(splatmap);
        m_TerrainData.SetAlphamaps(0, 0, splatmap);
        Debug.Log("Splatmap applied with biome-based textures.");
    }



    private (NativeArray<int> biomeIndices, NativeArray<int> terrainLayerIndices)
    ComputeVoronoiIndicesWithJob(float[,] heights2D, int width, int length)
    {
        int totalPixels = width * length;

        NativeArray<float> heightsNative = new NativeArray<float>(totalPixels, Allocator.TempJob);
        NativeArray<int> biomeIndices = new NativeArray<int>(totalPixels, Allocator.TempJob);
        NativeArray<int> terrainLayerIndices = new NativeArray<int>(totalPixels, Allocator.TempJob);

        NativeArray<float2> voronoiPoints = default;
        NativeArray<float3x3> biomeThresholds = default;

        try
        {
            // Flatten the 2D heights array
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < length; y++)
                {
                    heightsNative[x + y * width] = heights2D[x, y];
                }
            }

            // Generate Voronoi points and thresholds
            voronoiPoints = GenerateVoronoiPoints(terrainSettings.voronoiCellCount, width, length);
            biomeThresholds = GenerateBiomeThresholds();

            // Configure and schedule the VoronoiBiomeJob
            var job = new VoronoiBiomeJob
            {
                width = width,
                length = length,
                voronoiPoints = voronoiPoints,
                biomeThresholds = biomeThresholds,
                biomeIndices = biomeIndices,
                terrainLayerIndices = terrainLayerIndices,
                heights = heightsNative
            };

            JobHandle handle = job.Schedule(totalPixels, 64);
            handle.Complete();

            return (biomeIndices, terrainLayerIndices);
        }
        finally
        {
            // Dispose temporary arrays
            if (heightsNative.IsCreated) heightsNative.Dispose();
            if (voronoiPoints.IsCreated) voronoiPoints.Dispose();
            if (biomeThresholds.IsCreated) biomeThresholds.Dispose();
        }
    }

    private void AssignLayerWeights(
        float[,,] splatmap,
        int x,
        int y,
        TerrainLayer[] layers,
        TerrainGenerationSettings.BiomeThresholds thresholds,
        float heightValue)
    {
        AddWeightIfInRange(splatmap, x, y, layers, thresholds.layer1, thresholds.minHeight1, thresholds.maxHeight1, heightValue);
        AddWeightIfInRange(splatmap, x, y, layers, thresholds.layer2, thresholds.minHeight2, thresholds.maxHeight2, heightValue);
        AddWeightIfInRange(splatmap, x, y, layers, thresholds.layer3, thresholds.minHeight3, thresholds.maxHeight3, heightValue);
    }

    private void AddWeightIfInRange(
        float[,,] splatmap,
        int x,
        int y,
        TerrainLayer[] layers,
        TerrainLayer layer,
        float minHeight,
        float maxHeight,
        float heightValue)
    {
        if (layer == null) return;

        if (heightValue >= minHeight && heightValue <= maxHeight)
        {
            int layerIndex = Array.IndexOf(layers, layer);
            if (layerIndex >= 0 && layerIndex < splatmap.GetLength(2))
            {
                splatmap[x, y, layerIndex] += 1f;
            }
        }
    }

    private void SyncTerrainLayersWithBiomes()
    {
        if (terrainSettings.biomes == null || terrainSettings.biomes.Length == 0)
        {
            Debug.LogError("No biomes are defined in terrain settings. Cannot synchronize terrain layers.");
            return;
        }

        List<TerrainLayer> layers = new List<TerrainLayer>();
        foreach (var biome in terrainSettings.biomes)
        {
            if (biome.thresholds.layer1 != null)
            {
                layers.Add(biome.thresholds.layer1);
                Debug.Log($"Added Layer 1 for biome {biome.name}: {biome.thresholds.layer1.name}");
            }
            if (biome.thresholds.layer2 != null)
            {
                layers.Add(biome.thresholds.layer2);
                Debug.Log($"Added Layer 2 for biome {biome.name}: {biome.thresholds.layer2.name}");
            }
            if (biome.thresholds.layer3 != null)
            {
                layers.Add(biome.thresholds.layer3);
                Debug.Log($"Added Layer 3 for biome {biome.name}: {biome.thresholds.layer3.name}");
            }
        }

        m_TerrainData.terrainLayers = layers.Distinct().ToArray();

        if (m_TerrainData.terrainLayers.Length == 0)
        {
            Debug.LogError("No valid terrain layers were collected from biomes.");
        }
        else
        {
            Debug.Log($"Synchronized {m_TerrainData.terrainLayers.Length} terrain layers.");
        }
    }


    private NativeArray<float2> GenerateVoronoiPoints(int cellCount, int width, int length)
    {
        NativeArray<float2> points = new NativeArray<float2>(cellCount, Allocator.TempJob);

        if (terrainSettings.voronoiDistributionMode == TerrainGenerationSettings.DistributionMode.Grid)
        {
            int gridSize = (int)math.sqrt(cellCount);
            float cellWidth = width / (float)gridSize;
            float cellHeight = length / (float)gridSize;

            for (int i = 0; i < cellCount; i++)
            {
                int x = i % gridSize;
                int y = i / gridSize;
                points[i] = new float2(
                    x * cellWidth + cellWidth * 0.5f,
                    y * cellHeight + cellHeight * 0.5f
                );
            }
        }
        else if (terrainSettings.voronoiDistributionMode == TerrainGenerationSettings.DistributionMode.Random)
        {
            Unity.Mathematics.Random rng = new Unity.Mathematics.Random((uint)terrainSettings.randomSeed);
            for (int i = 0; i < cellCount; i++)
            {
                points[i] = new float2(rng.NextFloat(0, width), rng.NextFloat(0, length));
            }
        }

        return points;
    }

    private NativeArray<float3x3> GenerateBiomeThresholds()
    {
        NativeArray<float3x3> thresholds = new NativeArray<float3x3>(terrainSettings.biomes.Length, Allocator.TempJob);

        for (int i = 0; i < terrainSettings.biomes.Length; i++)
        {
            var biome = terrainSettings.biomes[i];
            thresholds[i] = new float3x3(
                new float3(biome.thresholds.minHeight1, biome.thresholds.maxHeight1, 0),
                new float3(biome.thresholds.minHeight2, biome.thresholds.maxHeight2, 0),
                new float3(biome.thresholds.minHeight3, biome.thresholds.maxHeight3, 0)
            );
        }

        return thresholds;
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
