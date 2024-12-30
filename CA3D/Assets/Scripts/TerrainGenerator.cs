using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class TerrainGenerator : ITerrainGenerator
{
    private readonly TerrainGenerationSettings settings;
    private readonly List<IHeightModifier> heightModifiers;

    public TerrainGenerator(TerrainGenerationSettings settings, List<IHeightModifier> heightModifiers = null)
    {
        this.settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
        this.heightModifiers = heightModifiers ?? HeightModifierFactory.CreateModifiers(settings);

        Debug.Log($"TerrainGenerator initialized with {this.heightModifiers.Count} modifiers");
    }

    public float[,] GenerateHeights(int width, int length)
    {
        NativeArray<float> heightsNative = new NativeArray<float>(width * length, Allocator.TempJob);
        JobHandle dependency = default; // Start with no dependency

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
                    dependency = voronoiModifier.ScheduleJob(heightsNative, width, length, settings, dependency);
                    break;
                default:
                    Debug.LogWarning($"Modifier {modifier.GetType()} does not support jobs.");
                    break;
            }
        }

        dependency.Complete(); // Ensure all jobs complete before accessing the NativeArray

        // Convert back to managed array
        float[,] heights = new float[width, length];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                heights[x, y] = heightsNative[x + y * width];
            }
        }

        heightsNative.Dispose();
        return heights;
    }

    private void ApplyBaseHeightModifiers(float[,] heights)
    {
        foreach (var modifier in heightModifiers)
        {
            modifier.ModifyHeight(heights, settings);
        }
    }

    private void ApplyFeatureModifiers(float[,] heights, int width, int length)
    {
        if (settings.useTrails)
        {
            Debug.Log("Applying Trail Modifier...");
            var trailModifier = new TrailModifier();
            trailModifier.ApplyFeature(heights, settings, new Vector2(0.2f, 0.3f), settings.trailIntensity, settings.trailWidth);
        }
        // Add more feature modifiers here if needed
    }

    private void NormalizeHeights(float[,] heights)
    {
        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;

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
            for (int x = 0; x < heights.GetLength(0); x++)
            {
                for (int y = 0; y < heights.GetLength(1); y++)
                {
                    heights[x, y] = (heights[x, y] - minHeight) / range;
                }
            }
        }
    }
}
