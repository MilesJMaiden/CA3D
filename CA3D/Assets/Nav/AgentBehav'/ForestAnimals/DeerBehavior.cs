using System.Collections.Generic;
using UnityEngine;

public class DeerBehavior : BaseAgentBehavior
{
    private enum DeerState
    {
        AvoidingTrail
    }

    protected override void CustomBehaviorUpdate()
    {
        if (currentState == (State)DeerState.AvoidingTrail)
        {
            AvoidingTrailBehavior();
        }
    }

    public override void ModifyBehavior(TerrainFeatureContext context, List<TerrainGenerationSettings.AgentBehaviorModifier> modifiers)
    {
        base.ModifyBehavior(context, modifiers);

        foreach (var modifier in modifiers)
        {
            if (modifier.modifierName == "AvoidTrails" && context.hasTrails)
            {
                ChangeState((State)DeerState.AvoidingTrail);
            }
        }
    }

    private void AvoidingTrailBehavior()
    {
        Vector3 safeZone = FindSafeZone();
        navMeshAgent.SetDestination(safeZone);

        if (currentContext != null && !currentContext.hasTrails)
        {
            ChangeState(State.Idle);
        }
    }

    private Vector3 FindSafeZone()
    {
        // Logic to find a safe zone
        return GetRandomNavMeshPoint();
    }
}
