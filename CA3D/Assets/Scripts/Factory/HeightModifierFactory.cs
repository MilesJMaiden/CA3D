using System;
using System.Collections.Generic;

/// <summary>
/// Factory class to dynamically create height modifiers based on settings.
/// </summary>
public static class HeightModifierFactory
{
    private static readonly Dictionary<string, Func<IHeightModifier>> RegisteredModifiers = new();

    static HeightModifierFactory()
    {
        // Register default modifiers
        RegisterModifier("PerlinNoise", () => new PerlinNoiseModifier());
        RegisterModifier("FractalBrownianMotion", () => new FractalBrownianMotionModifier());
        RegisterModifier("MidpointDisplacement", () => new MidpointDisplacementModifier());
        RegisterModifier("VoronoiBiomes", () => new VoronoiBiomesModifier());
    }

    public static void RegisterModifier(string key, Func<IHeightModifier> factoryMethod)
    {
        if (!RegisteredModifiers.ContainsKey(key))
        {
            RegisteredModifiers[key] = factoryMethod;
        }
    }

    public static List<IHeightModifier> CreateModifiers(TerrainGenerationSettings settings)
    {
        var modifiers = new List<IHeightModifier>();

        if (settings.usePerlinNoise && RegisteredModifiers.ContainsKey("PerlinNoise"))
            modifiers.Add(RegisteredModifiers["PerlinNoise"]());
        if (settings.useFractalBrownianMotion && RegisteredModifiers.ContainsKey("FractalBrownianMotion"))
            modifiers.Add(RegisteredModifiers["FractalBrownianMotion"]());
        if (settings.useMidPointDisplacement && RegisteredModifiers.ContainsKey("MidpointDisplacement"))
            modifiers.Add(RegisteredModifiers["MidpointDisplacement"]());
        if (settings.useVoronoiBiomes && RegisteredModifiers.ContainsKey("VoronoiBiomes"))
            modifiers.Add(RegisteredModifiers["VoronoiBiomes"]());

        return modifiers;
    }
}
