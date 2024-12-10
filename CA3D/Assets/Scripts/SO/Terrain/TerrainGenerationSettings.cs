using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Terrain/GenerationSettings")]
public class TerrainGenerationSettings : ScriptableObject
{
    [Header("Perlin Noise")]
    public bool usePerlinNoise;
    public int perlinLayers;
    public float perlinBaseScale;
    public float perlinAmplitudeDecay;
    public float perlinFrequencyGrowth;
    public Vector2 perlinOffset;

    [Header("Fractal Brownian Motion")]
    public bool useFractalBrownianMotion;
    public int fBmLayers;
    public float fBmBaseScale;
    public float fBmAmplitudeDecay;
    public float fBmFrequencyGrowth;
    public Vector2 fBmOffset;

    [Header("Midpoint Displacement")]
    public bool useMidPointDisplacement;
    public float displacementFactor;
    public float displacementDecayRate;
    public int randomSeed;

    [Header("Voronoi Biomes")]
    public bool useVoronoiBiomes;
    public int voronoiCellCount = 10;
    public Vector2 voronoiHeightRange = new Vector2(0.1f, 0.9f);
    public AnimationCurve voronoiFalloffCurve = AnimationCurve.Linear(0, 1, 1, 0); // Default curve
    public DistributionMode voronoiDistributionMode;
    public List<Vector2> customVoronoiPoints = new List<Vector2>(); // Initialize list

    // Enum for Voronoi Distribution Modes
    public enum DistributionMode
    {
        Random,
        Grid,
        Custom
    }
}
