using UnityEngine;

/// <summary>
/// A height modifier that applies the midpoint displacement algorithm to a terrain heightmap.
/// </summary>
public class MidpointDisplacementModifier : IHeightModifier
{
    #region Public Methods

    /// <summary>
    /// Modifies the terrain heights using the midpoint displacement algorithm.
    /// </summary>
    /// <param name="heights">The 2D array of terrain heights to modify.</param>
    /// <param name="settings">The terrain generation settings used for midpoint displacement.</param>
    public void ModifyHeight(float[,] heights, TerrainGenerationSettings settings)
    {
        int width = heights.GetLength(0);
        int length = heights.GetLength(1);

        // Validate grid size
        if (!IsPowerOfTwoPlusOne(width) || !IsPowerOfTwoPlusOne(length))
        {
            Debug.LogError(
                $"Width and Length must be of size 2^n + 1 (e.g., 129, 257, 513). " +
                $"Provided: Width = {width}, Length = {length}."
            );
            return;
        }

        // Initialize random seed for reproducibility
        Random.InitState(settings.randomSeed);

        // Create a temporary heightmap for midpoint displacement
        float[,] displacementHeights = new float[width, length];
        InitializeCorners(displacementHeights);

        int stepSize = width - 1;
        float currentDisplacement = settings.displacementFactor;

        while (stepSize > 1)
        {
            int halfStep = stepSize / 2;

            // Perform square and diamond steps
            SquareStep(displacementHeights, stepSize, halfStep, currentDisplacement);
            DiamondStep(displacementHeights, stepSize, halfStep, currentDisplacement, width, length);

            // Reduce step size and displacement
            stepSize /= 2;
            currentDisplacement *= settings.displacementDecayRate;
        }

        // Combine the generated displacement heights with the existing heights
        CombineHeights(heights, displacementHeights, settings.displacementFactor);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Initializes the corner heights of the displacement heightmap.
    /// </summary>
    private void InitializeCorners(float[,] heights)
    {
        int width = heights.GetLength(0);
        int length = heights.GetLength(1);

        heights[0, 0] = Random.Range(0f, 1f);
        heights[0, length - 1] = Random.Range(0f, 1f);
        heights[width - 1, 0] = Random.Range(0f, 1f);
        heights[width - 1, length - 1] = Random.Range(0f, 1f);
    }

    /// <summary>
    /// Performs the square step of the midpoint displacement algorithm.
    /// </summary>
    private void SquareStep(float[,] heights, int stepSize, int halfStep, float displacement)
    {
        int width = heights.GetLength(0);
        int length = heights.GetLength(1);

        for (int x = 0; x < width - 1; x += stepSize)
        {
            for (int y = 0; y < length - 1; y += stepSize)
            {
                int midX = x + halfStep;
                int midY = y + halfStep;

                float average = (
                    heights[x, y] +
                    heights[x + stepSize, y] +
                    heights[x, y + stepSize] +
                    heights[x + stepSize, y + stepSize]
                ) / 4f;

                heights[midX, midY] = Mathf.Clamp(average + Random.Range(-displacement, displacement), 0f, 1f);
            }
        }
    }

    /// <summary>
    /// Performs the diamond step of the midpoint displacement algorithm.
    /// </summary>
    private void DiamondStep(float[,] heights, int stepSize, int halfStep, float displacement, int width, int length)
    {
        for (int x = 0; x < width; x += halfStep)
        {
            for (int y = (x + halfStep) % stepSize; y < length; y += stepSize)
            {
                float average = 0f;
                int count = 0;

                if (y - halfStep >= 0) // Top neighbor
                {
                    average += heights[x, y - halfStep];
                    count++;
                }
                if (y + halfStep < length) // Bottom neighbor
                {
                    average += heights[x, y + halfStep];
                    count++;
                }
                if (x - halfStep >= 0) // Left neighbor
                {
                    average += heights[x - halfStep, y];
                    count++;
                }
                if (x + halfStep < width) // Right neighbor
                {
                    average += heights[x + halfStep, y];
                    count++;
                }

                heights[x, y] = Mathf.Clamp((average / count) + Random.Range(-displacement, displacement), 0f, 1f);
            }
        }
    }

    /// <summary>
    /// Combines the midpoint displacement heights with the existing heightmap.
    /// </summary>
    /// <param name="originalHeights">The original heightmap to combine with.</param>
    /// <param name="newHeights">The newly generated heightmap to combine.</param>
    /// <param name="scale">The scaling factor for the new heights.</param>
    private void CombineHeights(float[,] originalHeights, float[,] newHeights, float scale)
    {
        int width = originalHeights.GetLength(0);
        int length = originalHeights.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                originalHeights[x, y] += newHeights[x, y] * scale;
                originalHeights[x, y] = Mathf.Clamp(originalHeights[x, y], 0f, 1f); // Ensure within range
            }
        }
    }

    /// <summary>
    /// Validates if a number is of the form 2^n + 1.
    /// </summary>
    private bool IsPowerOfTwoPlusOne(int value)
    {
        int powerOfTwo = value - 1;
        return (powerOfTwo & (powerOfTwo - 1)) == 0 && powerOfTwo > 0;
    }

    #endregion
}
