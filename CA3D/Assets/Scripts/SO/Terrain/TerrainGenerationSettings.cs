using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Terrain/GenerationSettings")]
/// <summary>
/// Represents settings for terrain generation, including noise, displacement, biomes, lakes, rivers, trails, and features.
/// </summary>
public class TerrainGenerationSettings : ScriptableObject
{
    [Header("Perlin Noise Settings")]
    [Tooltip("Enable or disable Perlin Noise for terrain generation.")]
    public bool usePerlinNoise;

    [Tooltip("Number of Perlin Noise layers.")]
    public int perlinLayers = 1;

    [Tooltip("Base scale for Perlin Noise.")]
    public float perlinBaseScale = 10f;

    [Tooltip("Amplitude decay for subsequent Perlin layers.")]
    public float perlinAmplitudeDecay = 0.5f;

    [Tooltip("Frequency growth for subsequent Perlin layers.")]
    public float perlinFrequencyGrowth = 2f;

    [Tooltip("Offset for Perlin Noise sampling.")]
    public Vector2 perlinOffset = Vector2.zero;

    [Header("Fractal Brownian Motion Settings")]
    [Tooltip("Enable or disable Fractal Brownian Motion for terrain generation.")]
    public bool useFractalBrownianMotion;

    [Tooltip("Number of fBm layers.")]
    public int fBmLayers = 1;

    [Tooltip("Base scale for fBm.")]
    public float fBmBaseScale = 10f;

    [Tooltip("Amplitude decay for subsequent fBm layers.")]
    public float fBmAmplitudeDecay = 0.5f;

    [Tooltip("Frequency growth for subsequent fBm layers.")]
    public float fBmFrequencyGrowth = 2f;

    [Tooltip("Offset for fBm sampling.")]
    public Vector2 fBmOffset = Vector2.zero;

    [Header("Midpoint Displacement Settings")]
    [Tooltip("Enable or disable Midpoint Displacement for terrain generation.")]
    public bool useMidPointDisplacement;

    [Tooltip("Initial displacement factor for Midpoint Displacement.")]
    public float displacementFactor = 2f;

    [Tooltip("Decay rate of displacement for Midpoint Displacement.")]
    public float displacementDecayRate = 0.5f;

    [Tooltip("Random seed for Midpoint Displacement.")]
    public int randomSeed = 42;

    [Tooltip("Overall roughness of the displacement.")]
    public float roughness = 0.5f;

    [Tooltip("Seed value for random generation.")]
    public float seed = 1f;

    [Header("Voronoi Biomes Settings")]
    [Tooltip("Enable or disable Voronoi Biomes for terrain generation.")]
    public bool useVoronoiBiomes;

    [Tooltip("Number of Voronoi cells to generate.")]
    public int voronoiCellCount = 10;

    [Tooltip("Height range for Voronoi cells.")]
    public Vector2 voronoiHeightRange = new Vector2(0.1f, 0.9f);

    [Tooltip("Falloff curve for Voronoi biomes.")]
    public AnimationCurve voronoiFalloffCurve = AnimationCurve.Linear(0, 1, 1, 0);

    [Tooltip("Distribution mode for Voronoi cells.")]
    public DistributionMode voronoiDistributionMode;

    [Tooltip("Custom points for Voronoi biomes (used in Custom mode).")]
    public List<Vector2> customVoronoiPoints = new List<Vector2>();

    [Tooltip("Blend factor for Voronoi biomes' influence.")]
    [Range(0f, 1f)]
    public float voronoiBlendFactor = 0.5f;

    public enum DistributionMode
    {
        Random,
        Grid,
        Custom
    }

    [Header("Texture Mappings")]
    [Tooltip("Texture mappings for terrain layers.")]
    public TerrainTextureMapping[] textureMappings;

    [System.Serializable]
    public struct TerrainTextureMapping
    {
        [Tooltip("Terrain layer associated with this mapping.")]
        public TerrainLayer terrainLayer;

        [Tooltip("Minimum height for this texture.")]
        public float minHeight;

        [Tooltip("Maximum height for this texture.")]
        public float maxHeight;
    }

    [Header("Lake Settings")]
    [Tooltip("Enable or disable lakes in terrain generation.")]
    public bool useLakes;

    [Tooltip("Center position for the lake.")]
    public Vector2 lakeCenter = new Vector2(0.5f, 0.5f);

    [Tooltip("Radius of the lake.")]
    public float lakeRadius = 10f;

    [Tooltip("Water level for the lake.")]
    public float lakeWaterLevel = 0.3f;

    [Header("River Settings")]
    [Tooltip("Enable or disable rivers in terrain generation.")]
    public bool useRivers;

    [Tooltip("Height level for the river.")]
    public float riverHeight = 0.1f;

    [Tooltip("Width of the river.")]
    public float riverWidth = 5f;

    [Header("Trail Settings")]
    [Tooltip("Enable or disable trails in terrain generation.")]
    public bool useTrails;

    [Tooltip("Starting point for the trail.")]
    public Vector2 trailStartPoint = new Vector2(0.2f, 0.8f);

    [Tooltip("Ending point for the trail.")]
    public Vector2 trailEndPoint = new Vector2(0.8f, 0.2f);

    [Tooltip("Width of the trail.")]
    public float trailWidth = 2f;

    [Tooltip("Randomness factor for trail generation.")]
    public float trailRandomness = 0.2f;

    [Header("Erosion Settings")]
    [Tooltip("Enable or disable thermal erosion.")]
    public bool useErosion;

    [Tooltip("Talus angle for erosion.")]
    public float talusAngle = 0.05f;

    [Tooltip("Number of erosion iterations.")]
    public int erosionIterations = 3;

    [Header("Feature Settings")]
    [Tooltip("Enable feature placement on the terrain.")]
    public bool useFeatures;

    [Tooltip("Global spawn probability for features.")]
    [Range(0f, 1f)]
    public float globalSpawnProbability = 0.5f;

    [Tooltip("Feature definitions for terrain.")]
    public List<FeatureSettings> featureSettings;
}
