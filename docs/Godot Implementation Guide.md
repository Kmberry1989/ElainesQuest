# Elaine's Quest Implementation Guide

This guide explains how the current prototype is implemented in code and scenes. Use it when changing gameplay behavior, extending progression, or tracing how a world event updates UI and hub state.

## Core Runtime Model

The project is built around a small autoload plus scene-local gameplay scripts.

- `GameManager.cs` owns persistent progression and shared signals
- world scenes place content and instantiate reusable scenes
- UI scenes subscribe to signals instead of polling world state
- interactables update progression or move the player between scenes

That keeps the implementation simple while allowing progression to survive `ChangeSceneToFile(...)`.

## Global State: `GameManager`

`Scripts/Core/GameManager.cs` is the central shared state container.

It currently owns:

- `Glimmers`
- `FamilyRescuedCount`
- `RescuedFamilyIds`
- `BefriendedAnimals`
- `HasMagicRing`
- `CanTranslateAnimals`

It also emits the main cross-scene events:

- `GlimmersChanged`
- `FamilyRescuedChanged`
- `AnimalBefriended`
- `DialogueRequested`

Use `GameManager` when a value must persist across scene changes or when multiple systems need to react to the same gameplay event.

## Scene Startup

`Scripts/System/SceneBootstrap.cs` is the launch bridge between project settings and the actual playable world.

Behavior:

1. find or create `WorldRoot`
2. load the configured `DefaultWorldScene`
3. instantiate that scene if it is not already present

This allows startup logic to stay centralized without turning the hub scene itself into the global entry mechanism.

## Player Implementation

`Scripts/Player/ElaineController.cs` owns:

- movement input
- jumping
- hover behavior
- visuals rotation toward movement direction
- interaction search
- respawn point tracking

Implementation notes:

- the player joins the `player` group in `_Ready()`
- interaction is driven from `_UnhandledInput`
- nearest NPC selection uses the `npc_interaction` group plus squared distance checks
- hover is only active while airborne, the jump input is held, and hover time remains

If you change movement feel, this is the primary script to touch.

## Dialogue Flow

Dialogue is event-driven:

1. an NPC or rescue trigger calls `GameManager.RequestDialogue(...)`
2. `DialogueUI.cs` receives `DialogueRequested`
3. the dialogue panel is populated and shown
4. the panel closes on `interact` or `ui_cancel`

Relevant files:

- `Scripts/NPCs/NPC.cs`
- `Scripts/UI/DialogueUI.cs`
- `Scenes/UI/DialogueUI.tscn`

This is intentionally lightweight. The current system is a single-message overlay, not a branching dialogue tree.

## NPC and Wildlife Implementation

### `NPC.cs`

`NPC.cs` is the interaction endpoint for animal conversations.

On interaction:

- it resolves the speaker identity
- it chooses translated or untranslated dialogue based on `CanTranslateAnimals`
- it sends the chosen line to the dialogue UI through `GameManager`
- it marks the animal as befriended when translation is active

### `AnimalBehavior.cs`

`AnimalBehavior.cs` handles idle wildlife wandering:

- timed pause and move phases
- randomized horizontal move direction
- simple facing rotation while moving
- gravity and `MoveAndSlide()`

This is good for lightweight hub or level ambience. More authored behavior should be added in a separate script rather than overloading the current wander loop.

## UI Implementation

### `HUD.cs`

The HUD subscribes to:

- `GlimmersChanged`
- `FamilyRescuedChanged`

It redraws its labels from `GameManager` state instead of maintaining its own counters.

### `DialogueUI.cs`

`DialogueUI` subscribes to:

- `DialogueRequested`

It owns:

- current visible speaker name
- current dialogue text
- showing and hiding the dialogue panel

If dialogue does not appear, the fastest path is to verify this scene is instanced and the autoload signal is reaching it.

## Interactable Systems

### `Collectible.cs`

- reacts to `BodyEntered`
- accepts only the player
- adds `GlimmerValue`
- removes itself

### `Checkpoint.cs`

- reacts to `BodyEntered`
- accepts `ElaineController`
- updates the player's stored respawn point

### `HazardVolume.cs`

- reacts to `BodyEntered`
- accepts `ElaineController`
- respawns the player at `RespawnMarker`
- falls back to a scene node named `PlayerSpawn`
- otherwise falls back to Elaine's stored respawn point

### `LevelPortal.cs`

- reacts to `BodyEntered`
- accepts the player
- changes scene using exported `TargetScene`

### `FamilyRescue.cs`

- reacts to `BodyEntered`
- guards against duplicate rescues
- calls `RescueFamilyMember`
- optionally awards bonus glimmers
- requests rescue dialogue
- optionally removes itself after completion

Together these scripts define most of the moment-to-moment level loop.

## Hub Progression Systems

### `FamilyTree.cs`

`FamilyTree` reacts to rescued family count and updates the hub in two ways:

1. show the correct tree growth stage
2. instance rescued family scenes at configured spawn points

It can also seed default family scene references if they were not assigned manually.

### `HubAnimalSpawner.cs`

`HubAnimalSpawner` reacts to befriended animals and:

1. clears previously spawned hub animals
2. sorts befriended animal ids
3. instances matching scenes at available markers

It also seeds a default prefab mapping for Toby when the exported dictionary is empty.

These two scripts make the hub a projection of persistent progression rather than a place that stores its own copy of state.

## Camera

`Scripts/Camera/PlatformerCamera.cs` is a simple follow camera:

- finds the player by group if no explicit target is set
- lerps toward `Target.GlobalPosition + Offset`
- looks at the player with a slight vertical aim offset

If the camera feels wrong, adjust `Offset` and `FollowSpeed` before rewriting the script.

## Character Visual Pipeline

The most important implementation constraint in the repo is the character visual pipeline.

`Scripts/System/RuntimeGltfModel.cs` exists because direct packed-scene references to imported character `.glb` assets have been unreliable in this checkout.

Implementation behavior:

1. clear any previously generated runtime model
2. show the placeholder by default
3. load a raw `.glb` or `.gltf` through `GltfDocument`
4. generate a scene graph
5. attach it under the runtime loader node
6. apply position, rotation, and scale overrides
7. hide the placeholder after success

Do not remove this path casually. It is protecting the gameplay scenes from import-cache failures.

## Extension Patterns

Use these patterns when adding features:

### Add a new collectible-like trigger

Create a new scene under `Scenes/Interactables/` and a matching script under `Scripts/Interactables/`. Keep the trigger self-contained and call into `GameManager` only for persistent outcomes.

### Add a new hub-reactive progression type

Store the durable state in `GameManager`, emit a signal when it changes, and let the hub subscribe and redraw from that signal.

### Add a new animal

1. create the scene under `Scenes/Characters/Wildlife/`
2. attach `AnimalBehavior.cs` if wandering is enough
3. attach or configure `NPC.cs` for dialogue
4. register the animal in `HubAnimalSpawner`

### Add a new family rescue

1. create or reuse a family character scene
2. place a `FamilyRescue.tscn` trigger in the target level
3. assign `FamilyId`, display text, and any bonus
4. ensure the family scene can be spawned by `FamilyTree`

## Current Technical Risks

These are still the fragile areas:

- character model import health
- runtime GLTF alignment per character
- incomplete animation hookup beyond the current movement state machine assumptions
- any future scene that reintroduces direct imported-model dependencies without verification

If you are deciding where to invest next, the safest sequence is:

1. verify all current scenes in Godot
2. stabilize character transforms
3. decide whether to keep runtime GLTF loading or replace it with clean wrapper scenes generated from repaired imports
