using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages the UI for adjusting terrain generation settings and regenerates the terrain dynamically
/// whenever valid inputs are modified.
/// </summary>
public class TerrainUIManager : MonoBehaviour
{
    public enum DistributionMode
    {
        Random,
        Grid
    }

    #region UI References

    [Header("UI References")]
    public TMP_Dropdown configDropdown;

    // Perlin Noise
    public TMP_InputField perlinLayersField;
    public TMP_InputField perlinBaseScaleField;
    public TMP_InputField perlinAmplitudeDecayField;
    public TMP_InputField perlinFrequencyGrowthField;
    public TMP_InputField perlinOffsetXField;
    public TMP_InputField perlinOffsetYField;
    public Toggle usePerlinNoiseToggle;

    // Fractal Brownian Motion
    public TMP_InputField fBmLayersField;
    public TMP_InputField fBmBaseScaleField;
    public TMP_InputField fBmAmplitudeDecayField;
    public TMP_InputField fBmFrequencyGrowthField;
    public TMP_InputField fBmOffsetXField;
    public TMP_InputField fBmOffsetYField;
    public Toggle useFractalBrownianMotionToggle;

    // Midpoint Displacement
    public TMP_InputField displacementFactorField;
    public TMP_InputField displacementDecayRateField;
    public TMP_InputField randomSeedField;
    public Toggle useMidPointDisplacementToggle;

    // Voronoi Biomes
    public TMP_InputField voronoiCellCountField;
    public TMP_InputField voronoiHeightRangeMinField;
    public TMP_InputField voronoiHeightRangeMaxField;
    public TMP_Dropdown voronoiDistributionModeDropdown;
    public TMP_InputField customVoronoiPointsField;
    public Toggle useVoronoiBiomesToggle;

    public TMP_Text errorMessage;

    [Header("Terrain Generator Reference")]
    public TerrainGeneratorManager terrainGeneratorManager;

    [Header("Available Configurations")]
    public TerrainGenerationSettings[] availableConfigs;

    #endregion

    #region Private Fields

    private TerrainGenerationSettings currentSettings;

    #endregion

    #region Unity Methods

    private void Start()
    {
        PopulateConfigDropdown();
        LoadDefaultValues();
        PopulateVoronoiDistributionDropdown();
        AddListeners();
    }

    #endregion

    #region Configuration Management

