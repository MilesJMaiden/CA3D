using UnityEngine;

[CreateAssetMenu(menuName = "Terrain/Feature Definition")]
public class FeatureDefinition : ScriptableObject
{
    public GameObject prefab;
    public Vector2 heightRange; // The range of heights this feature can spawn in
    public Vector2 slopeRange; // The range of slopes this feature can spawn on
    public float spawnProbability; // The probability of this feature spawning in a valid location
}
