using UnityEngine;

[CreateAssetMenu(menuName = "Terrain/Feature Settings")]
public class FeatureSettings : ScriptableObject
{
    [Header("General Settings")]
    public string featureName;
    public bool enabled;
    public GameObject prefab;

    [Header("Placement Rules")]
    public Vector2 heightRange = new Vector2(0f, 1f); 
    public Vector2 slopeRange = new Vector2(0f, 45f);
    public float spawnProbability = 0.5f;

    [Header("Biome Interaction")]
    public bool requiresBiome;
    public int biomeIndex;

    [Header("Appearance Settings")]
    public Vector2 scaleRange = new Vector2(1f, 1.5f);
    public Vector2 rotationRange = new Vector2(0f, 360f);
}
