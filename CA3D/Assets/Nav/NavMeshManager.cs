using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
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

    private Dictionary<string, Toggle> agentToggles = new Dictionary<string, Toggle>();
    private List<GameObject> activeAgents = new List<GameObject>();

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
            string agentName = agentPrefab.name;
            label.text = agentName;

            // Ensure the toggle starts in an "off" state
            agentToggle.isOn = false;

            // Add a listener to handle toggle value changes
            agentToggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    SpawnAgent(agentPrefab);
                }
                else
                {
                    RemoveAgent(agentPrefab);
                }
            });

            // Store the toggle in a dictionary for future reference
            agentToggles.Add(agentName, agentToggle);
        }
    }


    private void OnTerrainRegenerated()
    {
        if (isNavMeshEnabled)
        {
            BakeNavMesh();
        }

        foreach (var agent in activeAgents)
        {
            if (agent == null) continue;

            var agentBehavior = agent.GetComponent<IAgentBehavior>();
            if (agentBehavior != null)
            {
                agentBehavior.OnTerrainUpdated(featureManager.GetFeatures());
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

    private void SpawnAgent(GameObject agentPrefab)
    {
        if (!isNavMeshEnabled || agentPrefab == null) return;

        Vector3 spawnPosition = GetFeatureBasedSpawnPoint();
        if (spawnPosition == Vector3.zero)
        {
            Debug.LogWarning($"Failed to find a valid spawn point for {agentPrefab.name}.");
            return;
        }

        GameObject agent = Instantiate(agentPrefab, spawnPosition, Quaternion.identity);
        activeAgents.Add(agent);
    }

    private void RemoveAgent(GameObject agentPrefab)
    {
        activeAgents.RemoveAll(agent =>
        {
            if (agent != null && agent.name.Contains(agentPrefab.name))
            {
                Destroy(agent);
                return true;
            }
            return false;
        });
    }

    private void ClearAgents()
    {
        foreach (var agent in activeAgents)
        {
            if (agent != null)
            {
                Destroy(agent);
            }
        }
        activeAgents.Clear();
    }

    private Vector3 GetFeatureBasedSpawnPoint()
    {
        List<Vector3> potentialSpawnPoints = new List<Vector3>();

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

        if (potentialSpawnPoints.Count > 0)
        {
            int randomIndex = Random.Range(0, potentialSpawnPoints.Count);
            return potentialSpawnPoints[randomIndex];
        }

        return GetRandomNavMeshPoint();
    }


    private Vector3 GetRandomNavMeshPoint()
    {
        if (terrainGeneratorManager == null)
        {
            Debug.LogWarning("TerrainGeneratorManager is not assigned.");
            return Vector3.zero;
        }

        NavMeshHit hit;
        Vector3 randomPoint = new Vector3(
            Random.Range(0, terrainGeneratorManager.terrainSize.x),
            0,
            Random.Range(0, terrainGeneratorManager.terrainSize.z)
        );

        if (NavMesh.SamplePosition(randomPoint, out hit, 10f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return Vector3.zero;
    }
}
