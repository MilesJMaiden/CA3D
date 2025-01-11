using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Terrain/GenerationSettings")]
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
    public List<Biome> biomes = new List<Biome>();
    public int BiomeCount => biomes?.Count ?? 0;

    // Adding this Length property to match the existing references
    public int Length => biomes?.Count ?? 0;

    [System.Serializable]
    public class Biome
    {
        public string name;
        public BiomeThresholds thresholds;
    }

    [System.Serializable]
    public struct BiomeThresholds
    {
        public TerrainLayer layer1;
        public float minHeight1;
        public float maxHeight1;

        public TerrainLayer layer2;
        public float minHeight2;
        public float maxHeight2;

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
    public List<FeatureEntry> featureSettings = new List<FeatureEntry>();
    public int featureCAIterations = 2;
    public int featureNeighborThreshold = 3;
    public float globalFeatureDensity = 1f;

    [System.Serializable]
    public class FeatureEntry
    {
        public bool isEnabled;
        public FeatureSettings feature;
    }

    [Header("Agent Settings")]
    public bool enableAgents;
    public List<AgentEntry> agentSettings = new List<AgentEntry>();

    [System.Serializable]
    public class AgentEntry
    {
        public bool isEnabled;
        public GameObject agentPrefab;
        public string agentName;
        public List<AgentBehaviorModifier> behaviorModifiers = new List<AgentBehaviorModifier>();
    }

    [System.Serializable]
    public class AgentBehaviorModifier
    {
        public string modifierName;
        public float intensity;
    }

    public void ApplyAgentBehaviorModifiers(GameObject agent, TerrainFeatureContext context)
    {
        foreach (var entry in agentSettings)
        {
            if (!entry.isEnabled) continue;

            var agentBehavior = agent.GetComponent<IAgentBehavior>();
            if (agentBehavior != null)
            {
                agentBehavior.ModifyBehavior(context, entry.behaviorModifiers);
            }
        }
    }
}
