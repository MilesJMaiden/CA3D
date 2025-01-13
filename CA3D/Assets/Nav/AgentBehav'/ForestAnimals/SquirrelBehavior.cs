using System.Collections.Generic;
using UnityEngine;

public class SquirrelBehavior : BaseAgentBehavior
{
    private enum SquirrelState
    {
        Foraging,
        ClimbingTree
    }

    private FeatureSettings treeFeature;

    protected override void CustomBehaviorUpdate()
    {
        if (currentState == (State)SquirrelState.Foraging)
        {
            ForagingBehavior();
        }
        else if (currentState == (State)SquirrelState.ClimbingTree)
        {
            ClimbingTreeBehavior();
        }
    }

    public override void OnTerrainUpdated(List<FeatureSettings> activeFeatures)
    {
        base.OnTerrainUpdated(activeFeatures);
        treeFeature = activeFeatures.Find(f => f.featureName == "Trees");
    }

    public override void ModifyBehavior(TerrainFeatureContext context, List<TerrainGenerationSettings.AgentBehaviorModifier> modifiers)
    {
        base.ModifyBehavior(context, modifiers);

        foreach (var modifier in modifiers)
        {
            if (modifier.modifierName == "ClimbTrees" && context.activeFeatures.Exists(f => f.featureName == "Trees"))
            {
                ChangeState((State)SquirrelState.ClimbingTree);
            }
        }
    }

    private void ForagingBehavior()
    {
        if (!navMeshAgent.hasPath)
        {
            Vector3 foragingSpot = GetRandomNavMeshPoint();
            navMeshAgent.SetDestination(foragingSpot);
        }

        if (treeFeature != null && Random.Range(0, 100) < 20)
        {
            ChangeState((State)SquirrelState.ClimbingTree);
        }
    }

    private void ClimbingTreeBehavior()
    {
        if (treeFeature != null)
        {
            Vector3 treeLocation = GetRandomNavMeshPoint(); // Replace with actual tree logic
            navMeshAgent.SetDestination(treeLocation);

            if (Vector3.Distance(transform.position, treeLocation) < 1f)
            {
                Debug.Log("Climbing tree...");
                ChangeState(State.Idle);
            }
        }
        else
        {
            ChangeState(State.Idle);
        }
    }
}
