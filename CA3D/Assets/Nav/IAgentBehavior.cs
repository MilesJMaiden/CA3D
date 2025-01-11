using System.Collections.Generic;

public interface IAgentBehavior
{
    void OnTerrainUpdated(List<FeatureSettings> features);

    /// <summary>
    /// Modifies the behavior of the agent based on the provided terrain context and behavior modifiers.
    /// </summary>
    /// <param name="context">The context of terrain features.</param>
    /// <param name="modifiers">List of behavior modifiers to apply.</param>
    void ModifyBehavior(TerrainFeatureContext context, List<TerrainGenerationSettings.AgentBehaviorModifier> modifiers);
}
