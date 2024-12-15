using UnityEngine;
public interface IFeatureModifier
{
    void ApplyFeature(float[,] heights, TerrainGenerationSettings settings, Vector2 location, float intensity, float size);
}
