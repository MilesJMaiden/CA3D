using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class VoronoiBiomesModifier : IHeightModifier
{
    public JobHandle ScheduleJob(
        NativeArray<float> heights,
        int width,
        int length,
        TerrainGenerationSettings settings,
        JobHandle dependency,
        out NativeArray<int> biomeIndices)
    {
        int sampleCount = 256;
        NativeArray<float> falloffSamples = new NativeArray<float>(sampleCount, Allocator.TempJob);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            falloffSamples[i] = settings.voronoiFalloffCurve.Evaluate(t);
        }

        NativeArray<float2> points = new NativeArray<float2>(settings.customVoronoiPoints.Count, Allocator.TempJob);
        for (int i = 0; i < settings.customVoronoiPoints.Count; i++)
        {
            points[i] = new float2(settings.customVoronoiPoints[i].x, settings.customVoronoiPoints[i].y);
        }

        biomeIndices = new NativeArray<int>(width * length, Allocator.TempJob);

        var job = new VoronoiBiomeJob
        {
            width = width,
            length = length,
            points = points,
            maxDistance = Mathf.Max(width, length),
            heightRange = new float2(settings.voronoiHeightRange.x, settings.voronoiHeightRange.y),
            falloffSamples = falloffSamples,
            sampleCount = sampleCount,
            biomeIndices = biomeIndices,
            heights = heights
        };

        JobHandle handle = job.Schedule(width * length, 64, dependency);

        points.Dispose(handle);
        falloffSamples.Dispose(handle);

        return handle;
    }


    public void ModifyHeight(float[,] heights, TerrainGenerationSettings settings)
    {
        int width = heights.GetLength(0);
        int length = heights.GetLength(1);

        // Process the Voronoi falloff curve directly on the CPU
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                float minDistSquared = float.MaxValue;

                // Calculate the closest Voronoi point
                foreach (var point in settings.customVoronoiPoints)
                {
                    float distSquared = (x - point.x) * (x - point.x) + (y - point.y) * (y - point.y);
                    minDistSquared = Mathf.Min(minDistSquared, distSquared);
                }

                // Normalize distance and apply the falloff curve
                float normalizedDistance = Mathf.Sqrt(minDistSquared) / Mathf.Max(width, length);
                float falloffValue = settings.voronoiFalloffCurve.Evaluate(normalizedDistance);

                // Apply the height modification
                heights[x, y] += Mathf.Lerp(settings.voronoiHeightRange.x, settings.voronoiHeightRange.y, falloffValue);
            }
        }
    }

    public void ApplyTerrainLayers(Terrain terrain, NativeArray<int> biomeIndices, TerrainGenerationSettings settings)
    {
        int width = terrain.terrainData.alphamapResolution;
        int length = terrain.terrainData.alphamapResolution;

        float[,,] splatmapData = new float[width, length, settings.terrainLayers.Length];

        // Build the splatmap based on biome indices
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                int index = x + y * width;
                int biomeIndex = biomeIndices[index];

                for (int i = 0; i < settings.terrainLayers.Length; i++)
                {
                    splatmapData[x, y, i] = (i == biomeIndex) ? 1f : 0f; // Full weight for the current biome
                }
            }
        }

        terrain.terrainData.terrainLayers = settings.terrainLayers; // Assign terrain layers
        terrain.terrainData.SetAlphamaps(0, 0, splatmapData);
    }
}
