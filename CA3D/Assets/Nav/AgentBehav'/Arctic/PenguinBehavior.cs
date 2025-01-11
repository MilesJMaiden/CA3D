using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PenguinBehavior : MonoBehaviour, IAgentBehavior
{
    private NavMeshAgent navMeshAgent;

    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    public void OnTerrainUpdated(List<FeatureSettings> activeFeatures)
    {
        // React to terrain updates (e.g., move to new lakes)
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
                case "FavorWater":
                    if (context.hasLakes)
                    {
                        navMeshAgent.SetDestination(FindLake());
                    }
                    break;
                default:
                    Debug.LogWarning($"Unknown modifier: {modifier.modifierName}");
                    break;
            }
        }
    }

    private Vector3 FindLake()
    {
        // Logic to locate water bodies
        return Vector3.zero;
    }
}
