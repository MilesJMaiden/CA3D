using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

public class ThermalErosionModifier : IFeatureModifier
{
    public JobHandle ScheduleJob(NativeArray<float> heights, int width, int length, TerrainGenerationSettings settings, JobHandle dependency)
    {
        NativeArray<float> bufferA = new NativeArray<float>(heights.Length, Allocator.TempJob);
        NativeArray<float> bufferB = new NativeArray<float>(heights.Length, Allocator.TempJob);

        // Copy initial heights into bufferA
        var initJob = new CopyJob
        {
            source = heights,
            destination = bufferA
        };
        JobHandle handle = initJob.Schedule(dependency);

        // Alternate buffers for erosion iterations
        for (int i = 0; i < settings.erosionIterations; i++)
        {
            var erosionJob = new ThermalErosionJob
            {
                width = width,
                length = length,
                talusAngle = settings.talusAngle,
                inputBuffer = i % 2 == 0 ? bufferA : bufferB,
                outputBuffer = i % 2 == 0 ? bufferB : bufferA
            };

            handle = erosionJob.Schedule(heights.Length, 64, handle);
        }

        // Copy the final buffer back to heights
        var finalCopyJob = new CopyJob
        {
            source = settings.erosionIterations % 2 == 0 ? bufferA : bufferB,
            destination = heights
        };
        handle = finalCopyJob.Schedule(handle);

        // Dispose buffers
        bufferA.Dispose(handle);
        bufferB.Dispose(handle);

        return handle;
    }

    [BurstCompile]
    private struct CopyJob : IJob
    {
        [ReadOnly] public NativeArray<float> source;
        [WriteOnly] public NativeArray<float> destination;

        public void Execute()
        {
            NativeArray<float>.Copy(source, destination);
        }
    }
}
