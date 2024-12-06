using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class AdvancedTerrainGenerator : MonoBehaviour
{
    [Header("Terrain Dimensions")]
    [Tooltip("Width of the terrain.")]
    public int width = 256;
    [Tooltip("Length of the terrain.")]
    public int length = 256;
    [Tooltip("Maximum height of the terrain.")]
    public int height = 50;

    [Header("Perlin Noise Settings")]
    public bool usePerlinNoise = true;
    [Tooltip("Number of Perlin Noise layers.")]
    public int perlinLayers = 1;
    [Tooltip("Scale of the first Perlin Noise layer.")]
    public float perlinBaseScale = 20f;
    [Tooltip("Amplitude multiplier for each subsequent layer.")]
    public float perlinAmplitudeDecay = 0.5f;
    [Tooltip("Frequency multiplier for each subsequent layer.")]
    public float perlinFrequencyGrowth = 2f;
    [Tooltip("Horizontal offset for the noise.")]
    public float perlinOffsetX = 0f;
    [Tooltip("Vertical offset for the noise.")]
    public float perlinOffsetY = 0f;
    [Tooltip("Blending mode for Perlin Noise layers (Additive/Multiplicative).")]
    public BlendingMode perlinBlendingMode = BlendingMode.Additive;

    public enum BlendingMode
    {
        Additive,
        Multiplicative
    }

    [Header("Fractal Brownian Motion Settings")]
    public bool useFractalBrownianMotion = false;
    [Tooltip("Number of fBm layers (octaves).")]
    public int fBmLayers = 4;
    [Tooltip("Base scale for the first fBm layer.")]
    public float fBmBaseScale = 20f;
    [Tooltip("Amplitude multiplier for each subsequent layer.")]
    public float fBmAmplitudeDecay = 0.5f;
    [Tooltip("Frequency multiplier for each subsequent layer.")]
    public float fBmFrequencyGrowth = 2f;
    [Tooltip("Horizontal offset for the noise.")]
    public float fBmOffsetX = 0f;
    [Tooltip("Vertical offset for the noise.")]
    public float fBmOffsetY = 0f;
    [Tooltip("Blending mode for fBm layers (Additive/Multiplicative).")]
    public BlendingMode fBmBlendingMode = BlendingMode.Additive;

    [Header("Mid-Point Displacement Settings")]
    public bool useMidPointDisplacement = false;
    [Tooltip("Initial randomness factor for displacement.")]
    public float displacementFactor = 1f;
    [Tooltip("Rate at which displacement decreases with each iteration.")]
    public float displacementDecayRate = 0.5f;
    [Tooltip("Random seed for consistent terrain generation.")]
    public int randomSeed = 42;
    [Tooltip("Enable debug logs for Mid-Point Displacement.")]
    public bool enableDebugLogs = false;


    [Header("Voronoi Biomes Settings")]
    public bool useVoronoiBiomes = false;
    [Tooltip("Number of Voronoi regions.")]
    public int voronoiCellCount = 10;
    [Tooltip("Random height range for Voronoi regions.")]
    public Vector2 voronoiHeightRange = new Vector2(0.1f, 0.9f);
    [Tooltip("Distribution mode for Voronoi points.")]
    public DistributionMode voronoiDistributionMode = DistributionMode.Random;
    [Tooltip("Gradient of influence for Voronoi cells.")]
    public AnimationCurve voronoiFalloffCurve = AnimationCurve.Linear(0, 1, 1, 0);
    [Tooltip("Enable debug visualization of Voronoi cells.")]
    public bool enableVoronoiDebug = false;

    public enum DistributionMode
    {
        Random,
        Grid,
        Custom
    }

    [Tooltip("Custom points for Voronoi distribution (used if DistributionMode is Custom).")]
    public List<Vector2> customVoronoiPoints = new List<Vector2>();


    [Header("Marching Cubes Settings")]
    public bool useMarchingCubes = false;
    [Tooltip("Density threshold for Marching Cubes (values above this are solid).")]
    public float marchingCubesThreshold = 0.5f;
    [Tooltip("Voxel size for the scalar field.")]
    public float marchingCubesVoxelSize = 1f;
    [Tooltip("Enable debug visualization for the Marching Cubes scalar field.")]
    public bool enableMarchingCubesDebug = false;


    [Header("Performance Settings")]
    [Tooltip("Regenerate terrain only when toggled or variables change.")]
    public bool regenerateOnVariableChange = true;

    private Terrain m_Terrain;
    private TerrainData m_TerrainData;

    private bool m_ShouldRegenerate;

    private void Awake()
    {
        m_Terrain = GetComponent<Terrain>();
        m_TerrainData = m_Terrain.terrainData;
        GenerateTerrain();
    }

    private void OnValidate()
    {
        if (regenerateOnVariableChange)
            m_ShouldRegenerate = true;
    }

    private void Update()
    {
        if (m_ShouldRegenerate)
        {
            GenerateTerrain();
            m_ShouldRegenerate = false;
        }
    }

    private void GenerateTerrain()
    {
        m_TerrainData.heightmapResolution = width + 1;
        m_TerrainData.size = new Vector3(width, height, length);
        m_TerrainData.SetHeights(0, 0, GenerateHeights());
    }

    private float[,] GenerateHeights()
    {
        float[,] heights = new float[width, length];

        if (usePerlinNoise)
            ApplyPerlinNoise(heights);

        if (useFractalBrownianMotion)
            ApplyFractalBrownianMotion(heights);

        if (useMidPointDisplacement)
            ApplyMidPointDisplacement(heights);

        if (useVoronoiBiomes)
            ApplyVoronoiBiomes(heights);

        if (useMarchingCubes)
            ApplyMarchingCubes(heights);

        NormalizeHeights(heights);

        return heights;
    }

    private void ApplyPerlinNoise(float[,] heights)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                float heightValue = 0f;
                float amplitude = 1f;
                float frequency = 1f;

                for (int layer = 0; layer < perlinLayers; layer++)
                {
                    // Calculate scaled and offset coordinates
                    float xCoord = (x / (float)width) * perlinBaseScale * frequency + perlinOffsetX;
                    float yCoord = (y / (float)length) * perlinBaseScale * frequency + perlinOffsetY;

                    // Sample Perlin Noise
                    float noiseValue = Mathf.PerlinNoise(xCoord, yCoord);

                    // Blend noise value
                    if (perlinBlendingMode == BlendingMode.Additive)
                    {
                        heightValue += noiseValue * amplitude;
                    }
                    else if (perlinBlendingMode == BlendingMode.Multiplicative)
                    {
                        heightValue = (heightValue == 0f ? 1f : heightValue) * (1 + noiseValue * amplitude);
                    }

                    // Update amplitude and frequency for next layer
                    amplitude *= perlinAmplitudeDecay;
                    frequency *= perlinFrequencyGrowth;
                }

                heights[x, y] += heightValue;
            }
        }
    }


    private void ApplyFractalBrownianMotion(float[,] heights)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                float heightValue = 0f;
                float amplitude = 1f;
                float frequency = 1f;

                for (int layer = 0; layer < fBmLayers; layer++)
                {
                    // Calculate scaled and offset coordinates
                    float xCoord = (x / (float)width) * fBmBaseScale * frequency + fBmOffsetX;
                    float yCoord = (y / (float)length) * fBmBaseScale * frequency + fBmOffsetY;

                    // Sample Perlin Noise for this layer
                    float noiseValue = Mathf.PerlinNoise(xCoord, yCoord);

                    // Blend noise value based on the chosen mode
                    if (fBmBlendingMode == BlendingMode.Additive)
                    {
                        heightValue += noiseValue * amplitude;
                    }
                    else if (fBmBlendingMode == BlendingMode.Multiplicative)
                    {
                        heightValue = (heightValue == 0f ? 1f : heightValue) * (1 + noiseValue * amplitude);
                    }

                    // Update amplitude and frequency for the next layer
                    amplitude *= fBmAmplitudeDecay;
                    frequency *= fBmFrequencyGrowth;
                }

                heights[x, y] += heightValue;
            }
        }
    }


    private void ApplyMidPointDisplacement(float[,] heights)
    {
        // Ensure grid size matches 2^n + 1
        if (!IsPowerOfTwoPlusOne(width) || !IsPowerOfTwoPlusOne(length))
        {
            Debug.LogError("Width and Length must be of size 2^n + 1 (e.g., 129, 257, 513) for Mid-Point Displacement to work.");
            return;
        }

        // Set random seed for deterministic results
        Random.InitState(randomSeed);

        // Initialize corners
        heights[0, 0] = Random.Range(0f, 1f);
        heights[0, length - 1] = Random.Range(0f, 1f);
        heights[width - 1, 0] = Random.Range(0f, 1f);
        heights[width - 1, length - 1] = Random.Range(0f, 1f);

        int stepSize = width - 1;
        float currentDisplacement = displacementFactor;

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

                    // Calculate average of corners
                    float average = (
                        heights[x, y] +                       // Top-left
                        heights[x + stepSize, y] +            // Top-right
                        heights[x, y + stepSize] +            // Bottom-left
                        heights[x + stepSize, y + stepSize]   // Bottom-right
                    ) / 4f;

                    // Add random displacement
                    heights[midX, midY] = Mathf.Clamp(average + Random.Range(-currentDisplacement, currentDisplacement), 0f, 1f);

                    if (enableDebugLogs)
                        Debug.Log($"Square Step - Midpoint ({midX}, {midY}) set to {heights[midX, midY]}");
                }
            }

            // Diamond Step
            for (int x = 0; x < width; x += halfStep)
            {
                for (int y = (x + halfStep) % stepSize; y < length; y += stepSize)
                {
                    float average = 0f;
                    int count = 0;

                    // Top
                    if (y - halfStep >= 0)
                    {
                        average += heights[x, y - halfStep];
                        count++;
                    }

                    // Bottom
                    if (y + halfStep < length)
                    {
                        average += heights[x, y + halfStep];
                        count++;
                    }

                    // Left
                    if (x - halfStep >= 0)
                    {
                        average += heights[x - halfStep, y];
                        count++;
                    }

                    // Right
                    if (x + halfStep < width)
                    {
                        average += heights[x + halfStep, y];
                        count++;
                    }

                    // Set diamond point height
                    heights[x, y] = Mathf.Clamp((average / count) + Random.Range(-currentDisplacement, currentDisplacement), 0f, 1f);

                    if (enableDebugLogs)
                        Debug.Log($"Diamond Step - Point ({x}, {y}) set to {heights[x, y]}");
                }
            }

            // Reduce step size and displacement
            stepSize /= 2;
            currentDisplacement *= displacementDecayRate;
        }
    }

    private bool IsPowerOfTwoPlusOne(int value)
    {
        return (value - 1 & (value - 2)) == 0;
    }

    private void ApplyVoronoiBiomes(float[,] heights)
    {
        List<Vector2> points = GenerateVoronoiPoints();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                float minDist = float.MaxValue;
                int closestPointIndex = -1;

                // Find the closest Voronoi point
                for (int i = 0; i < points.Count; i++)
                {
                    float dist = Vector2.Distance(points[i], new Vector2(x, y));
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestPointIndex = i;
                    }
                }

                // Use the falloff curve and height range for the influence
                float normalizedDistance = minDist / Mathf.Max(width, length);
                float falloffValue = voronoiFalloffCurve.Evaluate(1 - normalizedDistance);
                heights[x, y] = Mathf.Lerp(voronoiHeightRange.x, voronoiHeightRange.y, falloffValue);

                // Debug visualization (optional)
                if (enableVoronoiDebug && closestPointIndex != -1)
                {
                    Debug.DrawLine(
                        new Vector3(points[closestPointIndex].x, 0, points[closestPointIndex].y),
                        new Vector3(x, 0, y),
                        Color.Lerp(Color.red, Color.blue, falloffValue)
                    );
                }
            }
        }
    }

    private List<Vector2> GenerateVoronoiPoints()
    {
        List<Vector2> points = new List<Vector2>();

        switch (voronoiDistributionMode)
        {
            case DistributionMode.Random:
                for (int i = 0; i < voronoiCellCount; i++)
                {
                    points.Add(new Vector2(Random.Range(0, width), Random.Range(0, length)));
                }
                break;

            case DistributionMode.Grid:
                int gridSize = Mathf.CeilToInt(Mathf.Sqrt(voronoiCellCount));
                float cellWidth = (float)width / gridSize;
                float cellHeight = (float)length / gridSize;

                for (int x = 0; x < gridSize; x++)
                {
                    for (int y = 0; y < gridSize; y++)
                    {
                        if (points.Count >= voronoiCellCount)
                            break;

                        float px = x * cellWidth + cellWidth / 2f;
                        float py = y * cellHeight + cellHeight / 2f;
                        points.Add(new Vector2(px, py));
                    }
                }
                break;

            case DistributionMode.Custom:
                points.AddRange(customVoronoiPoints);
                break;
        }

        return points;
    }

    private void ApplyMarchingCubes(float[,] heights)
    {
        // Define the size of the scalar field
        int gridWidth = Mathf.CeilToInt(width / marchingCubesVoxelSize);
        int gridLength = Mathf.CeilToInt(length / marchingCubesVoxelSize);
        int gridHeight = Mathf.CeilToInt(height / marchingCubesVoxelSize);

        // Create a 3D scalar field
        float[,,] scalarField = new float[gridWidth, gridHeight, gridLength];

        // Populate the scalar field based on the heightmap
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                float heightValue = heights[Mathf.Clamp(x * (width / gridWidth), 0, width - 1), Mathf.Clamp(z * (length / gridLength), 0, length - 1)] * height;

                for (int y = 0; y < gridHeight; y++)
                {
                    scalarField[x, y, z] = y < heightValue ? 1f : 0f;
                }
            }
        }

        // Apply the Marching Cubes algorithm to generate the mesh
        Mesh marchingCubesMesh = GenerateMarchingCubesMesh(scalarField);

        // Assign the generated mesh to the terrain
        GameObject marchingCubesObject = new GameObject("MarchingCubesMesh", typeof(MeshFilter), typeof(MeshRenderer));
        marchingCubesObject.GetComponent<MeshFilter>().mesh = marchingCubesMesh;
        marchingCubesObject.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));

        // Optional debug visualization
        if (enableMarchingCubesDebug)
        {
            Debug.Log("Marching Cubes mesh generated with vertices: " + marchingCubesMesh.vertexCount);
        }
    }

    private Mesh GenerateMarchingCubesMesh(float[,,] scalarField)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        int width = scalarField.GetLength(0);
        int height = scalarField.GetLength(1);
        int length = scalarField.GetLength(2);

        // Iterate through the scalar field
        for (int x = 0; x < width - 1; x++)
        {
            for (int y = 0; y < height - 1; y++)
            {
                for (int z = 0; z < length - 1; z++)
                {
                    // Create a cube of scalar values
                    float[] cube = new float[8];
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 offset = GetVertexOffset(i);
                        cube[i] = scalarField[x + (int)offset.x, y + (int)offset.y, z + (int)offset.z];
                    }

                    // Determine the cube's case based on the threshold
                    int caseIndex = GetCubeCaseIndex(cube);

                    // Add triangles for this cube case
                    for (int i = 0; MarchingCubesTables.Triangles[caseIndex, i] != -1; i += 3)
                    {
                        int edge1 = MarchingCubesTables.Triangles[caseIndex, i];
                        int edge2 = MarchingCubesTables.Triangles[caseIndex, i + 1];
                        int edge3 = MarchingCubesTables.Triangles[caseIndex, i + 2];

                        Vector3 vertex1 = InterpolateEdge(edge1, cube, x, y, z);
                        Vector3 vertex2 = InterpolateEdge(edge2, cube, x, y, z);
                        Vector3 vertex3 = InterpolateEdge(edge3, cube, x, y, z);

                        int vertexIndex = vertices.Count;
                        vertices.Add(vertex1);
                        vertices.Add(vertex2);
                        vertices.Add(vertex3);

                        triangles.Add(vertexIndex);
                        triangles.Add(vertexIndex + 1);
                        triangles.Add(vertexIndex + 2);
                    }
                }
            }
        }

        // Create the mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }

    private int GetCubeCaseIndex(float[] cube)
    {
        int caseIndex = 0;
        for (int i = 0; i < 8; i++)
        {
            if (cube[i] > marchingCubesThreshold)
            {
                caseIndex |= 1 << i;
            }
        }
        return caseIndex;
    }

    private Vector3 InterpolateEdge(int edge, float[] cube, int x, int y, int z)
    {
        int vertexAIndex = MarchingCubesTables.EdgeVertices[edge, 0];
        int vertexBIndex = MarchingCubesTables.EdgeVertices[edge, 1];

        Vector3 vertexAOffset = GetVertexOffset(vertexAIndex);
        Vector3 vertexBOffset = GetVertexOffset(vertexBIndex);

        float valueA = cube[vertexAIndex];
        float valueB = cube[vertexBIndex];

        float t = (marchingCubesThreshold - valueA) / (valueB - valueA);

        Vector3 vertexA = new Vector3(x, y, z) + vertexAOffset;
        Vector3 vertexB = new Vector3(x, y, z) + vertexBOffset;

        return Vector3.Lerp(vertexA, vertexB, t);
    }

    private Vector3 GetVertexOffset(int vertexIndex)
    {
        return new Vector3(
            (vertexIndex & 1) == 1 ? 1 : 0,
            (vertexIndex & 2) == 2 ? 1 : 0,
            (vertexIndex & 4) == 4 ? 1 : 0
        );
    }

    private void NormalizeHeights(float[,] heights)
    {
        float maxHeight = float.MinValue;
        float minHeight = float.MaxValue;

        foreach (var heightValue in heights)
        {
            if (heightValue > maxHeight) maxHeight = heightValue;
            if (heightValue < minHeight) minHeight = heightValue;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                heights[x, y] = Mathf.InverseLerp(minHeight, maxHeight, heights[x, y]);
            }
        }
    }
}