    private void PopulateConfigDropdown()
    {
        configDropdown.ClearOptions();
        configDropdown.AddOptions(availableConfigs.Select(config => config.name).ToList());
        configDropdown.value = 0;
        configDropdown.RefreshShownValue();
        configDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    private void PopulateVoronoiDistributionDropdown()
    {
        voronoiDistributionModeDropdown.ClearOptions();
        voronoiDistributionModeDropdown.AddOptions(System.Enum.GetNames(typeof(DistributionMode)).ToList());
        voronoiDistributionModeDropdown.value = 0;
        voronoiDistributionModeDropdown.RefreshShownValue();
    }

    private void OnDropdownValueChanged(int index)
    {
        if (index < 0 || index >= availableConfigs.Length)
        {
            DisplayError("Invalid configuration selected.");
            return;
        }

        LoadValuesFromConfig(availableConfigs[index]);
        ClearError();
    }

    private void LoadDefaultValues()
    {
        if (availableConfigs == null || availableConfigs.Length == 0)
        {
            DisplayError("No configurations available!");
            return;
        }

        LoadValuesFromConfig(availableConfigs[0]);
    }

    private void LoadValuesFromConfig(TerrainGenerationSettings config)
    {
        currentSettings = ScriptableObject.CreateInstance<TerrainGenerationSettings>();
        CopySettings(config, currentSettings);
        UpdateUIFieldsFromSettings(config);
    }

    private void UpdateUIFieldsFromSettings(TerrainGenerationSettings config)
    {
        SetField(usePerlinNoiseToggle, config.usePerlinNoise);
        SetField(perlinLayersField, config.perlinLayers.ToString());
        SetField(perlinBaseScaleField, config.perlinBaseScale.ToString());
        SetField(perlinAmplitudeDecayField, config.perlinAmplitudeDecay.ToString());
        SetField(perlinFrequencyGrowthField, config.perlinFrequencyGrowth.ToString());
        SetField(perlinOffsetXField, config.perlinOffset.x.ToString());
        SetField(perlinOffsetYField, config.perlinOffset.y.ToString());

        SetField(useFractalBrownianMotionToggle, config.useFractalBrownianMotion);
        SetField(fBmLayersField, config.fBmLayers.ToString());
        SetField(fBmBaseScaleField, config.fBmBaseScale.ToString());
        SetField(fBmAmplitudeDecayField, config.fBmAmplitudeDecay.ToString());
        SetField(fBmFrequencyGrowthField, config.fBmFrequencyGrowth.ToString());
        SetField(fBmOffsetXField, config.fBmOffset.x.ToString());
        SetField(fBmOffsetYField, config.fBmOffset.y.ToString());

        SetField(useMidPointDisplacementToggle, config.useMidPointDisplacement);
        SetField(displacementFactorField, config.displacementFactor.ToString());
        SetField(displacementDecayRateField, config.displacementDecayRate.ToString());
        SetField(randomSeedField, config.randomSeed.ToString());

        SetField(useVoronoiBiomesToggle, config.useVoronoiBiomes);
        SetField(voronoiCellCountField, config.voronoiCellCount.ToString());
        SetField(voronoiHeightRangeMinField, config.voronoiHeightRange.x.ToString());
        SetField(voronoiHeightRangeMaxField, config.voronoiHeightRange.y.ToString());

        voronoiDistributionModeDropdown.value = (int)config.voronoiDistributionMode;
        customVoronoiPointsField.text = string.Join(";", config.customVoronoiPoints.Select(p => $"{p.x},{p.y}"));
    }

    #endregion

    #region Input Field Validation

    private void AddListeners()
    {
        // Perlin Noise
        AddValidatedFieldListener(perlinLayersField, value => currentSettings.perlinLayers = int.Parse(value), 1, 100);
        AddValidatedFieldListener(perlinBaseScaleField, value => currentSettings.perlinBaseScale = float.Parse(value), 0.1f, 500f);
        AddValidatedFieldListener(perlinAmplitudeDecayField, value =>
        {
            currentSettings.perlinAmplitudeDecay = float.Parse(value);
            Debug.Log($"Updated perlinAmplitudeDecay: {currentSettings.perlinAmplitudeDecay}");
            RegenerateTerrain();
        }, 0f, 1f);

        AddValidatedFieldListener(perlinFrequencyGrowthField, value => currentSettings.perlinFrequencyGrowth = float.Parse(value), 0.1f, 10f);

        AddInputFieldListener(perlinOffsetXField, value =>
        {
            float offsetX = float.Parse(value);
            currentSettings.perlinOffset = new Vector2(offsetX, currentSettings.perlinOffset.y);
            RegenerateTerrain();
        });

        AddInputFieldListener(perlinOffsetYField, value =>
        {
            float offsetY = float.Parse(value);
            currentSettings.perlinOffset = new Vector2(currentSettings.perlinOffset.x, offsetY);
            RegenerateTerrain();
        });

        // fBm
        AddValidatedFieldListener(fBmLayersField, value =>
        {
            currentSettings.fBmLayers = int.Parse(value);
            Debug.Log($"Updated fBmLayers: {currentSettings.fBmLayers}");
            RegenerateTerrain();
        }, 1, 100);

        AddValidatedFieldListener(fBmBaseScaleField, value =>
        {
            currentSettings.fBmBaseScale = float.Parse(value);
            Debug.Log($"Updated fBmBaseScale: {currentSettings.fBmBaseScale}");
            RegenerateTerrain();
        }, 0.1f, 500f);

        AddValidatedFieldListener(fBmAmplitudeDecayField, value =>
        {
            currentSettings.fBmAmplitudeDecay = float.Parse(value);
            Debug.Log($"Updated fBmAmplitudeDecay: {currentSettings.fBmAmplitudeDecay}");
            RegenerateTerrain();
        }, 0f, 1f);

        AddInputFieldListener(fBmOffsetXField, value =>
        {
            float offsetX = float.Parse(value);
            currentSettings.fBmOffset = new Vector2(offsetX, currentSettings.fBmOffset.y);
            RegenerateTerrain();
        });

        AddValidatedFieldListener(fBmFrequencyGrowthField, value =>
        {
            currentSettings.fBmFrequencyGrowth = float.Parse(value);
            Debug.Log($"Updated fBmFrequencyGrowth: {currentSettings.fBmFrequencyGrowth}");
            RegenerateTerrain();
        }, 0.1f, 10f);

        AddInputFieldListener(fBmOffsetYField, value =>
        {
            float offsetY = float.Parse(value);
            currentSettings.fBmOffset = new Vector2(currentSettings.fBmOffset.x, offsetY);
            RegenerateTerrain();
        });

        // Midpoint Displacement
        AddValidatedFieldListener(displacementFactorField, value => currentSettings.displacementFactor = float.Parse(value), 0.1f, 10f);
        AddValidatedFieldListener(displacementDecayRateField, value => currentSettings.displacementDecayRate = float.Parse(value), 0f, 1f);
        AddValidatedFieldListener(randomSeedField, value => currentSettings.randomSeed = int.Parse(value), 0, 10000);

        // Voronoi Biomes
        AddValidatedFieldListener(voronoiCellCountField, value => currentSettings.voronoiCellCount = int.Parse(value), 1, 100);
        AddValidatedFieldListener(voronoiHeightRangeMinField, value => currentSettings.voronoiHeightRange.x = float.Parse(value), 0f, 1f);
        AddValidatedFieldListener(voronoiHeightRangeMaxField, value => currentSettings.voronoiHeightRange.y = float.Parse(value), 0f, 1f);

        AddInputFieldListener(customVoronoiPointsField, value =>
        {
            try
            {
                currentSettings.customVoronoiPoints = ParseCustomVoronoiPoints(value);
                ClearError();
                RegenerateTerrain();
            }
            catch
            {
                DisplayError("Invalid custom Voronoi points format. Use 'x1,y1;x2,y2' format.");
            }
        });

        AddDropdownListener(voronoiDistributionModeDropdown, value =>
        {
            currentSettings.voronoiDistributionMode = (TerrainGenerationSettings.DistributionMode)(int)(DistributionMode)value;
            RegenerateTerrain();
        });

        // Toggles
        AddFieldListener(usePerlinNoiseToggle, value => currentSettings.usePerlinNoise = value);
        AddFieldListener(useFractalBrownianMotionToggle, value => currentSettings.useFractalBrownianMotion = value);
        AddFieldListener(useMidPointDisplacementToggle, value => currentSettings.useMidPointDisplacement = value);
        AddFieldListener(useVoronoiBiomesToggle, value => currentSettings.useVoronoiBiomes = value);
    }


    private void AddValidatedFieldListener(TMP_InputField field, System.Action<string> onChanged, float min, float max)
    {
        field.onEndEdit.AddListener(value =>
        {
            if (float.TryParse(value, out float result) && result >= min && result <= max)
            {
                onChanged(value);
                ClearError();
                RegenerateTerrain();
            }
            else
            {
                DisplayError($"Invalid input for {field.name}. Must be between {min} and {max}.");
                field.text = min.ToString();
            }
        });
    }

    private void AddDropdownListener(TMP_Dropdown dropdown, System.Action<int> onChanged)
    {
        dropdown.onValueChanged.AddListener(value =>
        {
            onChanged(value);
            ClearError();
            RegenerateTerrain();
        });
    }

    /// <summary>
    /// Adds a listener to an input field to update settings and regenerate terrain.
    /// </summary>
    /// <param name="field">The input field to listen to.</param>
    /// <param name="onChanged">The action to execute when the field changes.</param>
    private void AddInputFieldListener(TMP_InputField field, System.Action<string> onChanged)
    {
        field.onEndEdit.AddListener(value =>
        {
            Debug.Log($"Input Changed: {field.name} = {value}");
            onChanged(value);
        });
    }


    private void AddFieldListener(Toggle toggle, System.Action<bool> onChanged)
    {
        toggle.onValueChanged.AddListener(value =>
        {
            onChanged(value);
            ClearError();
            RegenerateTerrain();
        });
    }

    #endregion

    #region Terrain Regeneration

    /// <summary>
    /// Regenerates the terrain using the current settings.
    /// </summary>
    private void RegenerateTerrain()
    {
        if (terrainGeneratorManager != null)
        {
            Debug.Log("Regenerating terrain with updated settings...");
            terrainGeneratorManager.terrainSettings = currentSettings;
            terrainGeneratorManager.GenerateTerrain();
        }
        else
        {
            Debug.LogError("TerrainGeneratorManager is null!");
        }
    }

    #endregion

    #region Error Handling

    public void DisplayError(string message)
    {
        errorMessage.text = message;
        errorMessage.gameObject.SetActive(true);
    }

    private void ClearError()
    {
        errorMessage.text = "";
        errorMessage.gameObject.SetActive(false);
    }

    #endregion

    #region Helper Methods

    private void SetField(TMP_InputField field, string value) => field.text = value;
    private void SetField(Toggle toggle, bool value) => toggle.isOn = value;

    private List<Vector2> ParseCustomVoronoiPoints(string value)
    {
        return value.Split(';')
                    .Select(pair => pair.Split(','))
                    .Where(coords => coords.Length == 2 &&
                                     float.TryParse(coords[0], out _) &&
                                     float.TryParse(coords[1], out _))
                    .Select(coords => new Vector2(float.Parse(coords[0]), float.Parse(coords[1])))
                    .ToList();
    }

    private void CopySettings(TerrainGenerationSettings source, TerrainGenerationSettings target)
    {
        target.usePerlinNoise = source.usePerlinNoise;
        target.perlinLayers = source.perlinLayers;
        target.perlinBaseScale = source.perlinBaseScale;
        target.perlinAmplitudeDecay = source.perlinAmplitudeDecay;
        target.perlinFrequencyGrowth = source.perlinFrequencyGrowth;
        target.perlinOffset = source.perlinOffset;

        target.useFractalBrownianMotion = source.useFractalBrownianMotion;
        target.fBmLayers = source.fBmLayers;
        target.fBmBaseScale = source.fBmBaseScale;
        target.fBmAmplitudeDecay = source.fBmAmplitudeDecay;
        target.fBmFrequencyGrowth = source.fBmFrequencyGrowth;
        target.fBmOffset = source.fBmOffset;

        target.useMidPointDisplacement = source.useMidPointDisplacement;
        target.displacementFactor = source.displacementFactor;
        target.displacementDecayRate = source.displacementDecayRate;
        target.randomSeed = source.randomSeed;

        target.useVoronoiBiomes = source.useVoronoiBiomes;
        target.voronoiCellCount = source.voronoiCellCount;
        target.voronoiHeightRange = source.voronoiHeightRange;
        target.voronoiDistributionMode = source.voronoiDistributionMode;
        target.customVoronoiPoints = new List<Vector2>(source.customVoronoiPoints);

        // Preserve texture mappings
        target.textureMappings = source.textureMappings?.ToArray();
    }


    #endregion
}
