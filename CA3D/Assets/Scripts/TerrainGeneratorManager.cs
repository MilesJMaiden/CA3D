using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TerrainGeneratorManager : MonoBehaviour
{
    [Header("Terrain Dimensions")]
    public int width = 256;
    public int length = 256;
    public int height = 50;

    [Header("Settings")]
    public TerrainGenerationSettings terrainSettings;

    private ITerrainGenerator terrainGenerator;
    private Terrain m_Terrain;
    private TerrainData m_TerrainData;

    private void Awake()
    {
        m_Terrain = GetComponent<Terrain>();
        m_TerrainData = m_Terrain.terrainData;

        InitializeGenerator();
        GenerateTerrain();
    }

    public void GenerateTerrain()
    {
        if (terrainSettings == null)
        {
            Debug.LogError("Terrain settings not assigned!");
            return;
        }

        // Ensure the generator uses the latest settings
        InitializeGenerator();

        m_TerrainData.heightmapResolution = width + 1;
        m_TerrainData.size = new Vector3(width, height, length);
        m_TerrainData.SetHeights(0, 0, terrainGenerator.GenerateHeights(width, length));
    }

    private void InitializeGenerator()
    {
        if (terrainSettings != null)
        {
            terrainGenerator = new TerrainGenerator(terrainSettings);
        }
        else
        {
            Debug.LogError("Terrain settings are null. Cannot initialize generator.");
        }
    }
}
