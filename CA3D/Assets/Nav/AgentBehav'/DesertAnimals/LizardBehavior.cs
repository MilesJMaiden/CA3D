using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class LizardBehavior : MonoBehaviour, IAgentBehavior
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
                case "AvoidWater":
                    if (context.hasLakes)
                    {
                        navMeshAgent.SetDestination(FindDrySpot());
                    }
                    break;
                default:
                    Debug.LogWarning($"Unknown modifier: {modifier.modifierName}");
                    break;
            }
        }
    }

    private Vector3 FindDrySpot()
    {
        // Logic to locate dry areas
        return Vector3.zero;
    }
}
