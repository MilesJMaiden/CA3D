using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Terrain/GenerationSettings")]
/// <summary>
/// Represents settings for terrain generation, including noise, displacement, biomes, lakes, rivers, and trails.
/// </summary>
public class TerrainGenerationSettings : ScriptableObject
{
    #region Perlin Noise Settings

    [Header("Perlin Noise")]
    public bool usePerlinNoise;
    public int perlinLayers;
    public float perlinBaseScale;
    public float perlinAmplitudeDecay;
    public float perlinFrequencyGrowth;
    public Vector2 perlinOffset;

    #endregion

    #region Fractal Brownian Motion Settings

    [Header("Fractal Brownian Motion")]
    public bool useFractalBrownianMotion;
    public int fBmLayers;
    public float fBmBaseScale;
    public float fBmAmplitudeDecay;
    public float fBmFrequencyGrowth;
    public Vector2 fBmOffset;

    #endregion

    #region Midpoint Displacement Settings

    [Header("Midpoint Displacement")]
    public bool useMidPointDisplacement;
    public float displacementFactor;
    public float displacementDecayRate;
    public int randomSeed;

    #endregion

    #region Voronoi Biomes Settings

    [Header("Voronoi Biomes")]
    public bool useVoronoiBiomes;
    public int voronoiCellCount = 10;
    public Vector2 voronoiHeightRange = new Vector2(0.1f, 0.9f);
    public AnimationCurve voronoiFalloffCurve = AnimationCurve.Linear(0, 1, 1, 0);
    public DistributionMode voronoiDistributionMode;
    public List<Vector2> customVoronoiPoints = new List<Vector2>();

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

    [System.Serializable]
    public struct TerrainTextureMapping
    {
        public TerrainLayer terrainLayer;
        public float minHeight;
        public float maxHeight;
    }

    #endregion

    #region Lake Settings

    [Header("Lake Settings")]
    public bool useLakes;
    public Vector2 lakeCenter = new Vector2(0.5f, 0.5f);
    public float lakeRadius = 10f;
    public float lakeWaterLevel = 0.3f;

    #endregion

    #region River Settings

    [Header("River Settings")]
    public bool useRivers;
    public float riverHeight = 0.1f;
    public float riverWidth = 5f;

    #endregion

    #region Trail Settings

    [Header("Trail Settings")]
    public bool useTrails;
    public Vector2 trailStartPoint = new Vector2(0.2f, 0.8f);
    public Vector2 trailEndPoint = new Vector2(0.8f, 0.2f);
    public float trailWidth = 2f;
    public float trailRandomness = 0.2f;



    #endregion

    [Header("Erosion Settings")]
    public bool useErosion;
    public float talusAngle = 0.05f; // Slope threshold
    public int erosionIterations = 3;
}
