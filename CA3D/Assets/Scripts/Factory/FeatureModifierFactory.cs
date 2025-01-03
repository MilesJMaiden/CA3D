using System.Collections.Generic;

public static class FeatureModifierFactory
{
    /// <summary>
    /// Creates a list of feature modifiers based on the provided terrain settings.
    /// </summary>
    /// <param name="settings">The terrain generation settings.</param>
    /// <returns>A list of feature modifiers.</returns>
    public static List<IFeatureModifier> CreateModifiers(TerrainGenerationSettings settings)
    {
        var modifiers = new List<IFeatureModifier>();

        if (settings.useTrails) modifiers.Add(new TrailModifier());
        if (settings.useLakes) modifiers.Add(new LakeModifier());
        if (settings.useErosion) modifiers.Add(new ThermalErosionModifier());

        return modifiers;
    }
}
