using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SnakeBehavior : MonoBehaviour, IAgentBehavior
{
    private NavMeshAgent navMeshAgent;

    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
    }

    public void OnTerrainUpdated(List<FeatureSettings> activeFeatures)
    {
        // React to terrain updates
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
                case "FavorRocks":
                    if (context.activeFeatures.Exists(f => f.featureName == "Rocks"))
                    {
                        navMeshAgent.SetDestination(FindRocks());
                    }
                    break;
                default:
                    Debug.LogWarning($"Unknown modifier: {modifier.modifierName}");
                    break;
            }
        }
    }

    private Vector3 FindRocks()
    {
        // Logic to locate rocks
        return Vector3.zero;
    }
}
