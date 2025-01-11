using System.Collections.Generic;
using UnityEngine;

public class TerrainFeatureContext
{
    public bool hasLakes;
    public bool hasTrails;
    public List<FeatureSettings> activeFeatures;

    public TerrainFeatureContext(bool lakes, bool trails, List<FeatureSettings> features)
    {
        hasLakes = lakes;
        hasTrails = trails;
        activeFeatures = features;
    }
}
