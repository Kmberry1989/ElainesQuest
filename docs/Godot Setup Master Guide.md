# Elaine's Quest Setup Master Guide

This guide is for working with the current repository, not for recreating the project from scratch. Use it when you want to open the repo cleanly, build the C# code, and verify the main playable loop inside Godot.

## Current Project Baseline

- Engine: Godot 4 .NET
- C# solution: `ElainesQuest.sln`
- Main scene: `res://Scenes/System/Bootstrap.tscn`
- Default world: `res://Scenes/Hub/HubWorld.tscn`
- Autoload: `Scripts/Core/GameManager.cs`

CLI verification in this environment:

- `dotnet build ElainesQuest.sln`
- result: succeeded with 0 warnings and 0 errors on June 27, 2026

## First Open Checklist

1. Open the project with the Godot 4 .NET editor.
2. Let Godot import assets and regenerate any local editor cache it needs.
3. Open the C# build panel and confirm the project compiles in-editor.
4. Verify `project.godot` still points to `res://Scenes/System/Bootstrap.tscn`.
5. Verify the `GameManager` autoload is present.

If Godot reports broken character asset imports, do not immediately wire raw `.glb` imports back into gameplay scenes. This repo currently relies on runtime GLTF loading for character safety.

## Input and Runtime Settings

The expected gameplay actions already exist in `project.godot`:

- `move_left`
- `move_right`
- `move_forward`
- `move_backward`
- `jump`
- `interact`

`DialogueUI` also listens for `ui_cancel` when closing the dialogue panel.

If any of these actions are missing in the editor, repair them in Project Settings before debugging player behavior.

## How Startup Works

Startup is intentionally simple:

1. Godot runs `Bootstrap.tscn`
2. `SceneBootstrap.cs` ensures a `WorldRoot` exists
3. `SceneBootstrap.cs` instances `res://Scenes/Hub/HubWorld.tscn`

Do not change the main scene to a level scene unless you deliberately want to bypass the bootstrap flow.

## Scene Verification Order

Open and verify scenes in this order:

1. `Scenes/System/Bootstrap.tscn`
2. `Scenes/Hub/HubWorld.tscn`
3. `Scenes/Levels/World_1_Forest.tscn`
4. character scenes that use runtime GLTF loading

This order catches top-level startup problems before you spend time debugging secondary scenes.

## Main Playable Loop

The intended prototype loop is:

1. Launch into the hub
2. Move Elaine around the hub
3. Talk to Sasha
4. Enter the portal to `World_1_Forest.tscn`
5. Collect glimmers
6. Hit a checkpoint
7. Fall into or trigger a hazard and confirm respawn works
8. Talk to Toby
9. Rescue Matthew
10. Return to the hub
11. Confirm the family tree and hub population reflect progression

## Character Model Safety

Character scenes currently avoid hard dependencies on imported `.glb` scene resources.

The safe path is:

1. keep a placeholder under `Visuals`
2. load the raw model via `RuntimeGltfModel.cs`
3. use exported transform overrides to align the loaded model

This exists because some `.glb.import` files in the project history were invalid or incomplete. If Godot regenerates clean imports on this machine, that can be upgraded later, but it should be verified carefully first.

## Manual Editor Checks

These checks still matter even when the solution builds:

- `Bootstrap.tscn` opens without missing-resource errors
- `HubWorld.tscn` opens without missing-resource errors
- `World_1_Forest.tscn` opens without missing-resource errors
- Elaine can move, jump, and hover
- HUD updates when glimmers are collected
- dialogue opens and closes correctly
- portals change scenes cleanly
- checkpoints update respawn
- hazards respawn Elaine at the expected location
- Matthew rescue increments family progression
- befriended animals appear in the hub on return

## Common Problem Areas

### Character visuals appear missing or misaligned

Check the `Visuals` subtree on the character scene and inspect the exported values on `RuntimeGltfModel`:

- `ModelPath`
- `LoadedOffset`
- `LoadedRotationDegrees`
- `LoadedScale`

If the placeholder remains visible, the runtime load probably failed or the model path is wrong.

### Dialogue does not appear

Check:

- `DialogueUI.tscn` is instanced in the active world scene
- `GameManager` autoload exists
- the NPC is in the `npc_interaction` group through `NPC.cs`
- the player is pressing `interact` within range

### Respawn feels broken

Check:

- `Checkpoint.cs` is updating Elaine's respawn point
- `HazardVolume.cs` has a valid `RespawnMarker`, or the scene contains `PlayerSpawn`
- the active body is actually the `ElaineController`

### Hub progression does not refresh

Check:

- `GameManager` signals are firing
- `FamilyTree.cs` is subscribed to `FamilyRescuedChanged`
- `HubAnimalSpawner.cs` is subscribed to `AnimalBefriended`

## Safe Workflow For New Content

1. Add new progression state to `GameManager` only when it must survive scene changes.
2. Add new world triggers in `Scenes/Interactables/` and `Scripts/Interactables/`.
3. Add new characters under `Scenes/Characters/`.
4. Prefer signals and scene-local exports over hard cross-scene references.
5. Treat direct `.glb` scene references as unsafe until Godot import health is confirmed on the current machine.

## Recommended Follow-Up When Working In Godot

1. Open `Bootstrap.tscn` and play the full hub-to-forest loop.
2. Record any character alignment issues per scene.
3. Fix offsets on the relevant `RuntimeGltfModel` nodes.
4. Only after transforms are stable, decide whether animation hookup or clean imported wrapper scenes should be the next investment.
