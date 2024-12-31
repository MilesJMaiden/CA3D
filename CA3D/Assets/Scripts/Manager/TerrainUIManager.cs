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

    // Erosion
    public Toggle useErosionToggle;
    public TMP_InputField talusAngleField;
    public TMP_InputField erosionIterationsField;

    // Rivers
    public Toggle useRiversToggle;
    public TMP_InputField riverWidthField;
    public TMP_InputField riverHeightField;

    // Trails
    public Toggle useTrailsToggle;
    public TMP_InputField trailStartPointXField;
    public TMP_InputField trailStartPointYField;
    public TMP_InputField trailEndPointXField;
    public TMP_InputField trailEndPointYField;
    public TMP_InputField trailWidthField;
    public TMP_InputField trailRandomnessField;

    // Lakes
    public Toggle useLakesToggle;
    public TMP_InputField lakeCenterXField;
    public TMP_InputField lakeCenterYField;
    public TMP_InputField lakeRadiusField;
    public TMP_InputField lakeWaterLevelField;

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

    /// <summary>
    /// Populates the configuration dropdown with available terrain generation settings.
    /// </summary>
    private void PopulateConfigDropdown()
    {
        if (configDropdown == null)
        {
            Debug.LogError("ConfigDropdown reference is missing.");
            return;
        }

        if (availableConfigs == null || availableConfigs.Length == 0)
        {
            Debug.LogWarning("No configurations available to populate the dropdown.");
            return;
        }

        // Clear existing options and populate with available configurations
        configDropdown.ClearOptions();
        configDropdown.AddOptions(availableConfigs.Select(config => config.name).ToList());

        // Set default selection
        configDropdown.value = 0;
        configDropdown.RefreshShownValue();

        // Add event listener for when a new configuration is selected
        configDropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
        configDropdown.onValueChanged.AddListener(OnDropdownValueChanged);

        Debug.Log("Configuration dropdown successfully populated.");
    }


    /// <summary>
    /// Populates the Voronoi distribution mode dropdown with available options.
    /// </summary>
    private void PopulateVoronoiDistributionDropdown()
    {
        if (voronoiDistributionModeDropdown == null)
        {
            Debug.LogError("VoronoiDistributionModeDropdown reference is missing.");
            return;
        }

        // Fetch the enum names for distribution modes
        var distributionModes = System.Enum.GetNames(typeof(DistributionMode)).ToList();
        if (distributionModes == null || distributionModes.Count == 0)
        {
            Debug.LogWarning("No distribution modes found to populate the dropdown.");
            return;
        }

        // Clear existing options and populate dropdown
        voronoiDistributionModeDropdown.ClearOptions();
        voronoiDistributionModeDropdown.AddOptions(distributionModes);

        // Set default selection
        voronoiDistributionModeDropdown.value = 0;
        voronoiDistributionModeDropdown.RefreshShownValue();

        Debug.Log("Voronoi distribution mode dropdown successfully populated.");
    }

    /// <summary>
    /// Handles changes to the configuration dropdown and updates terrain settings.
    /// </summary>
    /// <param name="index">The selected dropdown index.</param>
    private void OnDropdownValueChanged(int index)
    {
        // Validate index
        if (!IsValidConfigIndex(index))
        {
            DisplayError($"Invalid configuration index selected: {index}");
            return;
        }

        // Load new settings from the selected configuration
        LoadValuesFromConfig(availableConfigs[index]);

        // Regenerate terrain and update layers
        if (RegenerateTerrainAndApplyLayers())
        {
            Debug.Log($"Configuration updated successfully: {availableConfigs[index].name}");
            ClearError();
        }
    }

    /// <summary>
    /// Validates whether the provided index is within the range of available configurations.
    /// </summary>
    /// <param name="index">The index to validate.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    private bool IsValidConfigIndex(int index)
    {
        return index >= 0 && index < availableConfigs.Length;
    }

    /// <summary>
    /// Regenerates the terrain and ensures terrain layers are applied.
    /// </summary>
    /// <returns>True if successful; otherwise, false.</returns>
    private bool RegenerateTerrainAndApplyLayers()
    {
        if (terrainGeneratorManager == null)
        {
            Debug.LogError("TerrainGeneratorManager is null. Cannot regenerate terrain or apply layers.");
            return false;
        }

        try
        {
            RegenerateTerrain();
            terrainGeneratorManager.ApplyTerrainLayers();
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error during terrain regeneration or layer application: {ex.Message}");
            DisplayError($"Error during terrain regeneration: {ex.Message}");
            return false;
        }
    }


    /// <summary>
    /// Loads the default configuration from the available configurations.
    /// </summary>
    private void LoadDefaultValues()
    {
        if (!HasAvailableConfigs())
        {
            DisplayError("No configurations available! Unable to load default settings.");
            Debug.LogError("LoadDefaultValues failed: No configurations found.");
            return;
        }

        Debug.Log($"Loading default configuration: {availableConfigs[0].name}");
        LoadValuesFromConfig(availableConfigs[0]);
    }

    /// <summary>
    /// Checks if there are available configurations.
    /// </summary>
    /// <returns>True if configurations are available; otherwise, false.</returns>
    private bool HasAvailableConfigs()
    {
        return availableConfigs != null && availableConfigs.Length > 0;
    }


    /// <summary>
    /// Loads values from the specified configuration and updates the current settings and UI fields.
    /// </summary>
    /// <param name="config">The configuration to load values from.</param>
    private void LoadValuesFromConfig(TerrainGenerationSettings config)
    {
        if (config == null)
        {
            DisplayError("Cannot load values: Provided configuration is null.");
            Debug.LogError("LoadValuesFromConfig failed: Config is null.");
            return;
        }

        Debug.Log($"Loading values from configuration: {config.name}");

        // Safely create and set the current settings
        currentSettings = ScriptableObject.CreateInstance<TerrainGenerationSettings>();
        CopySettings(config, currentSettings);

        // Update UI fields to reflect the loaded configuration
        UpdateUIFieldsFromSettings(config);
    }

    /// <summary>
    /// Updates all UI fields to reflect the provided configuration settings.
    /// </summary>
    /// <param name="config">The configuration whose settings will populate the UI fields.</param>
    private void UpdateUIFieldsFromSettings(TerrainGenerationSettings config)
    {
        if (config == null)
        {
            DisplayError("Cannot update UI fields: Configuration is null.");
            Debug.LogError("UpdateUIFieldsFromSettings failed: Config is null.");
            return;
        }

        Debug.Log("Updating UI fields to reflect current configuration...");

        // Perlin Noise Settings
        UpdatePerlinNoiseFields(config);

        // Fractal Brownian Motion (fBm) Settings
        UpdateFractalBrownianMotionFields(config);

        // Midpoint Displacement Settings
        UpdateMidpointDisplacementFields(config);

        // Voronoi Biomes Settings
        UpdateVoronoiBiomesFields(config);

        // Erosion
        UpdateErosionFields(config);

        // Rivers
        UpdateRiverFields(config);

        // Trails
        UpdateTrailFields(config);

        // Lakes
        UpdateLakeFields(config);
    }

    /// <summary>
    /// Updates UI fields related to Perlin Noise settings.
    /// </summary>
    private void UpdatePerlinNoiseFields(TerrainGenerationSettings config)
    {
        SetField(usePerlinNoiseToggle, config.usePerlinNoise);
        SetField(perlinLayersField, config.perlinLayers.ToString());
        SetField(perlinBaseScaleField, config.perlinBaseScale.ToString());
        SetField(perlinAmplitudeDecayField, config.perlinAmplitudeDecay.ToString());
        SetField(perlinFrequencyGrowthField, config.perlinFrequencyGrowth.ToString());
        SetField(perlinOffsetXField, config.perlinOffset.x.ToString());
        SetField(perlinOffsetYField, config.perlinOffset.y.ToString());
    }

    /// <summary>
    /// Updates UI fields related to Fractal Brownian Motion settings.
    /// </summary>
    private void UpdateFractalBrownianMotionFields(TerrainGenerationSettings config)
    {
        SetField(useFractalBrownianMotionToggle, config.useFractalBrownianMotion);
        SetField(fBmLayersField, config.fBmLayers.ToString());
        SetField(fBmBaseScaleField, config.fBmBaseScale.ToString());
        SetField(fBmAmplitudeDecayField, config.fBmAmplitudeDecay.ToString());
        SetField(fBmFrequencyGrowthField, config.fBmFrequencyGrowth.ToString());
        SetField(fBmOffsetXField, config.fBmOffset.x.ToString());
        SetField(fBmOffsetYField, config.fBmOffset.y.ToString());
    }

    /// <summary>
    /// Updates UI fields related to Midpoint Displacement settings.
    /// </summary>
    private void UpdateMidpointDisplacementFields(TerrainGenerationSettings config)
    {
        SetField(useMidPointDisplacementToggle, config.useMidPointDisplacement);
        SetField(displacementFactorField, config.displacementFactor.ToString());
        SetField(displacementDecayRateField, config.displacementDecayRate.ToString());
        SetField(randomSeedField, config.randomSeed.ToString());
    }

    /// <summary>
    /// Updates UI fields related to Voronoi Biomes settings.
    /// </summary>
    private void UpdateVoronoiBiomesFields(TerrainGenerationSettings config)
    {
        SetField(useVoronoiBiomesToggle, config.useVoronoiBiomes);
        SetField(voronoiCellCountField, config.voronoiCellCount.ToString());
        SetField(voronoiHeightRangeMinField, config.voronoiHeightRange.x.ToString());
        SetField(voronoiHeightRangeMaxField, config.voronoiHeightRange.y.ToString());

        voronoiDistributionModeDropdown.value = (int)config.voronoiDistributionMode;
        customVoronoiPointsField.text = string.Join(";", config.customVoronoiPoints.Select(p => $"{p.x},{p.y}"));
    }

    /// <summary>
    /// Updates UI fields related to Erosion settings.
    /// </summary>
    private void UpdateErosionFields(TerrainGenerationSettings config)
    {
        SetField(useErosionToggle, config.useErosion);
        SetField(talusAngleField, config.talusAngle.ToString());
        SetField(erosionIterationsField, config.erosionIterations.ToString());
    }

    /// <summary>
    /// Updates UI fields related to River settings.
    /// </summary>
    private void UpdateRiverFields(TerrainGenerationSettings config)
    {
        SetField(useRiversToggle, config.useRivers);
        SetField(riverWidthField, config.riverWidth.ToString());
        SetField(riverHeightField, config.riverHeight.ToString());
    }

    /// <summary>
    /// Updates UI fields related to Trail settings.
    /// </summary>
    private void UpdateTrailFields(TerrainGenerationSettings config)
    {
        SetField(useTrailsToggle, config.useTrails);
        SetField(trailStartPointXField, config.trailStartPoint.x.ToString());
        SetField(trailStartPointYField, config.trailStartPoint.y.ToString());
        SetField(trailEndPointXField, config.trailEndPoint.x.ToString());
        SetField(trailEndPointYField, config.trailEndPoint.y.ToString());
        SetField(trailWidthField, config.trailWidth.ToString());
        SetField(trailRandomnessField, config.trailRandomness.ToString());
    }

    /// <summary>
    /// Updates UI fields related to Lake settings.
    /// </summary>
    private void UpdateLakeFields(TerrainGenerationSettings config)
    {
        SetField(useLakesToggle, config.useLakes);
        SetField(lakeCenterXField, config.lakeCenter.x.ToString());
        SetField(lakeCenterYField, config.lakeCenter.y.ToString());
        SetField(lakeRadiusField, config.lakeRadius.ToString());
        SetField(lakeWaterLevelField, config.lakeWaterLevel.ToString());
    }


    #endregion

    #region Input Field Validation

    /// <summary>
    /// Adds listeners to all UI input fields and toggles to dynamically update terrain settings.
    /// Ensures terrain regeneration on any toggle change.
    /// </summary>
    private void AddListeners()
    {
        Debug.Log("Adding listeners to all UI components...");

        // Add listeners for each category of settings
        AddPerlinNoiseListeners();
        AddFractalBrownianMotionListeners();
        AddMidpointDisplacementListeners();
        AddVoronoiBiomesListeners();
        AddErosionListeners();
        AddRiverListeners();
        AddTrailListeners();
        AddLakeListeners();

        // Add listeners for all toggles to ensure terrain updates
        AddFieldListener(usePerlinNoiseToggle, value => currentSettings.usePerlinNoise = value);
        AddFieldListener(useFractalBrownianMotionToggle, value => currentSettings.useFractalBrownianMotion = value);
        AddFieldListener(useMidPointDisplacementToggle, value => currentSettings.useMidPointDisplacement = value);
        AddFieldListener(useVoronoiBiomesToggle, value => currentSettings.useVoronoiBiomes = value);
        AddFieldListener(useErosionToggle, value => currentSettings.useErosion = value);
        AddFieldListener(useRiversToggle, value => currentSettings.useRivers = value);
        AddFieldListener(useTrailsToggle, value => currentSettings.useTrails = value);
        AddFieldListener(useLakesToggle, value => currentSettings.useLakes = value);

        Debug.Log("Listeners successfully added to all UI components.");
    }


    /// <summary>
    /// Adds listeners for Perlin Noise UI fields.
    /// </summary>
    private void AddPerlinNoiseListeners()
    {
        AddValidatedFieldListener(perlinLayersField, value =>
        {
            currentSettings.perlinLayers = int.Parse(value);
        }, 1, 100);

        AddValidatedFieldListener(perlinBaseScaleField, value =>
        {
            currentSettings.perlinBaseScale = float.Parse(value);
        }, 0.1f, 500f);

        AddValidatedFieldListener(perlinAmplitudeDecayField, value =>
        {
            currentSettings.perlinAmplitudeDecay = float.Parse(value);
        }, 0f, 1f);

        AddValidatedFieldListener(perlinFrequencyGrowthField, value =>
        {
            currentSettings.perlinFrequencyGrowth = float.Parse(value);
        }, 0.1f, 10f);

        AddInputFieldListener(perlinOffsetXField, value =>
        {
            if (float.TryParse(value, out float offsetX))
            {
                currentSettings.perlinOffset = new Vector2(offsetX, currentSettings.perlinOffset.y);
                ClearError();
                RegenerateTerrain();
            }
            else
            {
                DisplayError($"Invalid input for {perlinOffsetXField.name}.");
            }
        });

        AddInputFieldListener(perlinOffsetYField, value =>
        {
            if (float.TryParse(value, out float offsetY))
            {
                currentSettings.perlinOffset = new Vector2(currentSettings.perlinOffset.x, offsetY);
                ClearError();
                RegenerateTerrain();
            }
            else
            {
                DisplayError($"Invalid input for {perlinOffsetYField.name}.");
            }
        });
    }

    /// <summary>
    /// Adds listeners for Fractal Brownian Motion UI fields.
    /// </summary>
    private void AddFractalBrownianMotionListeners()
    {
        AddValidatedFieldListener(fBmLayersField, value =>
        {
            currentSettings.fBmLayers = int.Parse(value);
        }, 1, 100);

        AddValidatedFieldListener(fBmBaseScaleField, value =>
        {
            currentSettings.fBmBaseScale = float.Parse(value);
        }, 0.1f, 500f);

        AddValidatedFieldListener(fBmAmplitudeDecayField, value =>
        {
            currentSettings.fBmAmplitudeDecay = float.Parse(value);
        }, 0f, 1f);

        AddValidatedFieldListener(fBmFrequencyGrowthField, value =>
        {
            currentSettings.fBmFrequencyGrowth = float.Parse(value);
        }, 0.1f, 10f);

        AddInputFieldListener(fBmOffsetXField, value =>
        {
            if (float.TryParse(value, out float offsetX))
            {
                currentSettings.fBmOffset = new Vector2(offsetX, currentSettings.fBmOffset.y);
                ClearError();
                RegenerateTerrain();
            }
            else
            {
                DisplayError($"Invalid input for {fBmOffsetXField.name}.");
            }
        });

        AddInputFieldListener(fBmOffsetYField, value =>
        {
            if (float.TryParse(value, out float offsetY))
            {
                currentSettings.fBmOffset = new Vector2(currentSettings.fBmOffset.x, offsetY);
                ClearError();
                RegenerateTerrain();
            }
            else
            {
                DisplayError($"Invalid input for {fBmOffsetYField.name}.");
            }
        });
    }

    /// <summary>
    /// Adds listeners for Midpoint Displacement UI fields.
    /// </summary>
    private void AddMidpointDisplacementListeners()
    {
        AddValidatedFieldListener(displacementFactorField, value =>
        {
            currentSettings.displacementFactor = float.Parse(value);
        }, 0.1f, 10f);

        AddValidatedFieldListener(displacementDecayRateField, value =>
        {
            currentSettings.displacementDecayRate = float.Parse(value);
        }, 0f, 1f);

        AddValidatedFieldListener(randomSeedField, value =>
        {
            currentSettings.randomSeed = int.Parse(value);
        }, 0, 10000);
    }

    /// <summary>
    /// Adds listeners for Voronoi Biomes UI fields.
    /// </summary>
    private void AddVoronoiBiomesListeners()
    {
        AddValidatedFieldListener(voronoiCellCountField, value =>
        {
            currentSettings.voronoiCellCount = int.Parse(value);
        }, 1, 100);

        AddValidatedFieldListener(voronoiHeightRangeMinField, value =>
        {
            currentSettings.voronoiHeightRange.x = float.Parse(value);
        }, 0f, 1f);

        AddValidatedFieldListener(voronoiHeightRangeMaxField, value =>
        {
            currentSettings.voronoiHeightRange.y = float.Parse(value);
        }, 0f, 1f);

        AddDropdownListener(voronoiDistributionModeDropdown, value =>
        {
            currentSettings.voronoiDistributionMode = (TerrainGenerationSettings.DistributionMode)value;
            ClearError();
            RegenerateTerrain();
        });

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
    }

    /// <summary>
    /// Adds listeners for Erosion UI fields.
    /// </summary>
    private void AddErosionListeners()
    {
        AddFieldListener(useErosionToggle, value =>
        {
            currentSettings.useErosion = value;
            ClearError();
            RegenerateTerrain();
        });

        AddValidatedFieldListener(talusAngleField, value =>
        {
            currentSettings.talusAngle = float.Parse(value);
        }, 0.01f, 0.2f);

        AddValidatedFieldListener(erosionIterationsField, value =>
        {
            currentSettings.erosionIterations = int.Parse(value);
        }, 1, 10);
    }

    /// <summary>
    /// Adds listeners for River UI fields.
    /// </summary>
    private void AddRiverListeners()
    {
        AddFieldListener(useRiversToggle, value =>
        {
            currentSettings.useRivers = value;
            ClearError();
            RegenerateTerrain();
        });

        AddValidatedFieldListener(riverWidthField, value =>
        {
            currentSettings.riverWidth = float.Parse(value);
        }, 1f, 20f);

        AddValidatedFieldListener(riverHeightField, value =>
        {
            currentSettings.riverHeight = float.Parse(value);
        }, 0f, 1f);
    }

    /// <summary>
    /// Adds listeners for Trail UI fields.
    /// </summary>
    private void AddTrailListeners()
    {
        AddFieldListener(useTrailsToggle, value =>
        {
            currentSettings.useTrails = value;
            ClearError();
            RegenerateTerrain();
        });

        AddInputFieldListener(trailStartPointXField, value =>
        {
            currentSettings.trailStartPoint = new Vector2(float.Parse(value), currentSettings.trailStartPoint.y);
        });

        AddInputFieldListener(trailStartPointYField, value =>
        {
            currentSettings.trailStartPoint = new Vector2(currentSettings.trailStartPoint.x, float.Parse(value));
        });

        AddInputFieldListener(trailEndPointXField, value =>
        {
            currentSettings.trailEndPoint = new Vector2(float.Parse(value), currentSettings.trailEndPoint.y);
        });

        AddInputFieldListener(trailEndPointYField, value =>
        {
            currentSettings.trailEndPoint = new Vector2(currentSettings.trailEndPoint.x, float.Parse(value));
        });

        AddValidatedFieldListener(trailWidthField, value =>
        {
            currentSettings.trailWidth = float.Parse(value);
        }, 1f, 50f);

        AddValidatedFieldListener(trailRandomnessField, value =>
        {
            currentSettings.trailRandomness = float.Parse(value);
        }, 0f, 5f);
    }

    /// <summary>
    /// Adds listeners for Lake UI fields.
    /// </summary>
    private void AddLakeListeners()
    {
        AddFieldListener(useLakesToggle, value =>
        {
            currentSettings.useLakes = value;
            ClearError();
            RegenerateTerrain();
        });

        AddInputFieldListener(lakeCenterXField, value =>
        {
            currentSettings.lakeCenter = new Vector2(float.Parse(value), currentSettings.lakeCenter.y);
        });

        AddInputFieldListener(lakeCenterYField, value =>
        {
            currentSettings.lakeCenter = new Vector2(currentSettings.lakeCenter.x, float.Parse(value));
        });

        AddValidatedFieldListener(lakeRadiusField, value =>
        {
            currentSettings.lakeRadius = float.Parse(value);
        }, 1f, 50f);

        AddValidatedFieldListener(lakeWaterLevelField, value =>
        {
            currentSettings.lakeWaterLevel = float.Parse(value);
        }, 0f, 1f);
    }

    /// <summary>
    /// Adds a listener to a TMP_InputField to validate and update its value.
    /// </summary>
    /// <param name="field">The TMP_InputField to listen to.</param>
    /// <param name="onChanged">Action to execute when a valid value is entered.</param>
    /// <param name="min">The minimum allowable value.</param>
    /// <param name="max">The maximum allowable value.</param>
    private void AddValidatedFieldListener(TMP_InputField field, System.Action<string> onChanged, float min, float max)
    {
        if (field == null)
        {
            Debug.LogError("Cannot add listener to a null TMP_InputField.");
            return;
        }

        field.onEndEdit.AddListener(value =>
        {
            try
            {
                if (float.TryParse(value, out float result) && result >= min && result <= max)
                {
                    onChanged?.Invoke(value);
                    ClearError();
                    RegenerateTerrain();
                    Debug.Log($"TMP_InputField '{field.name}' validated with value: {result}");
                }
                else
                {
                    string errorMsg = $"Invalid input for '{field.name}'. Must be between {min} and {max}.";
                    DisplayError(errorMsg);
                    Debug.LogWarning(errorMsg);
                    field.text = Mathf.Clamp(result, min, max).ToString(); // Clamp to valid range
                }
            }
            catch (System.Exception ex)
            {
                DisplayError($"Error processing input for '{field.name}': {ex.Message}");
                Debug.LogError(ex);
            }
        });
    }

    /// <summary>
    /// Adds a listener to a TMP_Dropdown to handle selection changes.
    /// </summary>
    /// <param name="dropdown">The TMP_Dropdown to listen to.</param>
    /// <param name="onChanged">Action to execute when the selection changes.</param>
    private void AddDropdownListener(TMP_Dropdown dropdown, System.Action<int> onChanged)
    {
        if (dropdown == null)
        {
            Debug.LogError("Cannot add listener to a null TMP_Dropdown.");
            return;
        }

        dropdown.onValueChanged.AddListener(value =>
        {
            try
            {
                onChanged?.Invoke(value);
                ClearError();
                RegenerateTerrain();
                Debug.Log($"TMP_Dropdown '{dropdown.name}' changed to option index: {value}");
            }
            catch (System.Exception ex)
            {
                DisplayError($"Error updating Dropdown '{dropdown.name}': {ex.Message}");
                Debug.LogError(ex);
            }
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

    /// <summary>
    /// Adds a listener to a Toggle to handle value changes and regenerate terrain.
    /// </summary>
    /// <param name="toggle">The Toggle to listen to.</param>
    /// <param name="onChanged">Action to execute when the value changes.</param>
    private void AddFieldListener(Toggle toggle, System.Action<bool> onChanged)
    {
        if (toggle == null)
        {
            Debug.LogError("Cannot add listener to a null Toggle.");
            return;
        }

        toggle.onValueChanged.AddListener(value =>
        {
            try
            {
                // Update the setting associated with the toggle
                onChanged?.Invoke(value);

                // Ensure the terrain settings are synchronized and regenerate the terrain
                if (terrainGeneratorManager != null && currentSettings != null)
                {
                    terrainGeneratorManager.terrainSettings = currentSettings;
                    RegenerateTerrain();
                    Debug.Log($"Terrain updated after toggling '{toggle.name}' to: {value}");
                }
                else
                {
                    Debug.LogError("TerrainGeneratorManager or currentSettings is null. Cannot update terrain.");
                }

                ClearError();
            }
            catch (System.Exception ex)
            {
                string errorMsg = $"Error updating Toggle '{toggle.name}': {ex.Message}";
                DisplayError(errorMsg);
                Debug.LogError(ex);
            }
        });
    }


    #endregion

    #region Terrain Regeneration

    /// <summary>
    /// Regenerates the terrain using the current settings.
    /// Ensures all dependencies are correctly assigned before attempting regeneration.
    /// </summary>
    private void RegenerateTerrain()
    {
        if (terrainGeneratorManager == null)
        {
            DisplayError("TerrainGeneratorManager is not set!");
            Debug.LogError("Cannot regenerate terrain: TerrainGeneratorManager reference is missing.");
            return;
        }

        if (currentSettings == null)
        {
            DisplayError("Current terrain settings are not initialized!");
            Debug.LogError("Cannot regenerate terrain: Current settings are null.");
            return;
        }

        try
        {
            Debug.Log("Regenerating terrain with updated settings...");

            // Assign the updated settings to the generator manager
            terrainGeneratorManager.terrainSettings = currentSettings;

            // Generate the terrain
            terrainGeneratorManager.GenerateTerrain();

            // Apply terrain layers for updated visual representation
            terrainGeneratorManager.ApplyTerrainLayers();

            // Clear any errors after successful regeneration
            ClearError();

            Debug.Log("Terrain regeneration completed successfully.");
        }
        catch (System.Exception ex)
        {
            // Log and display any exceptions encountered during regeneration
            string errorMessage = $"Error during terrain regeneration: {ex.Message}";
            DisplayError(errorMessage);
            Debug.LogError(ex);
        }
    }

    #endregion

    #region Error Handling

    /// <summary>
    /// Displays an error message on the UI and logs it to the console.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    public void DisplayError(string message)
    {
        if (errorMessage == null)
        {
            Debug.LogError($"Error message UI reference is missing! Cannot display the following error: {message}");
            return;
        }

        errorMessage.text = message;
        errorMessage.gameObject.SetActive(true);
        Debug.LogError($"UI Error Displayed: {message}");
    }

    /// <summary>
    /// Clears the error message from the UI and hides the error display.
    /// </summary>
    private void ClearError()
    {
        if (errorMessage == null)
        {
            Debug.LogWarning("Error message UI reference is missing! Nothing to clear.");
            return;
        }

        errorMessage.text = string.Empty;
        errorMessage.gameObject.SetActive(false);
        Debug.Log("Error message cleared.");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Sets the value of a TMP_InputField UI element.
    /// </summary>
    /// <param name="field">The TMP_InputField to update.</param>
    /// <param name="value">The value to set in the input field.</param>
    private void SetField(TMP_InputField field, string value)
    {
        if (field == null)
        {
            Debug.LogWarning($"Attempted to set value on a null input field. Value: {value}");
            return;
        }

        field.text = value;
        Debug.Log($"Set field {field.name} to value: {value}");
    }

    /// <summary>
    /// Sets the value of a Toggle UI element.
    /// </summary>
    /// <param name="toggle">The Toggle to update.</param>
    /// <param name="value">The value to set in the toggle.</param>
    private void SetField(Toggle toggle, bool value)
    {
        if (toggle == null)
        {
            Debug.LogWarning($"Attempted to set value on a null toggle. Value: {value}");
            return;
        }

        toggle.isOn = value;
        Debug.Log($"Set toggle {toggle.name} to value: {value}");
    }

    /// <summary>
    /// Parses a semicolon-separated string of Voronoi points into a list of Vector2 values.
    /// </summary>
    /// <param name="value">The string containing Voronoi points in "x1,y1;x2,y2" format.</param>
    /// <returns>A list of parsed Vector2 points.</returns>
    private List<Vector2> ParseCustomVoronoiPoints(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Debug.LogWarning("Input for custom Voronoi points is empty or null. Returning an empty list.");
            return new List<Vector2>();
        }

        var points = new List<Vector2>();

        foreach (string pair in value.Split(';'))
        {
            var coords = pair.Split(',');

            if (coords.Length != 2)
            {
                Debug.LogWarning($"Invalid point format: {pair}. Skipping.");
                continue;
            }

            if (float.TryParse(coords[0], out float x) && float.TryParse(coords[1], out float y))
            {
                points.Add(new Vector2(x, y));
            }
            else
            {
                Debug.LogWarning($"Failed to parse coordinates: {pair}. Skipping.");
            }
        }

        Debug.Log($"Parsed {points.Count} custom Voronoi points.");
        return points;
    }

    /// <summary>
    /// Copies terrain generation settings from a source to a target ScriptableObject instance.
    /// </summary>
    /// <param name="source">The source settings to copy from.</param>
    /// <param name="target">The target settings to copy to.</param>
    private void CopySettings(TerrainGenerationSettings source, TerrainGenerationSettings target)
    {
        if (source == null || target == null)
        {
            Debug.LogError("Source or target settings are null. Copy operation aborted.");
            return;
        }

        // Perlin Noise Settings
        target.usePerlinNoise = source.usePerlinNoise;
        target.perlinLayers = source.perlinLayers;
        target.perlinBaseScale = source.perlinBaseScale;
        target.perlinAmplitudeDecay = source.perlinAmplitudeDecay;
        target.perlinFrequencyGrowth = source.perlinFrequencyGrowth;
        target.perlinOffset = source.perlinOffset;

        // Fractal Brownian Motion (fBm) Settings
        target.useFractalBrownianMotion = source.useFractalBrownianMotion;
        target.fBmLayers = source.fBmLayers;
        target.fBmBaseScale = source.fBmBaseScale;
        target.fBmAmplitudeDecay = source.fBmAmplitudeDecay;
        target.fBmFrequencyGrowth = source.fBmFrequencyGrowth;
        target.fBmOffset = source.fBmOffset;

        // Midpoint Displacement Settings
        target.useMidPointDisplacement = source.useMidPointDisplacement;
        target.displacementFactor = source.displacementFactor;
        target.displacementDecayRate = source.displacementDecayRate;
        target.randomSeed = source.randomSeed;

        // Voronoi Biomes Settings
        target.useVoronoiBiomes = source.useVoronoiBiomes;
        target.voronoiCellCount = source.voronoiCellCount;
        target.voronoiHeightRange = source.voronoiHeightRange;
        target.voronoiDistributionMode = source.voronoiDistributionMode;
        target.customVoronoiPoints = new List<Vector2>(source.customVoronoiPoints); // Deep copy of list
        target.voronoiBlendFactor = source.voronoiBlendFactor;

        // Rivers
        target.useRivers = source.useRivers;
        target.riverWidth = source.riverWidth;
        target.riverHeight = source.riverHeight;

        // Trails
        target.useTrails = source.useTrails;
        target.trailStartPoint = source.trailStartPoint;
        target.trailEndPoint = source.trailEndPoint;
        target.trailWidth = source.trailWidth;
        target.trailRandomness = source.trailRandomness;

        // Lakes
        target.useLakes = source.useLakes;
        target.lakeCenter = source.lakeCenter;
        target.lakeRadius = source.lakeRadius;
        target.lakeWaterLevel = source.lakeWaterLevel;

        // Erosion
        target.useErosion = source.useErosion;
        target.talusAngle = source.talusAngle;
        target.erosionIterations = source.erosionIterations;

        // Texture Mappings
        if (source.textureMappings != null)
        {
            target.textureMappings = source.textureMappings.ToArray(); // Deep copy of array
        }
        else
        {
            target.textureMappings = null;
        }

        Debug.Log("Settings successfully copied from source to target.");
    }

    #endregion
}
