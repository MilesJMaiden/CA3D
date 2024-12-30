using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

public class ThermalErosionModifier : IFeatureModifier
{
    public JobHandle ScheduleJob(NativeArray<float> heights, int width, int length, TerrainGenerationSettings settings, JobHandle dependency)
    {
        NativeArray<float> buffer = new NativeArray<float>(heights.Length, Allocator.TempJob);

        var erosionJob = new ThermalErosionJob
        {
            width = width,
            length = length,
            talusAngle = settings.talusAngle,
            iterations = settings.erosionIterations,
            inputHeights = heights,
            outputHeights = buffer
        };

        JobHandle handle = erosionJob.Schedule(heights.Length, 64, dependency);

        // Swap input and output
        var copyJob = new CopyJob
        {
            source = buffer,
            destination = heights
        };

        handle = copyJob.Schedule(handle);

        buffer.Dispose(handle);

        return handle;
    }

    [BurstCompile]
    private struct ThermalErosionJob : IJobParallelFor
    {
        public int width;
        public int length;
        public float talusAngle;
        public int iterations;
        [ReadOnly] public NativeArray<float> inputHeights;
        [WriteOnly] public NativeArray<float> outputHeights;

        public void Execute(int index)
        {
            // Perform thermal erosion logic, writing to outputHeights
        }
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
