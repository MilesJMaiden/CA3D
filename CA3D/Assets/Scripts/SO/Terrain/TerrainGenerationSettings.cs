using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Terrain/GenerationSettings")]
/// <summary>
/// Represents settings for terrain generation, including noise, displacement, biomes, lakes, rivers, trails, erosion, and feature parameters.
/// </summary>
public class TerrainGenerationSettings : ScriptableObject
{
    [Header("Perlin Noise Settings")]
    public bool usePerlinNoise;
    public int perlinLayers = 1;
    public float perlinBaseScale = 10f;
    public float perlinAmplitudeDecay = 0.5f;
    public float perlinFrequencyGrowth = 2f;
    public Vector2 perlinOffset = Vector2.zero;

    [Header("Fractal Brownian Motion Settings")]
    public bool useFractalBrownianMotion;
    public int fBmLayers = 1;
    public float fBmBaseScale = 10f;
    public float fBmAmplitudeDecay = 0.5f;
    public float fBmFrequencyGrowth = 2f;
    public Vector2 fBmOffset = Vector2.zero;

    [Header("Midpoint Displacement Settings")]
    public bool useMidPointDisplacement;
    public float displacementFactor = 2f;
    public float displacementDecayRate = 0.5f;
    public int randomSeed = 42;
    public float roughness = 0.5f;
    public float seed = 1f;

    [Header("Voronoi Biomes Settings")]
    public bool useVoronoiBiomes;
    public int voronoiCellCount = 10;
    public float voronoiBlendFactor = 0.5f;
    public DistributionMode voronoiDistributionMode = DistributionMode.Random;
    public Biome[] biomes;
    public int BiomeCount => biomes?.Length ?? 0;

    [System.Serializable]
    public class Biome
    {
        public string name;
        public BiomeThresholds thresholds;
    }

    [System.Serializable]
    public struct BiomeThresholds
    {
        [Header("Layer 1 Settings")]
        public TerrainLayer layer1;
        public float minHeight1;
        public float maxHeight1;

        [Header("Layer 2 Settings")]
        public TerrainLayer layer2;
        public float minHeight2;
        public float maxHeight2;

        [Header("Layer 3 Settings")]
        public TerrainLayer layer3;
        public float minHeight3;
        public float maxHeight3;
    }

    public enum DistributionMode
    {
        Grid,
        Random,
        Custom
    }

    [Header("Lake Settings")]
    public bool useLakes;
    public Vector2 lakeCenter = new Vector2(0.5f, 0.5f);
    public float lakeRadius = 10f;
    public float lakeWaterLevel = 0.3f;

    [Header("River Settings")]
    public bool useRivers;
    public float riverHeight = 0.1f;
    public float riverWidth = 5f;

    [Header("Trail Settings")]
    public bool useTrails;
    public Vector2 trailStartPoint = new Vector2(0.2f, 0.8f);
    public Vector2 trailEndPoint = new Vector2(0.8f, 0.2f);
    public float trailWidth = 2f;
    public float trailRandomness = 0.2f;

    [Header("Erosion Settings")]
    public bool useErosion;
    public float talusAngle = 0.05f;
    public int erosionIterations = 3;

    [Header("Feature Settings")]
    public List<FeatureSettings> featureSettings;

    [Tooltip("Number of Cellular Automata iterations for feature placement.")]
    public int featureCAIterations = 2;

    [Tooltip("Neighbor threshold (Moore neighborhood) required to keep a feature cell alive.")]
    public int featureNeighborThreshold = 3;

    [Tooltip("Global multiplier applied to feature spawn probability.")]
    public float globalFeatureDensity = 1f;
}
