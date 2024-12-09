using UnityEngine;

public class MidpointDisplacementModifier : IHeightModifier
{
    public void ModifyHeight(float[,] heights, TerrainGenerationSettings settings)
    {
        int width = heights.GetLength(0);
        int length = heights.GetLength(1);

        // Ensure grid size matches 2^n + 1
        if (!IsPowerOfTwoPlusOne(width) || !IsPowerOfTwoPlusOne(length))
        {
            Debug.LogError("Width and Length must be of size 2^n + 1 (e.g., 129, 257, 513) for Midpoint Displacement to work.");
            return;
        }

        Random.InitState(settings.randomSeed);

        // Initialize corners
        heights[0, 0] = Random.Range(0f, 1f);
        heights[0, length - 1] = Random.Range(0f, 1f);
        heights[width - 1, 0] = Random.Range(0f, 1f);
        heights[width - 1, length - 1] = Random.Range(0f, 1f);

        int stepSize = width - 1;
        float currentDisplacement = settings.displacementFactor;

        while (stepSize > 1)
        {
            int halfStep = stepSize / 2;

            // Square Step
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

                    heights[midX, midY] = Mathf.Clamp(average + Random.Range(-currentDisplacement, currentDisplacement), 0f, 1f);
                }
            }

            // Diamond Step
            for (int x = 0; x < width; x += halfStep)
            {
                for (int y = (x + halfStep) % stepSize; y < length; y += stepSize)
                {
                    float average = 0f;
                    int count = 0;

                    if (y - halfStep >= 0)
                    {
                        average += heights[x, y - halfStep];
                        count++;
                    }
                    if (y + halfStep < length)
                    {
                        average += heights[x, y + halfStep];
                        count++;
                    }
                    if (x - halfStep >= 0)
                    {
                        average += heights[x - halfStep, y];
                        count++;
                    }
                    if (x + halfStep < width)
                    {
                        average += heights[x + halfStep, y];
                        count++;
                    }

                    heights[x, y] = Mathf.Clamp((average / count) + Random.Range(-currentDisplacement, currentDisplacement), 0f, 1f);
                }
            }

            stepSize /= 2;
            currentDisplacement *= settings.displacementDecayRate;
        }
    }

    private bool IsPowerOfTwoPlusOne(int value)
    {
        return (value - 1 & (value - 2)) == 0;
    }
}
