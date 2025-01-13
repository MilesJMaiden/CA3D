using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public abstract class BaseAgentBehavior : MonoBehaviour, IAgentBehavior
{
    protected NavMeshAgent navMeshAgent;
    protected TerrainFeatureContext currentContext;

    protected enum State
    {
        Idle,
        Roaming,
        Custom // Extendable by subclasses
    }

    protected State currentState;

    protected virtual void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        ChangeState(State.Idle);
    }

    private void Update()
    {
        switch (currentState)
        {
            case State.Idle:
                IdleBehavior();
                break;
            case State.Roaming:
                RoamingBehavior();
                break;
        }

        CustomBehaviorUpdate(); // For subclass-specific logic
    }

    public virtual void OnTerrainUpdated(List<FeatureSettings> activeFeatures)
    {
        // React to terrain updates (override in subclasses)
    }

    public virtual void ModifyBehavior(TerrainFeatureContext context, List<TerrainGenerationSettings.AgentBehaviorModifier> modifiers)
    {
        currentContext = context;
        // Apply generic modifiers (override in subclasses)
    }

    protected virtual void ChangeState(State newState)
    {
        currentState = newState;
    }

    protected virtual void IdleBehavior()
    {
        if (Random.Range(0, 100) < 10)
        {
            ChangeState(State.Roaming);
        }
    }

    protected virtual void RoamingBehavior()
    {
        if (!navMeshAgent.hasPath)
        {
            Vector3 randomDestination = GetRandomNavMeshPoint();
            navMeshAgent.SetDestination(randomDestination);
        }
    }

    protected abstract void CustomBehaviorUpdate(); // To be implemented by subclasses

    protected Vector3 GetRandomNavMeshPoint()
    {
        NavMeshHit hit;
        Vector3 randomPoint = new Vector3(
            Random.Range(0, 100), 0, Random.Range(0, 100) // Replace with actual terrain dimensions
        );

        if (NavMesh.SamplePosition(randomPoint, out hit, 10f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return Vector3.zero;
    }
}
