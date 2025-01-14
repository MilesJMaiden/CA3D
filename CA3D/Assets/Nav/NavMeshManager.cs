using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.AI.Navigation;

public class NavMeshManager : MonoBehaviour
{
    [Header("NavMesh Settings")]
    [Tooltip("Toggle to enable or disable the NavMesh system.")]
    public Toggle enableNavMeshToggle;

    [Tooltip("Button to manually rebake the NavMesh.")]
    public Button rebakeButton;

    [Header("Agent Controls")]
    [Tooltip("The parent GameObject that contains agent toggles.")]
    public GameObject agentScrollViewContent;

    [Tooltip("Prefab for creating agent toggles.")]
    public GameObject agentTogglePrefab;

    [Header("NavMesh Components")]
    [Tooltip("The NavMeshSurface responsible for baking the NavMesh.")]
    public NavMeshSurface navMeshSurface;

    [Header("Agent Prefabs")]
    [Tooltip("List of agent prefabs that can be spawned.")]
    public List<GameObject> agentPrefabs;

    [Header("Max Agents Settings")]
    [Tooltip("Input field to dynamically set the maximum number of agents per type.")]
    public TMP_InputField maxAgentsInputField;

    private int maxAgentsPerType = 4;
    private Dictionary<string, Toggle> agentToggles = new Dictionary<string, Toggle>();
    private Dictionary<string, List<GameObject>> activeAgentsByType = new Dictionary<string, List<GameObject>>();

    [Header("Dependencies")]
    [Tooltip("Reference to the Terrain Generator Manager.")]
    public TerrainGeneratorManager terrainGeneratorManager;

    [Tooltip("Reference to the Feature Manager.")]
    public FeatureManager featureManager;

    private bool isNavMeshEnabled = false;

    private void Start()
    {
        InitializeNavMeshToggle();
        PopulateAgentScrollView();

        if (terrainGeneratorManager != null)
        {
            terrainGeneratorManager.OnTerrainRegenerated += OnTerrainRegenerated;
        }

        if (enableNavMeshToggle.isOn)
        {
            isNavMeshEnabled = true;
            BakeNavMesh();
        }

        if (maxAgentsInputField != null)
        {
            maxAgentsInputField.text = maxAgentsPerType.ToString();
            maxAgentsInputField.onValueChanged.AddListener(OnMaxAgentsInputChanged);
        }
    }

    private void InitializeNavMeshToggle()
    {
        if (enableNavMeshToggle != null)
        {
            enableNavMeshToggle.onValueChanged.AddListener((value) =>
            {
                isNavMeshEnabled = value;
                if (isNavMeshEnabled)
                {
                    BakeNavMesh();
                }
                else
                {
                    ClearAgents();
                }
            });
        }

        if (rebakeButton != null)
        {
            rebakeButton.onClick.AddListener(() =>
            {
                if (isNavMeshEnabled)
                {
                    BakeNavMesh();
                }
            });
        }
    }

