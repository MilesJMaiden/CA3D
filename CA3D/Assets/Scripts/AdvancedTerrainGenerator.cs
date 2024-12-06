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
        // Define the size of the scalar field.
        int gridWidth = Mathf.CeilToInt(width / marchingCubesVoxelSize);
        int gridLength = Mathf.CeilToInt(length / marchingCubesVoxelSize);
        int gridHeight = Mathf.CeilToInt(height / marchingCubesVoxelSize);

        // Create a 3D scalar field based on the heightmap.
        // We'll create a binary field: density = 1 below terrain surface, 0 above.
        float[,,] scalarField = new float[gridWidth, gridHeight, gridLength];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridLength; z++)
            {
                int mapX = Mathf.Clamp((int)((x / (float)gridWidth) * (width - 1)), 0, width - 1);
                int mapZ = Mathf.Clamp((int)((z / (float)gridLength) * (length - 1)), 0, length - 1);

                float terrainHeight = heights[mapX, mapZ] * height;

                for (int y = 0; y < gridHeight; y++)
                {
                    // If the cell is below the height surface, it's inside (density=1)
                    // If above, it's outside (density=0)
                    float density = (y <= terrainHeight) ? 1f : 0f;
                    scalarField[x, y, z] = density;
                }
            }
        }

        // Apply the Marching Cubes algorithm to generate the mesh.
        Mesh marchingCubesMesh = GenerateMarchingCubesMesh(scalarField);

        // Create a new GameObject for the generated mesh.
        GameObject marchingCubesObject = new GameObject("MarchingCubesMesh", typeof(MeshFilter), typeof(MeshRenderer));
        marchingCubesObject.GetComponent<MeshFilter>().mesh = marchingCubesMesh;
        marchingCubesObject.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Standard"));

        if (enableMarchingCubesDebug)
        {
            Debug.Log("Marching Cubes mesh generated with vertices: " + marchingCubesMesh.vertexCount);
        }
    }

    private Mesh GenerateMarchingCubesMesh(float[,,] scalarField)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        Dictionary<(float, float, float), int> vertexCache = new Dictionary<(float, float, float), int>();

        int w = scalarField.GetLength(0);
        int h = scalarField.GetLength(1);
        int l = scalarField.GetLength(2);

        for (int x = 0; x < w - 1; x++)
        {
            for (int y = 0; y < h - 1; y++)
            {
                for (int z = 0; z < l - 1; z++)
                {
                    // Gather the cube's corner densities
                    float[] cube = new float[8];
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 offset = GetVertexOffset(i);
                        int ox = x + (int)offset.x;
                        int oy = y + (int)offset.y;
                        int oz = z + (int)offset.z;
                        cube[i] = scalarField[ox, oy, oz];
                    }

                    // Determine the cube configuration index.
                    int cubeIndex = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        if (cube[i] > marchingCubesThreshold) cubeIndex |= 1 << i;
                    }

                    // If the cube is entirely inside or outside, no triangles:
                    if (MarchingCubesTables.EdgeTable[cubeIndex] == 0) continue;

                    // For each possible triangle in this configuration:
                    for (int i = 0; i < 16; i += 3)
                    {
                        int a0 = MarchingCubesTables.TriangleTable[cubeIndex, i + 0];
                        if (a0 == -1) break;
                        int a1 = MarchingCubesTables.TriangleTable[cubeIndex, i + 1];
                        int a2 = MarchingCubesTables.TriangleTable[cubeIndex, i + 2];

                        Vector3 v0 = InterpolateEdge(a0, cube, x, y, z);
                        Vector3 v1 = InterpolateEdge(a1, cube, x, y, z);
                        Vector3 v2 = InterpolateEdge(a2, cube, x, y, z);

                        int vIndex0 = AddVertex(vertices, vertexCache, v0);
                        int vIndex1 = AddVertex(vertices, vertexCache, v1);
                        int vIndex2 = AddVertex(vertices, vertexCache, v2);

                        triangles.Add(vIndex0);
                        triangles.Add(vIndex1);
                        triangles.Add(vIndex2);
                    }
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }

    private int AddVertex(List<Vector3> vertices, Dictionary<(float, float, float), int> cache, Vector3 vertex)
    {
        var key = (vertex.x, vertex.y, vertex.z);
        if (cache.TryGetValue(key, out int index))
            return index;

        index = vertices.Count;
        vertices.Add(vertex);
        cache[key] = index;
        return index;
    }

    private Vector3 InterpolateEdge(int edge, float[] cube, int x, int y, int z)
    {
        int v0 = MarchingCubesTables.EdgeToVertex[edge, 0];
        int v1 = MarchingCubesTables.EdgeToVertex[edge, 1];

        Vector3 p0 = new Vector3(x, y, z) + GetVertexOffset(v0);
        Vector3 p1 = new Vector3(x, y, z) + GetVertexOffset(v1);

        float v0Density = cube[v0];
        float v1Density = cube[v1];

        float denominator = (v1Density - v0Density);
        float t = Mathf.Abs(denominator) < 1e-6f ? 0.5f : (marchingCubesThreshold - v0Density) / denominator;
        t = Mathf.Clamp01(t);

        Vector3 p = Vector3.Lerp(p0, p1, t) * marchingCubesVoxelSize;
        return p;
    }

    private Vector3 GetVertexOffset(int vertexIndex)
    {
        return new Vector3(
            (vertexIndex & 1) == 1 ? 1 : 0,
            (vertexIndex & 2) == 2 ? 1 : 0,
            (vertexIndex & 4) == 4 ? 1 : 0
        );
    }



    private int GetCubeCaseIndex(float[] cube)
    {
        int caseIndex = 0;
        for (int i = 0; i < 8; i++)
        {
            if (cube[i] > marchingCubesThreshold)
                caseIndex |= 1 << i;
        }
        return caseIndex;
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