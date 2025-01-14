using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// Concrete implementation of ITerrainGenerator that applies height modifiers and feature modifiers
/// (e.g., trails, lakes, rivers) to generate a final 2D heightmap.
/// </summary>
public class TerrainGenerator : ITerrainGenerator
{
    private readonly TerrainGenerationSettings settings;
    private readonly List<IHeightModifier> heightModifiers;
    private readonly List<IFeatureModifier> featureModifiers;

    // Optional: store biome indices if needed
    public NativeArray<int> BiomeIndices { get; private set; }

    /// <summary>
    /// Constructs a TerrainGenerator with the given settings and optional lists of modifiers.
    /// If no modifiers are provided, factories will create them based on the settings.
    /// </summary>
    /// <param name="settings">TerrainGenerationSettings to use.</param>
    /// <param name="heightModifiers">List of height-modifying jobs (like Perlin, fBm, midpoint, etc.).</param>
    /// <param name="featureModifiers">List of feature modifiers (like trails, lakes, etc.).</param>
    public TerrainGenerator(
        TerrainGenerationSettings settings,
        List<IHeightModifier> heightModifiers = null,
        List<IFeatureModifier> featureModifiers = null)
    {
        this.settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
        this.heightModifiers = heightModifiers ?? HeightModifierFactory.CreateModifiers(settings);
        this.featureModifiers = featureModifiers ?? FeatureModifierFactory.CreateModifiers(settings);

        Debug.Log($"TerrainGenerator initialized with " +
                  $"{this.heightModifiers.Count} height modifiers and " +
                  $"{this.featureModifiers.Count} feature modifiers.");
    }

    /// <summary>
    /// Generates a 2D height array (range [0..1]) by scheduling jobs for each
    /// height/feature modifier and then applying them in sequence.
    /// </summary>
    /// <param name="width">Width of the terrain heightmap.</param>
    /// <param name="length">Length of the terrain heightmap.</param>
    /// <returns>2D float array of normalized heights.</returns>
    public float[,] GenerateHeights(int width, int length)
    {
        // 1) Create a NativeArray to hold the final heights for all pixels.
        NativeArray<float> heightsNative = new NativeArray<float>(width * length, Allocator.TempJob);

        // 2) Schedule height modifiers and feature modifiers.
        JobHandle dependency = default;

        // Apply "standard" height modifiers (e.g., Perlin, fBm, midpoint, etc.).
        dependency = ApplyHeightModifiers(heightsNative, width, length, dependency);

        // Apply feature modifiers (e.g., trails, rivers, lakes), but only if they are enabled in settings.
        dependency = ApplyFeatureModifiers(heightsNative, width, length, dependency);

        // 3) Wait for jobs to finish so we can read back the data.
        dependency.Complete();

        // 4) Convert from NativeArray<float> to a 2D float[,] array.
        float[,] heights2D = ConvertNativeArrayToManaged(heightsNative, width, length);

        // Cleanup the NativeArray
        heightsNative.Dispose();

        // 5) Clamp/normalize the final height range to [0..1].
        ClampAndNormalizeHeights(heights2D);

        return heights2D;
    }

    /// <summary>
    /// Applies all standard height modifiers (Perlin, fBm, etc.) in sequence.
    /// </summary>
    private JobHandle ApplyHeightModifiers(NativeArray<float> heightsNative,
                                           int width,
                                           int length,
                                           JobHandle dependency)
    {
        foreach (var modifier in heightModifiers)
        {
            // Each modifier should implement .ScheduleJob(...)
            // e.g. PerlinNoiseModifier, FractalBrownianMotionModifier, MidpointDisplacementModifier
            dependency = modifier.ScheduleJob(heightsNative, width, length, settings, dependency);
        }
        return dependency;
    }

    /// <summary>
    /// Applies feature-based modifiers (e.g., trails, lakes) if they are enabled in settings.
    /// This is where we check 'useTrails', 'useLakes', etc.
    /// </summary>
    private JobHandle ApplyFeatureModifiers(NativeArray<float> heightsNative,
                                            int width,
                                            int length,
                                            JobHandle dependency)
    {
        foreach (var feature in featureModifiers)
        {
            // Example check for the TrailModifier
            if (feature is TrailModifier trailModifier)
            {
                // Only schedule the trail carving if 'useTrails' is true
                if (settings.useTrails)
                {
                    dependency = trailModifier.ScheduleJob(heightsNative, width, length, settings, dependency);
                }
            }
            else if (feature is LakeModifier lakeModifier)
            {
                // Only carve lakes if 'useLakes' is true
                if (settings.useLakes)
                {
                    dependency = lakeModifier.ScheduleJob(heightsNative, width, length, settings, dependency);
                }
            }
            // Example for other feature modifiers...
            // else if (feature is RiverModifier riverModifier)
            // {
            //     if (settings.useRivers)
            //     {
            //         dependency = riverModifier.ScheduleJob(...);
            //     }
            // }
        }

        return dependency;
    }

    /// <summary>
    /// Converts the flattened NativeArray to a 2D managed float array so that Unity can set the final terrain data.
    /// </summary>
    private float[,] ConvertNativeArrayToManaged(NativeArray<float> nativeHeights, int width, int length)
    {
        float[,] result = new float[width, length];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                int index = x + y * width;
                result[x, y] = nativeHeights[index];
            }
        }
        return result;
    }

    /// <summary>
    /// Finds min and max of the generated heights, then normalizes to [0..1].
    /// </summary>
    private void ClampAndNormalizeHeights(float[,] heights)
    {
        float minH = float.MaxValue;
        float maxH = float.MinValue;

        int width = heights.GetLength(0);
        int length = heights.GetLength(1);

        // Find min and max
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                float val = heights[x, y];
                if (val < minH) minH = val;
                if (val > maxH) maxH = val;
            }
        }

        float range = maxH - minH;
        if (range < 1e-5f) range = 1f; // avoid division by zero

        // Normalize to [0..1]
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                float normalized = (heights[x, y] - minH) / range;
                heights[x, y] = Mathf.Clamp01(normalized);
            }
        }
    }
}
