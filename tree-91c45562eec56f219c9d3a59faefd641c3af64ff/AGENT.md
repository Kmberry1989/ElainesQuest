# Elaine's Quest Handoff

## Goal

Turn this repo into a runnable Godot 4 .NET prototype with:

- a playable `HubWorld`
- a playable `World_1_Forest`
- working UI, dialogue, collectibles, hazard respawn, rescue progression, and hub return flow
- actual character models restored safely without hard scene dependencies on broken Godot import artifacts

## Current State

- `project.godot` launches into `res://Scenes/System/Bootstrap.tscn`
- `Bootstrap.tscn` instantiates `res://Scenes/Hub/HubWorld.tscn`
- `HubWorld.tscn` includes:
  - Elaine
  - HUD
  - Dialogue UI
  - portal to `World_1_Forest`
  - Sasha NPC
  - Family Tree progression node
  - Hub animal spawner
- `World_1_Forest.tscn` includes:
  - Elaine
  - HUD
  - Dialogue UI
  - Toby NPC
  - collectibles
  - checkpoint
  - hazard volume
  - Matthew rescue trigger
  - return portal back to `HubWorld`

## Important Scripts

- `Scripts/Core/GameManager.cs`
  - global state for glimmers, rescued family, befriended animals, and dialogue events
  - `CanTranslateAnimals` is currently `true`
- `Scripts/System/SceneBootstrap.cs`
  - only instantiates the default world scene
- `Scripts/System/RuntimeGltfModel.cs`
  - safe runtime/editor GLTF loader
  - loads raw `.glb` files via `GltfDocument`
  - hides a placeholder mesh when load succeeds
  - avoids brittle direct `ext_resource` links to character `.glb` assets
- `Scripts/Interactables/FamilyRescue.cs`
  - handles family rescue progression and reward
- `Scripts/Hub/FamilyTree.cs`
  - shows tree growth and spawns rescued family members
- `Scripts/Hub/HubAnimalSpawner.cs`
  - spawns befriended animals into the hub

## Scene Strategy

Character scenes do **not** directly reference `Assets/Models/.../*.glb` as packed scene resources anymore.

Instead:

- each relevant character scene has a placeholder mesh
- the placeholder lives under a `Visuals` node
- `Visuals` uses `RuntimeGltfModel.cs`
- `RuntimeGltfModel.cs` loads the raw `.glb` file at runtime / in-editor

This was done because the repo's Godot import cache is inconsistent:

- many `.glb.import` files exist but are marked `valid=false`
- some assets have cached `.scn` import outputs, many do not
- direct scene dependencies on raw model imports caused editor load failures like missing `Ann-model.glb`

## Files Changed In This Handoff

- `AGENT.md`
- `Scenes/Characters/Player/Elaine.tscn`
- `Scenes/Characters/Wildlife/Toby.tscn`
- `Scenes/Characters/Wildlife/Sasha.tscn`
- `Scenes/Characters/Family/Ann.tscn`
- `Scenes/Characters/Family/Matthew.tscn`
- `Scenes/Characters/Family/Antonio.tscn`
- `Scenes/Characters/Family/Armando.tscn`
- `Scenes/Characters/Family/GrandmaJudy.tscn`
- `Scenes/Hub/HubWorld.tscn`
- `Scenes/Levels/World_1_Forest.tscn`
- `Scenes/Interactables/FamilyRescue.tscn`
- `Scripts/System/RuntimeGltfModel.cs`
- `Scripts/Interactables/FamilyRescue.cs`
- plus supporting updates in `GameManager`, `HUD`, `DialogueUI`, `LevelPortal`, `SceneBootstrap`, and `HubAnimalSpawner`

## Verified

- `dotnet build ElainesQuest.sln` succeeds with 0 errors and 0 warnings

## Not Verified

Interactive Godot validation was **not** completed in this environment because the `godot` executable was not available here.

That means these still need manual editor checks:

- `Bootstrap.tscn` opens cleanly
- `HubWorld.tscn` opens cleanly
- `World_1_Forest.tscn` opens cleanly
- runtime-loaded character models appear and are reasonably aligned
- NPC interaction distance feels correct
- dialogue opens and closes with `E` / `Esc`
- rescuing Matthew updates the Family Tree and returns cleanly to hub
- Toby appears in the hub after befriending

## Highest-Risk Areas

1. `RuntimeGltfModel.cs`
   - the GLTF runtime load path compiles, but visual alignment may need per-character offsets/scale tweaks
2. Character orientation / scale
   - current scenes use basic offsets only
   - real models may face the wrong direction or clip into the ground
3. Environment `.glb` scene references
   - hub / forest still directly instance environment model imports
   - those loaded previously in repo state, but the import cache is still inconsistent overall
4. `AnimationTree`
   - `ElaineController.cs` still references an `AnimationTree`, but actual model animation hookup was not rebuilt yet
   - movement works as prototype logic, but animation playback may still be inert

## Recommended Next Steps

1. Open Godot 4 .NET and reload the project.
2. Open `Scenes/System/Bootstrap.tscn`.
3. Confirm the playable loop:
   - hub spawn
   - portal to forest
   - collect glimmers
   - talk to Toby
   - rescue Matthew
   - return to hub
4. For each character scene, adjust:
   - `LoadedOffset`
   - `LoadedRotationDegrees`
   - `LoadedScale`
   on the `Visuals` node if the GLTF appears misaligned.
5. If runtime GLTF loading is too slow or unstable, replace it with proper imported wrapper scenes generated from a clean Godot reimport on this machine.
6. Rebuild actual animation hookup only after model transforms are stable.

## Notes For The Next Agent

- Do not reintroduce direct packed-scene references from gameplay scenes to raw character `.glb` files unless the import cache has been repaired first.
- Prefer wrapper scenes or the current `RuntimeGltfModel` approach.
- If Godot regenerates clean import artifacts on this machine, the safe upgrade path is:
  - verify each model import opens
  - create stable wrapper scenes around imported assets
  - then remove runtime GLTF loading if desired
