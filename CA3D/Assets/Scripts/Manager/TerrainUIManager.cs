using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TerrainUIManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown configDropdown;

    public TMP_InputField perlinLayersField;
    public TMP_InputField perlinBaseScaleField;
    public TMP_InputField perlinAmplitudeDecayField;
    public TMP_InputField perlinFrequencyGrowthField;
    public TMP_InputField perlinOffsetXField;
    public TMP_InputField perlinOffsetYField;

    public Toggle usePerlinNoiseToggle;

    public TMP_InputField fBmLayersField;
    public TMP_InputField fBmBaseScaleField;
    public TMP_InputField fBmAmplitudeDecayField;
    public TMP_InputField fBmFrequencyGrowthField;
    public TMP_InputField fBmOffsetXField;
    public TMP_InputField fBmOffsetYField;

    public Toggle useFractalBrownianMotionToggle;

    public TMP_InputField displacementFactorField;
    public TMP_InputField displacementDecayRateField;
    public TMP_InputField randomSeedField;

    public Toggle useMidPointDisplacementToggle;

    public TMP_InputField voronoiCellCountField;
    public TMP_InputField voronoiHeightRangeMinField;
    public TMP_InputField voronoiHeightRangeMaxField;

    public Toggle useVoronoiBiomesToggle;

    public Button generateButton;

    [Header("Terrain Generator Reference")]
    public TerrainGeneratorManager terrainGeneratorManager;

    [Header("Available Configurations")]
    public TerrainGenerationSettings[] availableConfigs;

    private void Start()
    {
        PopulateConfigDropdown();
        LoadDefaultValues();
        configDropdown.onValueChanged.AddListener(OnConfigSelected);
        generateButton.onClick.AddListener(OnGenerateButtonPressed);
    }

    private void PopulateConfigDropdown()
    {
        configDropdown.ClearOptions();

        foreach (var config in availableConfigs)
        {
            configDropdown.options.Add(new TMP_Dropdown.OptionData(config.name));
        }

        configDropdown.value = 0;
        configDropdown.RefreshShownValue();
    }

    private void OnConfigSelected(int index)
    {
        if (index < 0 || index >= availableConfigs.Length)
        {
            Debug.LogError("Invalid configuration index selected.");
            return;
        }

        LoadValuesFromConfig(availableConfigs[index]);
    }

    private void LoadDefaultValues()
    {
        if (availableConfigs == null || availableConfigs.Length == 0)
        {
            Debug.LogError("No configurations available!");
            return;
        }

        LoadValuesFromConfig(availableConfigs[0]);
    }

    private void LoadValuesFromConfig(TerrainGenerationSettings config)
    {
        // Perlin Noise
        usePerlinNoiseToggle.isOn = config.usePerlinNoise;
        perlinLayersField.text = config.perlinLayers.ToString();
        perlinBaseScaleField.text = config.perlinBaseScale.ToString();
        perlinAmplitudeDecayField.text = config.perlinAmplitudeDecay.ToString();
        perlinFrequencyGrowthField.text = config.perlinFrequencyGrowth.ToString();
        perlinOffsetXField.text = config.perlinOffset.x.ToString();
        perlinOffsetYField.text = config.perlinOffset.y.ToString();

        // Fractal Brownian Motion
        useFractalBrownianMotionToggle.isOn = config.useFractalBrownianMotion;
        fBmLayersField.text = config.fBmLayers.ToString();
        fBmBaseScaleField.text = config.fBmBaseScale.ToString();
        fBmAmplitudeDecayField.text = config.fBmAmplitudeDecay.ToString();
        fBmFrequencyGrowthField.text = config.fBmFrequencyGrowth.ToString();
        fBmOffsetXField.text = config.fBmOffset.x.ToString();
        fBmOffsetYField.text = config.fBmOffset.y.ToString();

        // Midpoint Displacement
        useMidPointDisplacementToggle.isOn = config.useMidPointDisplacement;
        displacementFactorField.text = config.displacementFactor.ToString();
        displacementDecayRateField.text = config.displacementDecayRate.ToString();
        randomSeedField.text = config.randomSeed.ToString();

        // Voronoi Biomes
        useVoronoiBiomesToggle.isOn = config.useVoronoiBiomes;
        voronoiCellCountField.text = config.voronoiCellCount.ToString();
        voronoiHeightRangeMinField.text = config.voronoiHeightRange.x.ToString();
        voronoiHeightRangeMaxField.text = config.voronoiHeightRange.y.ToString();
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
