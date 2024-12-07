using UnityEngine;
using UnityEngine.UI;

public class TerrainUIManager : MonoBehaviour
{
    [Header("UI References")]
    public InputField perlinLayersField;
    public InputField perlinBaseScaleField;
    public InputField perlinAmplitudeDecayField;
    public InputField perlinFrequencyGrowthField;
    public InputField perlinOffsetXField;
    public InputField perlinOffsetYField;

    public Toggle usePerlinNoiseToggle;

    public InputField fBmLayersField;
    public InputField fBmBaseScaleField;
    public InputField fBmAmplitudeDecayField;
    public InputField fBmFrequencyGrowthField;
    public InputField fBmOffsetXField;
    public InputField fBmOffsetYField;

    public Toggle useFractalBrownianMotionToggle;

    public InputField displacementFactorField;
    public InputField displacementDecayRateField;
    public InputField randomSeedField;

    public Toggle useMidPointDisplacementToggle;

    public InputField voronoiCellCountField;
    public InputField voronoiHeightRangeMinField;
    public InputField voronoiHeightRangeMaxField;

    public Toggle useVoronoiBiomesToggle;

    public Button generateButton;

    [Header("Terrain Generator Reference")]
    public TerrainGeneratorManager terrainGeneratorManager;

    [Header("Default Configuration")]
    public TerrainGenerationSettings defaultSettings;

    private void Start()
    {
        LoadDefaultValues();
        generateButton.onClick.AddListener(OnGenerateButtonPressed);
    }

    private void LoadDefaultValues()
    {
        if (defaultSettings == null)
        {
            Debug.LogError("Default settings not assigned!");
            return;
        }

        // Perlin Noise
        usePerlinNoiseToggle.isOn = defaultSettings.usePerlinNoise;
        perlinLayersField.text = defaultSettings.perlinLayers.ToString();
        perlinBaseScaleField.text = defaultSettings.perlinBaseScale.ToString();
        perlinAmplitudeDecayField.text = defaultSettings.perlinAmplitudeDecay.ToString();
        perlinFrequencyGrowthField.text = defaultSettings.perlinFrequencyGrowth.ToString();
        perlinOffsetXField.text = defaultSettings.perlinOffset.x.ToString();
        perlinOffsetYField.text = defaultSettings.perlinOffset.y.ToString();

        // Fractal Brownian Motion
        useFractalBrownianMotionToggle.isOn = defaultSettings.useFractalBrownianMotion;
        fBmLayersField.text = defaultSettings.fBmLayers.ToString();
        fBmBaseScaleField.text = defaultSettings.fBmBaseScale.ToString();
        fBmAmplitudeDecayField.text = defaultSettings.fBmAmplitudeDecay.ToString();
        fBmFrequencyGrowthField.text = defaultSettings.fBmFrequencyGrowth.ToString();
        fBmOffsetXField.text = defaultSettings.fBmOffset.x.ToString();
        fBmOffsetYField.text = defaultSettings.fBmOffset.y.ToString();

        // Midpoint Displacement
        useMidPointDisplacementToggle.isOn = defaultSettings.useMidPointDisplacement;
        displacementFactorField.text = defaultSettings.displacementFactor.ToString();
        displacementDecayRateField.text = defaultSettings.displacementDecayRate.ToString();
        randomSeedField.text = defaultSettings.randomSeed.ToString();

        // Voronoi Biomes
        useVoronoiBiomesToggle.isOn = defaultSettings.useVoronoiBiomes;
        voronoiCellCountField.text = defaultSettings.voronoiCellCount.ToString();
        voronoiHeightRangeMinField.text = defaultSettings.voronoiHeightRange.x.ToString();
        voronoiHeightRangeMaxField.text = defaultSettings.voronoiHeightRange.y.ToString();
    }

    private void OnGenerateButtonPressed()
    {
        if (terrainGeneratorManager == null)
        {
            Debug.LogError("Terrain Generator Manager not assigned!");
            return;
        }

        // Apply updated settings
        TerrainGenerationSettings updatedSettings = ScriptableObject.CreateInstance<TerrainGenerationSettings>();

        // Perlin Noise
        updatedSettings.usePerlinNoise = usePerlinNoiseToggle.isOn;
        updatedSettings.perlinLayers = int.Parse(perlinLayersField.text);
        updatedSettings.perlinBaseScale = float.Parse(perlinBaseScaleField.text);
        updatedSettings.perlinAmplitudeDecay = float.Parse(perlinAmplitudeDecayField.text);
        updatedSettings.perlinFrequencyGrowth = float.Parse(perlinFrequencyGrowthField.text);
        updatedSettings.perlinOffset = new Vector2(
            float.Parse(perlinOffsetXField.text),
            float.Parse(perlinOffsetYField.text)
        );

        // Fractal Brownian Motion
        updatedSettings.useFractalBrownianMotion = useFractalBrownianMotionToggle.isOn;
        updatedSettings.fBmLayers = int.Parse(fBmLayersField.text);
        updatedSettings.fBmBaseScale = float.Parse(fBmBaseScaleField.text);
        updatedSettings.fBmAmplitudeDecay = float.Parse(fBmAmplitudeDecayField.text);
        updatedSettings.fBmFrequencyGrowth = float.Parse(fBmFrequencyGrowthField.text);
        updatedSettings.fBmOffset = new Vector2(
            float.Parse(fBmOffsetXField.text),
            float.Parse(fBmOffsetYField.text)
        );

        // Midpoint Displacement
        updatedSettings.useMidPointDisplacement = useMidPointDisplacementToggle.isOn;
        updatedSettings.displacementFactor = float.Parse(displacementFactorField.text);
        updatedSettings.displacementDecayRate = float.Parse(displacementDecayRateField.text);
        updatedSettings.randomSeed = int.Parse(randomSeedField.text);

        // Voronoi Biomes
        updatedSettings.useVoronoiBiomes = useVoronoiBiomesToggle.isOn;
        updatedSettings.voronoiCellCount = int.Parse(voronoiCellCountField.text);
        updatedSettings.voronoiHeightRange = new Vector2(
            float.Parse(voronoiHeightRangeMinField.text),
            float.Parse(voronoiHeightRangeMaxField.text)
        );

        // Pass the new settings to the Terrain Generator Manager
        terrainGeneratorManager.terrainSettings = updatedSettings;
        terrainGeneratorManager.GenerateTerrain();

        Debug.Log("Terrain generated with updated settings.");
    }
}
