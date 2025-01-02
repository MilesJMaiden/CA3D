using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class TerrainGenerator : ITerrainGenerator
{
    private readonly TerrainGenerationSettings settings;
    private readonly List<IHeightModifier> heightModifiers;
    private readonly List<IFeatureModifier> featureModifiers;

    public NativeArray<int> BiomeIndices { get; private set; }

    public TerrainGenerator(
        TerrainGenerationSettings settings,
        List<IHeightModifier> heightModifiers = null,
        List<IFeatureModifier> featureModifiers = null)
    {
        this.settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
        this.heightModifiers = heightModifiers ?? HeightModifierFactory.CreateModifiers(settings);
        this.featureModifiers = featureModifiers ?? FeatureModifierFactory.CreateModifiers(settings);

        Debug.Log($"TerrainGenerator initialized with {this.heightModifiers.Count} height modifiers and {this.featureModifiers.Count} feature modifiers");
    }

    public float[,] GenerateHeights(int width, int length)
    {
        NativeArray<float> heightsNative = new NativeArray<float>(width * length, Allocator.TempJob);
        JobHandle dependency = default;

        // Apply height modifiers
        dependency = ApplyHeightModifiers(heightsNative, width, length, dependency);

        // Apply feature-specific modifiers
        dependency = ApplyFeatureModifiers(heightsNative, width, length, dependency);

        dependency.Complete();

        // Convert to managed array
        float[,] heights = ConvertNativeArrayToManaged(heightsNative, width, length);

        heightsNative.Dispose();
        ClampAndNormalizeHeights(heights);

        return heights;
    }

    private JobHandle ApplyHeightModifiers(NativeArray<float> heightsNative, int width, int length, JobHandle dependency)
    {
        NativeArray<int> biomeIndices = default;

        foreach (var modifier in heightModifiers)
        {
            switch (modifier)
            {
                case PerlinNoiseModifier perlinModifier:
                    dependency = perlinModifier.ScheduleJob(heightsNative, width, length, settings, dependency);
                    break;
                case FractalBrownianMotionModifier fBmModifier:
                    dependency = fBmModifier.ScheduleJob(heightsNative, width, length, settings, dependency);
                    break;
                case MidpointDisplacementModifier midpointModifier:
                    dependency = midpointModifier.ScheduleJob(heightsNative, width, length, settings, dependency);
                    break;
                case VoronoiBiomesModifier voronoiModifier:
                    dependency = voronoiModifier.ScheduleJob(
                        heightsNative, width, length, settings, dependency, out biomeIndices);
                    BiomeIndices = biomeIndices;
                    break;
                default:
                    Debug.LogWarning($"Modifier {modifier.GetType()} does not support jobs.");
                    break;
            }
        }
        return dependency;
    }

    private JobHandle ApplyFeatureModifiers(NativeArray<float> heightsNative, int width, int length, JobHandle dependency)
    {
        foreach (var feature in featureModifiers)
        {
            switch (feature)
            {
                case RiverModifier riverModifier when settings.useRivers:
                    dependency = riverModifier.ScheduleJob(heightsNative, width, length, settings, dependency);
                    break;
                case TrailModifier trailModifier when settings.useTrails:
                    dependency = trailModifier.ScheduleJob(heightsNative, width, length, settings, dependency);
                    break;
                case LakeModifier lakeModifier when settings.useLakes:
                    dependency = lakeModifier.ScheduleJob(heightsNative, width, length, settings, dependency);
                    break;
                case ThermalErosionModifier erosionModifier when settings.useErosion:
                    dependency = erosionModifier.ScheduleJob(heightsNative, width, length, settings, dependency);
                    break;
                default:
                    Debug.LogWarning($"Feature {feature.GetType()} does not support jobs.");
                    break;
            }
        }
        return dependency;
    }

    private float[,] ConvertNativeArrayToManaged(NativeArray<float> heightsNative, int width, int length)
    {
        float[,] heights = new float[width, length];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                heights[x, y] = heightsNative[x + y * width];
            }
        }
        return heights;
    }

    private void ClampAndNormalizeHeights(float[,] heights)
    {
        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;

        // Determine min and max heights
        for (int x = 0; x < heights.GetLength(0); x++)
        {
            for (int y = 0; y < heights.GetLength(1); y++)
            {
                float height = heights[x, y];
                minHeight = Mathf.Min(minHeight, height);
                maxHeight = Mathf.Max(maxHeight, height);
            }
        }

        float range = maxHeight - minHeight;
        if (range > Mathf.Epsilon)
        {
            // Normalize and clamp heights
            for (int x = 0; x < heights.GetLength(0); x++)
            {
                for (int y = 0; y < heights.GetLength(1); y++)
                {
                    heights[x, y] = Mathf.Clamp01((heights[x, y] - minHeight) / range);
                }
            }
        }
    }
}