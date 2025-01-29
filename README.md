##Procedural Terrain and Ecosystem Simulator

A **hybrid terrain-generation system** built in Unity that combines noise-based algorithms (Perlin Noise, Fractal Brownian Motion, Voronoi Biomes) with a **Cellular Automata** approach to place features (rocks, shrubs, cacti) and a **basic AI** predator-prey model using Unity's Navigation mesh and finite state machines (FSMs).

## Features

- **Noise-Based Terrain Generation**  
  - Perlin Noise, Fractal Brownian Motion (fBm), and optional Mid-Point Displacement  
  - Voronoi partitioning for biome-specific regions (e.g., desert, forest)  
  - Material layering for terrain textures based on height and slope

- **Cellular Automata (CA) for Feature Placement**  
  - Simple 2D CA rules to cluster objects (e.g., rocks, vegetation)  
  - Configurable neighborhood threshold, iteration count, and global spawn density  
  - Distinct rules for each feature (height range, slope tolerance, biome restrictions)

- **Environmental Modifiers**  
  - **TrailModifier**: Carves paths into the terrain  
  - **LakeModifier**: Adds water bodies at lower elevations

- **Basic AI Navigation**  
  - Wolves, deer, and squirrels with finite state machines (FSMs)  
  - Unity's NavMesh-based pathfinding for roaming and fleeing  
  - Illustrates predator-prey interactions in a simplistic food-chain dynamic

- **Runtime UI**  
  - Toggle noise algorithms, Voronoi biomes, CA iterations, and AI agents on-the-fly  
  - Real-time updates to the generated terrain for rapid experimentation  
  - ScriptableObjects to store and share generation settings across scenes

## Getting Started

1. **Clone or Download** this repository.
2. **Open** the project in Unity (tested with Unity 6000.0.30f1).
3. **Open** the main scene (e.g., `Assets/Scenes/MainScene.unity`).
4. **Check** the `TerrainGeneratorManager` and `FeatureManager` components in the scene to customize default presets.
5. **Press Play** to see the terrain generated in real-time. Adjust parameters in the UI to experiment.

## Dependencies

- Unity’s **Burst** and **Mathematics** packages for jobified noise generation.
- Unity’s **Navigation** system (NavMesh) for AI pathfinding.
- **ScriptableObjects** for storing terrain generation settings and feature definitions.
