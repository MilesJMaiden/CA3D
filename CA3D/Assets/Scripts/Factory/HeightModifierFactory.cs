using System.Collections.Generic;

/// <summary>
/// Factory class to create height modifiers based on settings.
/// </summary>
public static class HeightModifierFactory
{
    public static List<IHeightModifier> CreateModifiers(TerrainGenerationSettings settings)
    {
        var modifiers = new List<IHeightModifier>();
        if (settings.usePerlinNoise) modifiers.Add(new PerlinNoiseModifier());
        if (settings.useFractalBrownianMotion) modifiers.Add(new FractalBrownianMotionModifier());
        if (settings.useMidPointDisplacement) modifiers.Add(new MidpointDisplacementModifier());
        if (settings.useVoronoiBiomes) modifiers.Add(new VoronoiBiomesModifier());
        return modifiers;
    }
}