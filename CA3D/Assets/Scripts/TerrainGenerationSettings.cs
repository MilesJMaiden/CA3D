using UnityEngine;

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
    public int voronoiCellCount;
    public Vector2 voronoiHeightRange;
    public AnimationCurve voronoiFalloffCurve;
}
