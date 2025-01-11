using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DeerBehavior : MonoBehaviour, IAgentBehavior
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
                case "AvoidTrails":
                    if (context.hasTrails)
                    {
                        navMeshAgent.SetDestination(FindSafeZone());
                    }
                    break;
                default:
                    Debug.LogWarning($"Unknown modifier: {modifier.modifierName}");
                    break;
            }
        }
    }

    private Vector3 FindSafeZone()
    {
        // Logic to locate safe zones
        return Vector3.zero;
    }
}
