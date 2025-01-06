// TerrainUIManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Manages the UI for adjusting terrain generation settings and regenerates the terrain dynamically
/// whenever valid inputs are modified.
/// Also provides controls for adjusting feature placement parameters (e.g., CA Iterations, Neighbor Threshold).
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
    public TMP_Dropdown voronoiDistributionModeDropdown;
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

    [Header("Feature Toggles")]
    public GameObject featureToggleContainer;
    public GameObject togglePrefab;
    private List<Toggle> featureToggles = new List<Toggle>();

    [Header("Feature Placement Controls")]
    [Tooltip("Number of iterations for the Cellular Automata pass on features.")]
    public TMP_InputField featureCAIterationsField;

    [Tooltip("Neighbor threshold for Cellular Automata pass.")]
    public TMP_InputField featureNeighborThresholdField;

    [Tooltip("Global multiplier for spawn probability (applies to all features).")]
    public TMP_InputField globalFeatureDensityField;

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

        configDropdown.ClearOptions();
        configDropdown.AddOptions(availableConfigs.Select(config => config.name).ToList());

        configDropdown.value = 0;
        configDropdown.RefreshShownValue();

        configDropdown.onValueChanged.RemoveListener(OnConfigDropdownChanged);
        configDropdown.onValueChanged.AddListener(OnConfigDropdownChanged);
    }

    private void PopulateVoronoiDistributionDropdown()
    {
        if (voronoiDistributionModeDropdown == null)
        {
            Debug.LogError("VoronoiDistributionModeDropdown reference is missing.");
            return;
        }

        var distributionModes = Enum.GetNames(typeof(DistributionMode)).ToList();
        if (distributionModes == null || distributionModes.Count == 0)
        {
            Debug.LogWarning("No distribution modes found to populate the dropdown.");
            return;
        }

        voronoiDistributionModeDropdown.ClearOptions();
        voronoiDistributionModeDropdown.AddOptions(distributionModes);

        voronoiDistributionModeDropdown.value = 0;
        voronoiDistributionModeDropdown.RefreshShownValue();

        Debug.Log("Voronoi distribution mode dropdown successfully populated.");
    }

    private void OnConfigDropdownChanged(int index)
    {
        if (!IsValidConfigIndex(index))
        {
            DisplayError($"Invalid configuration index selected: {index}");
            return;
        }

        LoadValuesFromConfig(availableConfigs[index]);
        UpdateUIFieldsFromSettings(currentSettings);
    }

    private void PopulateFeatureToggles()
    {
        if (featureToggleContainer == null || togglePrefab == null)
        {
            Debug.LogError("FeatureToggleContainer or TogglePrefab is not assigned.");
            return;
        }

        foreach (Transform child in featureToggleContainer.transform)
        {
            Destroy(child.gameObject);
        }

        if (currentSettings.featureSettings == null) return;

        foreach (var feature in currentSettings.featureSettings)
        {
            GameObject toggleObj = Instantiate(togglePrefab, featureToggleContainer.transform);
            Toggle toggle = toggleObj.GetComponent<Toggle>();
            TextMeshProUGUI label = toggleObj.GetComponentInChildren<TextMeshProUGUI>();

            if (label != null)
            {
                label.text = feature.featureName;
            }

            toggle.isOn = feature.enabled;
            toggle.onValueChanged.AddListener(value =>
            {
                feature.enabled = value;
                // Force a terrain regeneration to refresh features
                RegenerateTerrain();
            });
        }
    }

    private bool IsValidConfigIndex(int index)
    {
        return index >= 0 && index < availableConfigs.Length;
    }

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

    private bool HasAvailableConfigs()
    {
        return availableConfigs != null && availableConfigs.Length > 0;
    }

    private void LoadValuesFromConfig(TerrainGenerationSettings config)
    {
        if (config == null) return;

        currentSettings = ScriptableObject.CreateInstance<TerrainGenerationSettings>();
        CopySettings(config, currentSettings);

        UpdateUIFieldsFromSettings(config);
        PopulateFeatureToggles();
    }

    private void UpdateUIFieldsFromSettings(TerrainGenerationSettings config)
    {
        if (config == null)
        {
            DisplayError("Cannot update UI fields: Configuration is null.");
            return;
        }

        Debug.Log("Updating UI fields to reflect current configuration...");

        UpdatePerlinNoiseFields(config);
        UpdateFractalBrownianMotionFields(config);
        UpdateMidpointDisplacementFields(config);
        UpdateVoronoiBiomesFields(config);
        UpdateErosionFields(config);
        UpdateRiverFields(config);
        UpdateTrailFields(config);
        UpdateLakeFields(config);

        // For feature-specific fields (Cellular Automata, etc.), 
        // we show them with default or user-defined values.
        // Suppose we store them in "currentSettings" to keep it consistent.
        if (featureCAIterationsField)
        {
            featureCAIterationsField.text = currentSettings.featureCAIterations.ToString();
        }
        if (featureNeighborThresholdField)
        {
            featureNeighborThresholdField.text = currentSettings.featureNeighborThreshold.ToString();
        }
        if (globalFeatureDensityField)
        {
            globalFeatureDensityField.text = currentSettings.globalFeatureDensity.ToString("F2");
        }
    }

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

    private void UpdateMidpointDisplacementFields(TerrainGenerationSettings config)
    {
        SetField(useMidPointDisplacementToggle, config.useMidPointDisplacement);
        SetField(displacementFactorField, config.displacementFactor.ToString());
        SetField(displacementDecayRateField, config.displacementDecayRate.ToString());
        SetField(randomSeedField, config.randomSeed.ToString());
    }

    private void UpdateVoronoiBiomesFields(TerrainGenerationSettings config)
    {
        SetField(useVoronoiBiomesToggle, config.useVoronoiBiomes);
        SetField(voronoiCellCountField, config.voronoiCellCount.ToString());

        voronoiDistributionModeDropdown.value = (int)config.voronoiDistributionMode;
        voronoiDistributionModeDropdown.RefreshShownValue();
    }

    private void UpdateErosionFields(TerrainGenerationSettings config)
    {
        SetField(useErosionToggle, config.useErosion);
        SetField(talusAngleField, config.talusAngle.ToString());
        SetField(erosionIterationsField, config.erosionIterations.ToString());
    }

    private void UpdateRiverFields(TerrainGenerationSettings config)
    {
        SetField(useRiversToggle, config.useRivers);
        SetField(riverWidthField, config.riverWidth.ToString());
        SetField(riverHeightField, config.riverHeight.ToString());
    }

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

    private void AddListeners()
    {
        Debug.Log("Adding listeners to all UI components...");

        AddPerlinNoiseListeners();
        AddFractalBrownianMotionListeners();
        AddMidpointDisplacementListeners();
        AddVoronoiBiomesListeners();
        AddErosionListeners();
        AddRiverListeners();
        AddTrailListeners();
        AddLakeListeners();

        // Feature-related new listeners
        if (featureCAIterationsField)
        {
            featureCAIterationsField.onEndEdit.AddListener(value =>
            {
                if (int.TryParse(value, out int iters) && iters >= 0)
                {
                    currentSettings.featureCAIterations = iters;
                    RegenerateTerrain();
                }
                else
                {
                    DisplayError($"Invalid CA Iterations: {value}");
                }
            });
        }

        if (featureNeighborThresholdField)
        {
            featureNeighborThresholdField.onEndEdit.AddListener(value =>
            {
                if (int.TryParse(value, out int thresh) && thresh >= 0)
                {
                    currentSettings.featureNeighborThreshold = thresh;
                    RegenerateTerrain();
                }
                else
                {
                    DisplayError($"Invalid neighbor threshold: {value}");
                }
            });
        }

        if (globalFeatureDensityField)
        {
            globalFeatureDensityField.onEndEdit.AddListener(value =>
            {
                if (float.TryParse(value, out float density) && density >= 0f)
                {
                    currentSettings.globalFeatureDensity = density;
                    RegenerateTerrain();
                }
                else
                {
                    DisplayError($"Invalid global feature density: {value}");
                }
            });
        }

        // Toggles
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

    private void AddPerlinNoiseListeners()
    {
        AddValidatedFieldListener(perlinLayersField, val =>
        {
            currentSettings.perlinLayers = int.Parse(val);
        }, 1, 100);

        AddValidatedFieldListener(perlinBaseScaleField, val =>
        {
            currentSettings.perlinBaseScale = float.Parse(val);
        }, 0.1f, 500f);

        AddValidatedFieldListener(perlinAmplitudeDecayField, val =>
        {
            currentSettings.perlinAmplitudeDecay = float.Parse(val);
        }, 0f, 1f);

        AddValidatedFieldListener(perlinFrequencyGrowthField, val =>
        {
            currentSettings.perlinFrequencyGrowth = float.Parse(val);
        }, 0.1f, 10f);

        AddInputFieldListener(perlinOffsetXField, val =>
        {
            if (float.TryParse(val, out float offsetX))
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

        AddInputFieldListener(perlinOffsetYField, val =>
        {
            if (float.TryParse(val, out float offsetY))
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

    private void AddFractalBrownianMotionListeners()
    {
        AddValidatedFieldListener(fBmLayersField, val =>
        {
            currentSettings.fBmLayers = int.Parse(val);
        }, 1, 100);

        AddValidatedFieldListener(fBmBaseScaleField, val =>
        {
            currentSettings.fBmBaseScale = float.Parse(val);
        }, 0.1f, 500f);

        AddValidatedFieldListener(fBmAmplitudeDecayField, val =>
        {
            currentSettings.fBmAmplitudeDecay = float.Parse(val);
        }, 0f, 1f);

        AddValidatedFieldListener(fBmFrequencyGrowthField, val =>
        {
            currentSettings.fBmFrequencyGrowth = float.Parse(val);
        }, 0.1f, 10f);

        AddInputFieldListener(fBmOffsetXField, val =>
        {
            if (float.TryParse(val, out float offsetX))
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

        AddInputFieldListener(fBmOffsetYField, val =>
        {
            if (float.TryParse(val, out float offsetY))
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

    private void AddMidpointDisplacementListeners()
    {
        AddValidatedFieldListener(displacementFactorField, val =>
        {
            currentSettings.displacementFactor = float.Parse(val);
        }, 0.1f, 10f);

        AddValidatedFieldListener(displacementDecayRateField, val =>
        {
            currentSettings.displacementDecayRate = float.Parse(val);
        }, 0f, 1f);

        AddValidatedFieldListener(randomSeedField, val =>
        {
            currentSettings.randomSeed = int.Parse(val);
        }, 0, 10000);
    }

    private void AddVoronoiBiomesListeners()
    {
        AddValidatedFieldListener(voronoiCellCountField, val =>
        {
            if (int.TryParse(val, out int cellCount))
            {
                currentSettings.voronoiCellCount = Mathf.Clamp(cellCount, 1, currentSettings.biomes.Length);
                RegenerateTerrain();
            }
        }, 1, currentSettings.biomes.Length);

        AddDropdownListener(voronoiDistributionModeDropdown, val =>
        {
            if (Enum.IsDefined(typeof(TerrainGenerationSettings.DistributionMode), val))
            {
                currentSettings.voronoiDistributionMode = (TerrainGenerationSettings.DistributionMode)val;
                RegenerateTerrain();
            }
        });
    }

    private void AddErosionListeners()
    {
        AddFieldListener(useErosionToggle, val =>
        {
            currentSettings.useErosion = val;
            ClearError();
            RegenerateTerrain();
        });

        AddValidatedFieldListener(talusAngleField, val =>
        {
            currentSettings.talusAngle = float.Parse(val);
        }, 0.01f, 0.2f);

        AddValidatedFieldListener(erosionIterationsField, val =>
        {
            currentSettings.erosionIterations = int.Parse(val);
        }, 1, 10);
    }

    private void AddRiverListeners()
    {
        AddFieldListener(useRiversToggle, val =>
        {
            currentSettings.useRivers = val;
            RegenerateTerrain();
        });

        AddValidatedFieldListener(riverWidthField, val =>
        {
            currentSettings.riverWidth = float.Parse(val);
            RegenerateTerrain();
        }, 1f, 20f);

        AddValidatedFieldListener(riverHeightField, val =>
        {
            currentSettings.riverHeight = float.Parse(val);
            RegenerateTerrain();
        }, 0f, 1f);
    }

    private void AddTrailListeners()
    {
        AddFieldListener(useTrailsToggle, val =>
        {
            currentSettings.useTrails = val;
            ClearError();
            RegenerateTerrain();
        });

        AddInputFieldListener(trailStartPointXField, val =>
        {
            currentSettings.trailStartPoint = new Vector2(float.Parse(val), currentSettings.trailStartPoint.y);
        });

        AddInputFieldListener(trailStartPointYField, val =>
        {
            currentSettings.trailStartPoint = new Vector2(currentSettings.trailStartPoint.x, float.Parse(val));
        });

        AddInputFieldListener(trailEndPointXField, val =>
        {
            currentSettings.trailEndPoint = new Vector2(float.Parse(val), currentSettings.trailEndPoint.y);
        });

        AddInputFieldListener(trailEndPointYField, val =>
        {
            currentSettings.trailEndPoint = new Vector2(currentSettings.trailEndPoint.x, float.Parse(val));
        });

        AddValidatedFieldListener(trailWidthField, val =>
        {
            currentSettings.trailWidth = float.Parse(val);
        }, 1f, 50f);

        AddValidatedFieldListener(trailRandomnessField, val =>
        {
            currentSettings.trailRandomness = float.Parse(val);
        }, 0f, 5f);
    }

    private void AddLakeListeners()
    {
        AddFieldListener(useLakesToggle, val =>
        {
            currentSettings.useLakes = val;
            ClearError();
            RegenerateTerrain();
        });

        AddInputFieldListener(lakeCenterXField, val =>
        {
            currentSettings.lakeCenter = new Vector2(float.Parse(val), currentSettings.lakeCenter.y);
        });

        AddInputFieldListener(lakeCenterYField, val =>
        {
            currentSettings.lakeCenter = new Vector2(currentSettings.lakeCenter.x, float.Parse(val));
        });

        AddValidatedFieldListener(lakeRadiusField, val =>
        {
            currentSettings.lakeRadius = float.Parse(val);
        }, 1f, 50f);

        AddValidatedFieldListener(lakeWaterLevelField, val =>
        {
            currentSettings.lakeWaterLevel = float.Parse(val);
        }, 0f, 1f);
    }

    private void AddValidatedFieldListener(TMP_InputField field, Action<string> onChanged, float min, float max)
    {
        if (field == null)
        {
            Debug.LogError("Cannot add listener to a null TMP_InputField.");
            return;
        }

        field.onEndEdit.AddListener(val =>
        {
            try
            {
                if (float.TryParse(val, out float result) && result >= min && result <= max)
                {
                    onChanged?.Invoke(val);
                    ClearError();
                    RegenerateTerrain();
                }
                else
                {
                    string errorMsg = $"Invalid input for '{field.name}'. Must be between {min} and {max}.";
                    DisplayError(errorMsg);
                    Debug.LogWarning(errorMsg);
                    field.text = Mathf.Clamp(result, min, max).ToString();
                }
            }
            catch (Exception ex)
            {
                DisplayError($"Error processing input for '{field.name}': {ex.Message}");
                Debug.LogError(ex);
            }
        });
    }

    private void AddDropdownListener(TMP_Dropdown dropdown, Action<int> onChanged)
    {
        if (dropdown == null)
        {
            Debug.LogError("Cannot add listener to a null TMP_Dropdown.");
            return;
        }

        dropdown.onValueChanged.AddListener(val =>
        {
            try
            {
                onChanged?.Invoke(val);
                ClearError();
                RegenerateTerrain();
            }
            catch (Exception ex)
            {
                DisplayError($"Error updating Dropdown '{dropdown.name}': {ex.Message}");
                Debug.LogError(ex);
            }
        });
    }

    private void AddInputFieldListener(TMP_InputField field, Action<string> onChanged)
    {
        field.onEndEdit.AddListener(val =>
        {
            Debug.Log($"Input Changed: {field.name} = {val}");
            onChanged(val);
        });
    }

    private void AddFieldListener(Toggle toggle, Action<bool> onChanged)
    {
        if (toggle == null)
        {
            Debug.LogError("Cannot add listener to a null Toggle.");
            return;
        }

        toggle.onValueChanged.AddListener(val =>
        {
            try
            {
                onChanged?.Invoke(val);

                if (terrainGeneratorManager != null && currentSettings != null)
                {
                    terrainGeneratorManager.terrainSettings = currentSettings;
                    RegenerateTerrain();
                }
                else
                {
                    Debug.LogError("TerrainGeneratorManager or currentSettings is null. Cannot update terrain.");
                }

                ClearError();
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error updating Toggle '{toggle.name}': {ex.Message}";
                DisplayError(errorMsg);
                Debug.LogError(ex);
            }
        });
    }

    #endregion

    #region Terrain Regeneration

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

            terrainGeneratorManager.terrainSettings = currentSettings;
            terrainGeneratorManager.GenerateTerrain();
            terrainGeneratorManager.ApplyTerrainLayers();

            // Clear and re-instantiate features whenever terrain changes
            FeatureManager fm = FindObjectOfType<FeatureManager>();
            if (fm != null)
            {
                fm.ClearFeatures();
                if (fm.featuresEnabled)
                {
                    fm.PlaceFeatures();
                }
            }

            ClearError();
            Debug.Log("Terrain regeneration completed successfully.");
        }
        catch (Exception ex)
        {
            string errorMessage = $"Error during terrain regeneration: {ex.Message}";
            DisplayError(errorMessage);
            Debug.LogError(ex);
        }
    }

    #endregion

    #region Error Handling

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

    private bool floatInRange(string val, float min, float max)
    {
        return (float.TryParse(val, out float f) && f >= min && f <= max);
    }

    private bool intInRange(string val, int min, int max)
    {
        return (int.TryParse(val, out int i) && i >= min && i <= max);
    }

    private void CopySettings(TerrainGenerationSettings source, TerrainGenerationSettings target)
    {
        if (source == null || target == null)
        {
            Debug.LogError("Source or target settings are null. Copy operation aborted.");
            return;
        }

        // Perlin Noise
        target.usePerlinNoise = source.usePerlinNoise;
        target.perlinLayers = source.perlinLayers;
        target.perlinBaseScale = source.perlinBaseScale;
        target.perlinAmplitudeDecay = source.perlinAmplitudeDecay;
        target.perlinFrequencyGrowth = source.perlinFrequencyGrowth;
        target.perlinOffset = source.perlinOffset;

        // fBm
        target.useFractalBrownianMotion = source.useFractalBrownianMotion;
        target.fBmLayers = source.fBmLayers;
        target.fBmBaseScale = source.fBmBaseScale;
        target.fBmAmplitudeDecay = source.fBmAmplitudeDecay;
        target.fBmFrequencyGrowth = source.fBmFrequencyGrowth;
        target.fBmOffset = source.fBmOffset;

        // Midpoint
        target.useMidPointDisplacement = source.useMidPointDisplacement;
        target.displacementFactor = source.displacementFactor;
        target.displacementDecayRate = source.displacementDecayRate;
        target.randomSeed = source.randomSeed;

        // Voronoi
        target.useVoronoiBiomes = source.useVoronoiBiomes;
        target.voronoiCellCount = source.voronoiCellCount;
        target.voronoiDistributionMode = source.voronoiDistributionMode;
        target.voronoiBlendFactor = source.voronoiBlendFactor;

        if (source.biomes != null)
        {
            target.biomes = new TerrainGenerationSettings.Biome[source.biomes.Length];
            for (int i = 0; i < source.biomes.Length; i++)
            {
                target.biomes[i] = new TerrainGenerationSettings.Biome
                {
                    name = source.biomes[i].name,
                    thresholds = new TerrainGenerationSettings.BiomeThresholds
                    {
                        layer1 = source.biomes[i].thresholds.layer1,
                        minHeight1 = source.biomes[i].thresholds.minHeight1,
                        maxHeight1 = source.biomes[i].thresholds.maxHeight1,

                        layer2 = source.biomes[i].thresholds.layer2,
                        minHeight2 = source.biomes[i].thresholds.minHeight2,
                        maxHeight2 = source.biomes[i].thresholds.maxHeight2,

                        layer3 = source.biomes[i].thresholds.layer3,
                        minHeight3 = source.biomes[i].thresholds.minHeight3,
                        maxHeight3 = source.biomes[i].thresholds.maxHeight3
                    }
                };
            }
        }
        else
        {
            target.biomes = null;
        }

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

        // Features
        // We'll copy any new settings such as CA iterations, neighbor threshold, and global feature density.
        target.featureSettings = new List<FeatureSettings>(source.featureSettings);
        target.featureCAIterations = source.featureCAIterations;
        target.featureNeighborThreshold = source.featureNeighborThreshold;
        target.globalFeatureDensity = source.globalFeatureDensity;

        // Texture Mappings
        if (source.textureMappings != null)
        {
            target.textureMappings = source.textureMappings.ToArray();
        }
        else
        {
            target.textureMappings = null;
        }

        Debug.Log("Settings successfully copied from source to target.");
    }

    #endregion
}
