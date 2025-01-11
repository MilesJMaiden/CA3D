using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PolarBearBehavior : MonoBehaviour, IAgentBehavior
{
    private NavMeshAgent navMeshAgent;

    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    public void OnTerrainUpdated(List<FeatureSettings> activeFeatures)
    {
        // React to terrain updates (e.g., find new cold areas)
    }

    public void ModifyBehavior(TerrainFeatureContext context, List<TerrainGenerationSettings.AgentBehaviorModifier> modifiers)
    {
        foreach (var modifier in modifiers)
        {
            switch (modifier.modifierName)
            {
                case "SpeedAdjustment":
                    navMeshAgent.speed *= modifier.intensity;
                    break;
                case "PreferColdBiomes":
                    if (!context.hasLakes)
                    {
                        navMeshAgent.SetDestination(FindColdArea());
                    }
                    break;
                default:
                    Debug.LogWarning($"Unknown modifier: {modifier.modifierName}");
                    break;
            }
        }
    }

    private Vector3 FindColdArea()
    {
        // Logic to locate cold biomes
        return Vector3.zero;
    }
}
