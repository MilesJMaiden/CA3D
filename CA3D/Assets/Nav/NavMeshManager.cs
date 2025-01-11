using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.AI.Navigation;

public class NavMeshManager : MonoBehaviour
{
    [Header("NavMesh Settings")]
    public Toggle enableNavMeshToggle;
    public Button rebakeButton;

    [Header("Agent Controls")]
    public GameObject agentScrollViewContent;
    public GameObject agentTogglePrefab; // A prefab toggle to instantiate for each agent type

    [Header("NavMesh Components")]
    public NavMeshSurface navMeshSurface;

    [Header("Agent Prefabs")]
    public List<GameObject> agentPrefabs; // Different animal prefabs
    private Dictionary<string, Toggle> agentToggles = new Dictionary<string, Toggle>();
    private List<GameObject> activeAgents = new List<GameObject>();

    [Header("Dependencies")]
    public TerrainGeneratorManager terrainGeneratorManager; // Your existing terrain generator
    public FeatureManager featureManager; // Existing feature manager
    private bool isNavMeshEnabled = false;

    private void Start()
    {
        InitializeNavMeshToggle();
        PopulateAgentScrollView();

        // Hook into terrain regeneration
        if (terrainGeneratorManager != null)
        {
            terrainGeneratorManager.OnTerrainRegenerated += OnTerrainRegenerated;
        }

        // Optionally bake NavMesh at startup if enabled
        if (enableNavMeshToggle.isOn)
        {
            isNavMeshEnabled = true;
            BakeNavMesh();
        }
    }

    private void InitializeNavMeshToggle()
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

        rebakeButton.onClick.AddListener(() =>
        {
            if (isNavMeshEnabled)
            {
                BakeNavMesh();
            }
        });
    }

    private void PopulateAgentScrollView()
    {
        foreach (var agentPrefab in agentPrefabs)
        {
            GameObject toggleObj = Instantiate(agentTogglePrefab, agentScrollViewContent.transform);
            Toggle agentToggle = toggleObj.GetComponent<Toggle>();
            Text label = toggleObj.GetComponentInChildren<Text>();

            string agentName = agentPrefab.name;
            label.text = agentName;
            agentToggle.isOn = false;

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

            agentToggles.Add(agentName, agentToggle);
        }
    }

    private void OnTerrainRegenerated()
    {
        if (isNavMeshEnabled)
        {
            BakeNavMesh();
        }

        // Adjust agent behaviors dynamically based on the new terrain
        foreach (var agent in activeAgents)
        {
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

        Vector3 randomPosition = GetRandomNavMeshPoint();
        GameObject agent = Instantiate(agentPrefab, randomPosition, Quaternion.identity);
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

    private Vector3 GetRandomNavMeshPoint()
    {
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

        return randomPoint;
    }

}
