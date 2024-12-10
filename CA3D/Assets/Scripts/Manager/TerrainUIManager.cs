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
        // This would require exposing more elements via UI I.e. Vector 2 list
        //Custom
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

    public TMP_Text errorMessage; // TMP_Text for displaying errors

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

    /// <summary>
    /// Populates the configuration dropdown with available settings and sets up a listener for selection changes.
    /// </summary>
    private void PopulateConfigDropdown()
    {
        configDropdown.ClearOptions();

        foreach (var config in availableConfigs)
        {
            configDropdown.options.Add(new TMP_Dropdown.OptionData(config.name));
        }

        configDropdown.value = 0;
        configDropdown.RefreshShownValue();

        // Add listener to handle selection changes
        configDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    /// <summary>
    /// Populates the Voronoi distribution dropdown with options from the DistributionMode enum.
    /// </summary>
    private void PopulateVoronoiDistributionDropdown()
    {
        voronoiDistributionModeDropdown.ClearOptions();

        foreach (var mode in System.Enum.GetNames(typeof(DistributionMode)))
        {
            voronoiDistributionModeDropdown.options.Add(new TMP_Dropdown.OptionData(mode));
        }

        voronoiDistributionModeDropdown.value = 0;
        voronoiDistributionModeDropdown.RefreshShownValue();
    }



    /// <summary>
    /// Handles dropdown selection changes by loading the selected configuration.
    /// </summary>
    /// <param name="index">The selected index of the dropdown.</param>
    private void OnDropdownValueChanged(int index)
    {
        if (index < 0 || index >= availableConfigs.Length)
        {
            DisplayError("Invalid configuration selected.");
            return;
        }

        // Load the selected configuration
        LoadValuesFromConfig(availableConfigs[index]);

        // Clear any error messages
        ClearError();
    }

    /// <summary>
    /// Loads the default values from the first configuration.
    /// </summary>
    private void LoadDefaultValues()
    {
        if (availableConfigs == null || availableConfigs.Length == 0)
        {
            DisplayError("No configurations available!");
            return;
        }

        LoadValuesFromConfig(availableConfigs[0]);
    }

    /// <summary>
    /// Loads values from a specified configuration into the UI fields and settings.
    /// </summary>
    /// <param name="config">The configuration to load.</param>
    private void LoadValuesFromConfig(TerrainGenerationSettings config)
    {
        currentSettings = ScriptableObject.CreateInstance<TerrainGenerationSettings>();
        CopySettings(config, currentSettings);

        // Update UI fields
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

    /// <summary>
    /// Adds listeners to all UI elements for validation and regeneration.
    /// </summary>
    private void AddListeners()
    {
        AddValidatedFieldListener(perlinLayersField, value => currentSettings.perlinLayers = int.Parse(value), 1, 100);
        AddValidatedFieldListener(perlinBaseScaleField, value => currentSettings.perlinBaseScale = float.Parse(value), 0.1f, 500f);
        AddValidatedFieldListener(perlinAmplitudeDecayField, value => currentSettings.perlinAmplitudeDecay = float.Parse(value), 0f, 1f);
        AddValidatedFieldListener(perlinFrequencyGrowthField, value => currentSettings.perlinFrequencyGrowth = float.Parse(value), 0.1f, 10f);

        AddValidatedFieldListener(fBmLayersField, value => currentSettings.fBmLayers = int.Parse(value), 1, 100);
        AddValidatedFieldListener(fBmBaseScaleField, value => currentSettings.fBmBaseScale = float.Parse(value), 0.1f, 500f);

        AddValidatedFieldListener(displacementFactorField, value => currentSettings.displacementFactor = float.Parse(value), 0.1f, 10f);
        AddValidatedFieldListener(displacementDecayRateField, value => currentSettings.displacementDecayRate = float.Parse(value), 0f, 1f);
        AddValidatedFieldListener(randomSeedField, value => currentSettings.randomSeed = int.Parse(value), 0, 10000);

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
                DisplayError("Invalid custom Voronoi points format. Use 'x1,y1;x2,y2'.");
            }
        });

        AddDropdownListener(voronoiDistributionModeDropdown, value =>
        {
            currentSettings.voronoiDistributionMode = (TerrainGenerationSettings.DistributionMode)(int)(DistributionMode)value;
            RegenerateTerrain();
        });

        AddFieldListener(usePerlinNoiseToggle, value => currentSettings.usePerlinNoise = value);
        AddFieldListener(useFractalBrownianMotionToggle, value => currentSettings.useFractalBrownianMotion = value);
        AddFieldListener(useMidPointDisplacementToggle, value => currentSettings.useMidPointDisplacement = value);
        AddFieldListener(useVoronoiBiomesToggle, value => currentSettings.useVoronoiBiomes = value);
    }
    /// <summary>
    /// Adds validation logic to input fields, ensuring valid input and terrain regeneration.
    /// </summary>
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
                field.text = min.ToString(); // Reset to minimum valid value
            }
        });
    }


    /// <summary>
    /// Adds a listener to a dropdown field to update settings and regenerate terrain.
    /// </summary>
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
    private void AddInputFieldListener(TMP_InputField field, System.Action<string> onChanged)
    {
        field.onEndEdit.AddListener(value =>
        {
            onChanged(value);
        });
    }

    /// <summary>
    /// Adds a listener to a toggle field to update settings and regenerate terrain.
    /// </summary>
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
    /// Regenerates the terrain with the current settings.
    /// </summary>
    private void RegenerateTerrain()
    {
        terrainGeneratorManager.terrainSettings = currentSettings;
        terrainGeneratorManager.GenerateTerrain();
    }

    #endregion

    #region Error Handling

    /// <summary>
    /// Displays an error message in the UI.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    public void DisplayError(string message)
    {
        errorMessage.text = message;
        errorMessage.gameObject.SetActive(true);
    }

    /// <summary>
    /// Clears any error messages in the UI.
    /// </summary>
    private void ClearError()
    {
        errorMessage.text = "";
        errorMessage.gameObject.SetActive(false);
    }

    #endregion

    #region Helper Methods

    private void SetField(TMP_InputField field, string value) => field.text = value;
    private void SetField(Toggle toggle, bool value) => toggle.isOn = value;

    /// <summary>
    /// Parses a semicolon-separated list of custom Voronoi points into a list of Vector2.
    /// </summary>
    private List<Vector2> ParseCustomVoronoiPoints(string value)
    {
        var points = new List<Vector2>();
        var pairs = value.Split(';');
        foreach (var pair in pairs)
        {
            var coords = pair.Split(',');
            if (coords.Length == 2 && float.TryParse(coords[0], out float x) && float.TryParse(coords[1], out float y))
            {
                points.Add(new Vector2(x, y));
            }
        }
        return points;
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

    }

    #endregion
}
