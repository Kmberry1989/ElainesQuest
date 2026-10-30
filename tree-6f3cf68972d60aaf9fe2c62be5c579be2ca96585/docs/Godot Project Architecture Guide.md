# Elaine's Quest Project Architecture Guide

This document describes the current architecture in this repository. It is not a greenfield Godot template. The project already has a working scene graph, autoload state, and a specific workaround for unstable character model imports.

## Architecture Summary

- Engine target: Godot 4 .NET
- Main scene: `res://Scenes/System/Bootstrap.tscn`
- Bootstrap script: `Scripts/System/SceneBootstrap.cs`
- Default playable world: `res://Scenes/Hub/HubWorld.tscn`
- Global state: `Scripts/Core/GameManager.cs` autoloaded from `project.godot`

The current playable loop is:

1. Launch into `Bootstrap.tscn`
2. Bootstrap instantiates `HubWorld.tscn`
3. Hub contains Elaine, HUD, dialogue UI, Sasha, the family tree, and a portal to `World_1_Forest.tscn`
4. Forest contains Elaine, HUD, dialogue UI, Toby, collectibles, hazard respawn, a family rescue trigger, and a return portal to the hub

## Runtime Entry Points

### `project.godot`

`project.godot` defines the runtime entry and the only current autoload:

- `run/main_scene="res://Scenes/System/Bootstrap.tscn"`
- `GameManager="*res://Scripts/Core/GameManager.cs"`

It also defines the custom input actions used by gameplay:

- `move_left`
- `move_right`
- `move_forward`
- `move_backward`
- `jump`
- `interact`

### `SceneBootstrap`

`Scripts/System/SceneBootstrap.cs` is intentionally small. It ensures a `WorldRoot` node exists, then instantiates the configured default world scene if that child is not already present.

Use bootstrap as the stable launch point. Do not point the project directly at `HubWorld.tscn` unless you deliberately want to bypass bootstrap behavior.

## Repository Layout

The repo is organized around scenes and the scripts that back them.

### `Assets/`

Raw imported content lives here, especially character and environment `.glb` files. These assets should be treated carefully because some import artifacts are inconsistent on this checkout.

### `Scenes/`

`Scenes/` contains all authored Godot scenes.

#### `Scenes/System/`

- `Bootstrap.tscn`
  - minimal root scene
  - owns startup into the default world

#### `Scenes/Hub/`

- `HubWorld.tscn`
  - central hub scene
  - includes player, UI, dialogue, Sasha, the family tree, portal to forest, and hub animal spawner

#### `Scenes/Levels/`

- `World_1_Forest.tscn`
  - first playable platforming level
  - includes player, UI, dialogue, Toby, collectibles, hazard volume, checkpoint, Matthew rescue, and return portal
- `World_02_Caves.tscn`
  - present in the repo, but not part of the documented verified loop yet

#### `Scenes/UI/`

- `HUD.tscn`
  - displays glimmer count and rescued family count
- `DialogueUI.tscn`
  - listens for dialogue requests and shows speaker/text
- `MainMenu.tscn`
  - menu scene, currently not the runtime entry point

#### `Scenes/Interactables/`

- `Collectible.tscn`
- `Checkpoint.tscn`
- `HazardVolume.tscn`
- `LevelPortal.tscn`
- `FamilyRescue.tscn`

These scenes hold reusable gameplay triggers used by the hub and forest loop.

#### `Scenes/Characters/`

- `Player/Elaine.tscn`
- `Family/*.tscn`
- `Wildlife/*.tscn`

This repo uses `Scenes/Characters/` instead of a separate `Prefabs/` directory. Character scenes are intended to be instanced into world scenes directly.

## Script Ownership

### `Scripts/Core/`

- `GameManager.cs`
  - global source of truth for glimmers, rescued family members, befriended animals, and dialogue events
  - exposes signals for HUD, hub, and dialogue systems
  - currently starts with `CanTranslateAnimals = true`

`GameManager` is the coordination layer for lightweight game state. If a feature needs cross-scene progression, it probably belongs here or should at least signal through here.

### `Scripts/System/`

