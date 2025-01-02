using UnityEngine;

[CreateAssetMenu(menuName = "Terrain/Feature Settings")]
public class FeatureSettings : ScriptableObject
{
    [Header("General Settings")]
    public string featureName; // Display name for the feature
    public bool enabled; // Toggle to enable/disable this feature
    public GameObject prefab; // Prefab for the feature

    [Header("Placement Rules")]
    public Vector2 heightRange = new Vector2(0f, 1f); // Min and max heights
    public Vector2 slopeRange = new Vector2(0f, 45f); // Min and max slopes
    public float spawnProbability = 0.5f; // Probability of spawning

    [Header("Biome Interaction")]
    public bool requiresBiome; // Does this feature depend on a biome?
    public int biomeIndex; // Biome index if required

    [Header("Appearance Settings")]
    public Vector2 scaleRange = new Vector2(1f, 1.5f); // Random scale range
    public Vector2 rotationRange = new Vector2(0f, 360f); // Random rotation range
}
