using Unity.Collections;

using Unity.Jobs;

/// <summary>
/// Interface for classes that modify the terrain heightmap.
/// Implementations can provide both synchronous and asynchronous (jobified) approaches.
/// </summary>
public interface IHeightModifier
{
    /// <summary>
    /// Synchronous method to modify the entire 2D heightmap in-place.
    /// Usually used for smaller or less performance-critical operations.
    /// </summary>
    /// <param name="heights">2D array of the terrain heights.</param>
    /// <param name="settings">Terrain generation settings.</param>
    void ModifyHeight(float[,] heights, TerrainGenerationSettings settings);

    /// <summary>
    /// Asynchronous job-based method to modify a flattened heightmap (NativeArray).
    /// This enables parallel processing for performance-critical tasks.
    /// </summary>
    /// <param name="heightsNative">Flattened NativeArray of heights.</param>
    /// <param name="width">Terrain width.</param>
    /// <param name="length">Terrain length.</param>
    /// <param name="settings">Terrain generation settings.</param>
    /// <param name="dependency">An existing job dependency (if any).</param>
    /// <returns>A JobHandle for chaining additional jobs.</returns>
    JobHandle ScheduleJob(
        NativeArray<float> heightsNative,
        int width,
        int length,
        TerrainGenerationSettings settings,
        JobHandle dependency
    );
}