    private void PopulateAgentScrollView()
    {
        foreach (var agentPrefab in agentPrefabs)
        {
            string agentName = agentPrefab.name;

            // Instantiate the agent toggle prefab as a child of the scroll view content
            GameObject toggleObj = Instantiate(agentTogglePrefab, agentScrollViewContent.transform);

            // Find the Toggle component
            Toggle agentToggle = toggleObj.GetComponent<Toggle>();
            if (agentToggle == null)
            {
                Debug.LogError("AgentTogglePrefab is missing a Toggle component.");
                continue;
            }

            // Find the TextMeshPro child (assuming it's correctly set up)
            TMPro.TextMeshProUGUI label = toggleObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (label == null)
            {
                Debug.LogError("AgentTogglePrefab is missing a TextMeshProUGUI component.");
                continue;
            }

            // Set the name of the prefab as the label's text
            label.text = agentName;

            // Ensure the toggle starts in an "off" state
            agentToggle.isOn = false;

            // Add a listener to handle toggle value changes
            agentToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    SpawnAgents(agentPrefab, maxAgentsPerType);
                }
                else
                {
                    RemoveAgents(agentPrefab);
                }
            });

            // Store the toggle in a dictionary for future reference
            agentToggles.Add(agentName, agentToggle);

            // Initialize the active agents list for this type
            activeAgentsByType[agentName] = new List<GameObject>();
        }
    }

    private void OnTerrainRegenerated()
    {
        if (isNavMeshEnabled)
        {
            BakeNavMesh();
        }

        foreach (var agentList in activeAgentsByType.Values)
        {
            foreach (var agent in agentList)
            {
                if (agent == null) continue;

                var agentBehavior = agent.GetComponent<IAgentBehavior>();
                if (agentBehavior != null)
                {
                    agentBehavior.OnTerrainUpdated(featureManager.GetFeatures());
                }
            }
        }
    }

    private void BakeNavMesh()
    {
        if (navMeshSurface == null)
        {
            Debug.LogError("NavMeshSurface is not assigned.");
            return;
        }

        navMeshSurface.BuildNavMesh();
        Debug.Log("NavMesh baked successfully.");
    }

    private void SpawnAgents(GameObject agentPrefab, int count)
    {
        string agentName = agentPrefab.name;
        var activeAgents = activeAgentsByType[agentName];

        // Spawn agents up to the limit
        while (activeAgents.Count < count)
        {
            Vector3 spawnPosition = GetFeatureBasedSpawnPoint();
            if (spawnPosition == Vector3.zero)
            {
                Debug.LogWarning($"Failed to find a valid spawn point for {agentName}.");
                break;
            }

            GameObject agent = Instantiate(agentPrefab, spawnPosition, Quaternion.identity);
            activeAgents.Add(agent);
        }
    }

    private void RemoveAgents(GameObject agentPrefab)
    {
        string agentName = agentPrefab.name;
        var activeAgents = activeAgentsByType[agentName];

        // Remove all active agents of this type
        foreach (var agent in activeAgents)
        {
            if (agent != null)
            {
                Destroy(agent);
            }
        }

        activeAgents.Clear();
    }

    private void ClearAgents()
    {
        foreach (var agentList in activeAgentsByType.Values)
        {
            foreach (var agent in agentList)
            {
                if (agent != null)
                {
                    Destroy(agent);
                }
            }
            agentList.Clear();
        }
    }

    private Vector3 GetFeatureBasedSpawnPoint()
    {
        List<Vector3> potentialSpawnPoints = new List<Vector3>();

        // Collect spawn points from features
        foreach (var feature in featureManager.GetFeatures())
        {
            if (feature == null) continue;

            if (feature.featureName == "Lake" || feature.featureName == "Trail")
            {
                Vector3 position = FeatureUtility.GetRandomPosition(feature, terrainGeneratorManager.terrainSize);
                if (position != Vector3.zero)
                {
                    potentialSpawnPoints.Add(position);
                }
            }
        }

        // If feature-based spawn points are available, select one at random
        if (potentialSpawnPoints.Count > 0)
        {
            int randomIndex = Random.Range(0, potentialSpawnPoints.Count);
            return potentialSpawnPoints[randomIndex];
        }

        // Fallback to a random NavMesh point
        Vector3 randomNavMeshPoint = GetRandomNavMeshPoint();
        if (randomNavMeshPoint != Vector3.zero)
        {
            return randomNavMeshPoint;
        }

        // If all else fails, use a fallback position at the center of the terrain
        Debug.LogWarning("No valid spawn points found. Using terrain center as fallback.");
        return terrainGeneratorManager.terrainSize * 0.5f;
    }

    private Vector3 GetRandomNavMeshPoint()
    {
        if (terrainGeneratorManager == null)
        {
            Debug.LogWarning("TerrainGeneratorManager is not assigned.");
            return Vector3.zero;
        }

        int maxAttempts = 10; // Maximum number of attempts to find a valid NavMesh point
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 randomPoint = new Vector3(
                Random.Range(0, terrainGeneratorManager.terrainSize.x),
                0,
                Random.Range(0, terrainGeneratorManager.terrainSize.z)
            );

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        // If no valid point is found after maxAttempts, log a warning
        Debug.LogWarning("Failed to find a valid NavMesh point after multiple attempts.");
        return Vector3.zero;
    }

    private void OnMaxAgentsInputChanged(string value)
    {
        if (int.TryParse(value, out int newMaxAgents) && newMaxAgents > 0)
        {
            maxAgentsPerType = newMaxAgents;
            Debug.Log($"Updated maxAgentsPerType to {maxAgentsPerType}.");

            // Clear all existing agents and reinstantiate based on the updated max value
            ClearAgents();
            ReInstantiateAgents();
        }
        else
        {
            Debug.LogWarning("Invalid input for maxAgentsPerType. Please enter a positive integer.");
            maxAgentsInputField.text = maxAgentsPerType.ToString(); // Reset to the previous valid value
        }
    }

    private void ReInstantiateAgents()
    {
        foreach (var kvp in agentToggles)
        {
            string agentName = kvp.Key;
            Toggle toggle = kvp.Value;

            if (toggle.isOn)
            {
                GameObject agentPrefab = agentPrefabs.Find(p => p.name == agentName);
                if (agentPrefab != null)
                {
                    SpawnAgents(agentPrefab, maxAgentsPerType);
                }
            }
        }
    }


}
