using System.Collections.Generic;
using UnityEngine;

public class WolfBehavior : BaseAgentBehavior
{
    private enum WolfState
    {
        Hunting
    }

    protected override void CustomBehaviorUpdate()
    {
        if (currentState == (State)WolfState.Hunting)
        {
            HuntingBehavior();
        }
    }

    public override void ModifyBehavior(TerrainFeatureContext context, List<TerrainGenerationSettings.AgentBehaviorModifier> modifiers)
    {
        base.ModifyBehavior(context, modifiers);

        foreach (var modifier in modifiers)
        {
            if (modifier.modifierName == "Hunting")
            {
                ChangeState((State)WolfState.Hunting);
            }
        }
    }

    private void HuntingBehavior()
    {
        Vector3 preyPosition = FindPrey(); // Replace with actual prey-finding logic
        navMeshAgent.SetDestination(preyPosition);

        if (Vector3.Distance(transform.position, preyPosition) < 1f)
        {
            Debug.Log("Caught prey!");
            ChangeState(State.Idle);
        }
    }

    private Vector3 FindPrey()
    {
        // Logic to find prey (e.g., based on tags or proximity)
        return GetRandomNavMeshPoint();
    }
}
