using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Terrain/GenerationSettings")]
/// <summary>
/// Represents settings for terrain generation, including noise, displacement, biomes, and material mappings.
/// </summary>
public class TerrainGenerationSettings : ScriptableObject
{
    #region Perlin Noise Settings

    [Header("Perlin Noise")]
    [Tooltip("Enable or disable Perlin noise for terrain generation.")]
    public bool usePerlinNoise;

    [Tooltip("Number of Perlin noise layers to apply.")]
    public int perlinLayers;

    [Tooltip("Base scale of the Perlin noise.")]
    public float perlinBaseScale;

    [Tooltip("Amplitude decay factor for each layer of Perlin noise.")]
    public float perlinAmplitudeDecay;

    [Tooltip("Frequency growth factor for each layer of Perlin noise.")]
    public float perlinFrequencyGrowth;

    [Tooltip("Offset to apply to the Perlin noise.")]
    public Vector2 perlinOffset;

    #endregion

    #region Fractal Brownian Motion Settings

    [Header("Fractal Brownian Motion")]
    [Tooltip("Enable or disable Fractal Brownian Motion (fBm) for terrain generation.")]
    public bool useFractalBrownianMotion;

    [Tooltip("Number of fBm layers to apply.")]
    public int fBmLayers;

    [Tooltip("Base scale of the fBm noise.")]
    public float fBmBaseScale;

    [Tooltip("Amplitude decay factor for each layer of fBm.")]
    public float fBmAmplitudeDecay;

    [Tooltip("Frequency growth factor for each layer of fBm.")]
    public float fBmFrequencyGrowth;

    [Tooltip("Offset to apply to the fBm noise.")]
    public Vector2 fBmOffset;

    #endregion

    #region Midpoint Displacement Settings

    [Header("Midpoint Displacement")]
    [Tooltip("Enable or disable Midpoint Displacement for terrain generation.")]
    public bool useMidPointDisplacement;

    [Tooltip("Displacement factor for Midpoint Displacement.")]
    public float displacementFactor;

    [Tooltip("Decay rate for displacement over iterations.")]
    public float displacementDecayRate;

    [Tooltip("Seed for the random number generator used in Midpoint Displacement.")]
    public int randomSeed;

    #endregion

    #region Voronoi Biomes Settings

    [Header("Voronoi Biomes")]
    [Tooltip("Enable or disable Voronoi Biomes for terrain generation.")]
    public bool useVoronoiBiomes;

    [Tooltip("Number of Voronoi cells to generate.")]
    public int voronoiCellCount = 10;

    [Tooltip("Height range for Voronoi influence.")]
    public Vector2 voronoiHeightRange = new Vector2(0.1f, 0.9f);

    [Tooltip("Falloff curve to apply to Voronoi biomes.")]
    public AnimationCurve voronoiFalloffCurve = AnimationCurve.Linear(0, 1, 1, 0);

    [Tooltip("Distribution mode for Voronoi points.")]
    public DistributionMode voronoiDistributionMode;

    [Tooltip("Custom Voronoi points for terrain generation.")]
    public List<Vector2> customVoronoiPoints = new List<Vector2>();

    /// <summary>
    /// Enum representing distribution modes for Voronoi points.
    /// </summary>
    public enum DistributionMode
    {
        Random,
        Grid,
        Custom
    }

    #endregion

    #region Material Mapping Settings

    [Header("Texture Mappings")]
    public TerrainTextureMapping[] textureMappings;

    /// <summary>
    /// Struct representing a single material mapping based on height thresholds.
    /// </summary>
    [System.Serializable]
    public struct TerrainTextureMapping
    {
        public TerrainLayer terrainLayer; // Reference to a TerrainLayer
        public float minHeight;          // Minimum height threshold
        public float maxHeight;          // Maximum height threshold
    }

    [Header("Lake Settings")]
    [Tooltip("Enable or disable lake generation.")]
    public bool useLakes;

    [Tooltip("Height level for the lake.")]
    public float lakeHeight = 0.1f;

    #region River Settings
    [Header("River Settings")]
    [Tooltip("Enable or disable river generation.")]
    public bool useRivers;

    [Tooltip("Width of the river.")]
    public float riverWidth = 5.0f;

    [Tooltip("Intensity of river carving.")]
    public float riverIntensity = 0.5f;
    #endregion

    #region Trail Settings
    [Header("Trail Settings")]
    [Tooltip("Enable or disable trail generation.")]
    public bool useTrails;

    [Tooltip("Width of the trails.")]
    public float trailWidth = 3.0f;

    [Tooltip("Intensity of trail carving.")]
    public float trailIntensity = 0.3f;

    [Tooltip("Number of control points for trail curves.")]
    public int trailResolution = 10;
    #endregion


    #endregion
}