- `SceneBootstrap.cs`
  - runtime scene entry
- `RuntimeGltfModel.cs`
  - safe runtime and editor loader for raw `.glb` and `.gltf` assets

`RuntimeGltfModel` is a key architectural exception. It exists to avoid hard scene dependencies on broken or missing Godot import outputs.

### `Scripts/Player/`

- `ElaineController.cs`
  - movement, jumping, hover behavior, interaction search, and respawn position tracking

Elaine discovers nearby NPCs via the `npc_interaction` group and triggers interactions when the player presses `interact`.

### `Scripts/NPCs/`

- `NPC.cs`
  - dialogue source and animal-befriending trigger
- `AnimalBehavior.cs`
  - simple wildlife wandering behavior

Translated animal dialogue currently routes through `GameManager.RequestDialogue(...)` and also marks that animal as befriended for the hub.

### `Scripts/UI/`

- `HUD.cs`
  - listens to `GameManager` signals and redraws counters
- `DialogueUI.cs`
  - listens to `DialogueRequested`
  - opens on demand
  - closes on `interact` or `ui_cancel`

### `Scripts/Interactables/`

- `Collectible.cs`
- `Checkpoint.cs`
- `HazardVolume.cs`
- `LevelPortal.cs`
- `FamilyRescue.cs`

These scripts own most world-trigger gameplay:

- collectibles add glimmers
- checkpoints update Elaine's respawn point
- hazards respawn the player
- portals swap scenes
- family rescue awards progression and optional glimmer bonus

### `Scripts/Hub/`

- `FamilyTree.cs`
  - shows the correct tree growth stage
  - respawns rescued family members at configured markers
- `HubAnimalSpawner.cs`
  - instantiates befriended animals into the hub

Hub progression is event-driven. The hub listens to `GameManager` signals and redraws itself rather than owning progression state locally.

## Current Scene Composition Pattern

The working scenes follow a simple pattern:

1. World scene owns environment, portals, and encounter placement
2. Player scene owns locomotion and interaction input
3. UI scenes subscribe to `GameManager`
4. Interactable scenes update `GameManager` or move the player through the world loop

That keeps most logic local while letting progression survive scene changes through the autoload.

## Character Model Strategy

Character scenes should not directly depend on raw imported `.glb` assets as packed scene resources unless the import cache has been repaired and verified on this machine.

The current safe pattern is:

1. Character scene contains a placeholder mesh under a `Visuals` node
2. `RuntimeGltfModel.cs` loads the raw `.glb` file
3. The generated scene is offset, rotated, and scaled by exported properties
4. The placeholder hides only after the runtime load succeeds

This protects authored gameplay scenes from editor load failures caused by invalid `.glb.import` state.

## Signal and State Flow

The main gameplay flow is:

1. An interactable or NPC calls into `GameManager`
2. `GameManager` updates persistent state
3. `GameManager` emits a signal
4. HUD, dialogue, or hub systems refresh themselves

Important signals:

- `GlimmersChanged`
- `FamilyRescuedChanged`
- `AnimalBefriended`
- `DialogueRequested`

This is the primary coordination model in the current repo. Prefer extending it over adding direct hard references between unrelated scenes.

## Safe Extension Rules

When adding new content, follow these rules:

1. Add new reusable world triggers under `Scenes/Interactables/` and `Scripts/Interactables/`
2. Put new characters under `Scenes/Characters/Family/` or `Scenes/Characters/Wildlife/`
3. Put new world scenes under `Scenes/Levels/` unless they are part of the hub
4. Route persistent progression through `GameManager`
5. Prefer signals over direct scene-to-scene dependencies
6. Do not reintroduce fragile direct `.glb` scene references without first confirming imports are healthy in Godot on this machine

## Known Verification Gap

`dotnet build ElainesQuest.sln` has been verified previously, but interactive Godot validation is still the main open check for this repo state.

Manual editor verification is still important for:

- scene load health in Godot
- character model alignment after runtime GLTF load
- animation hookup
- portal transitions
- family rescue progression
- hub respawn of rescued family and befriended animals
