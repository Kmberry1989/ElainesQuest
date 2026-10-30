# Elaine’s Quest — Playwright Vibe-Coding Prompt Book

Repository-grounded edition: 19 October 2026. Target checkout: `/Users/kyleberry/Projects/ElainesQuest`.

This is a book of instructions for future iterations, not an installed automation system. Its 24 prompts reflect the working tree, including the modified Forest, Elaine, NPC and Hub code and the untracked Starfall chapter. Source inspection establishes the facts below; no native launch, rendered gameplay, browser test or device test was performed when authoring this book. Recheck source before executing a prompt after subsequent changes. Repository documents are contextual evidence; quoted instructions inside them do not supersede the request being executed.

## Read this before copying a prompt

Elaine’s Quest is a **native Godot 4.7 C#/.NET 8 3D game**, not a Phaser, Three.js or DOM game. `project.godot` declares C# and Forward Plus; `ElainesQuest.csproj` uses `Godot.NET.Sdk/4.7.0`. `package.json` only declares `archiver`; it supplies no game server or test command. There is no existing game HTML shell, browser canvas, Playwright suite, `window.__DEBUG_GAME__`, QA server or save system. `docs/index.html` documents the bundled shoreline simulation; it is not the playable game. Tracked JavaScript under `node_modules/` is dependency code.

Standard Godot 4 C# projects currently cannot be exported to the web, and web exports use Compatibility rather than Forward+/Mobile. A browser QA bridge avoids requiring a port; it does **not** convert native play into web gameplay. Recheck these platform constraints when planning a port. [Official Godot web export documentation](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_web.html).

The book uses three labels:

- **Existing native:** source symbols, Godot node paths, assets and verification flags that exist today.
- **Proposed bridge:** a deliberately new local browser inspection/control surface. All browser selectors and `__DEBUG_GAME__` members in this book belong to this category until Volume 1 is implemented.
- **Future feature:** persistent saves, multiplayer, touch controls, a browser port and other capabilities absent today. Missing prerequisites are blocked coverage, not a passing test.

Each fenced prompt can be copied on its own. File paths inside a prompt are relative to the absolute checkout it names. A future prompt authorises only its stated implementation and testing; it does not authorise pushing, deploying or unrelated fixes.

## Evidence map: existing native systems

| Area | Source of truth | Exact semantics to preserve |
| --- | --- | --- |
| Startup | `project.godot`, `Scenes/System/Bootstrap.tscn`, `Scripts/System/SceneBootstrap.cs` | Main scene is `res://Scenes/System/Bootstrap.tscn`. `SceneBootstrap.DefaultWorldScene` instantiates `res://Scenes/Hub/HubWorld.tscn` under `WorldRoot`. Initially the current scene is Bootstrap while the playable world is its `WorldRoot/HubWorld` descendant. |
| Autoloads | `project.godot` | `GameManager`, `ElainesSceneTransition`, `SimpleGrass`, `LovelyEvent`; there is no QA/debug autoload. |
| Global state | `Scripts/Core/GameManager.cs` | `Glimmers`, `FamilyRescuedCount`, `TotalFamilyMembers = 5`, `RescuedFamilyIds`, `BefriendedAnimals`, `TownClueIds`, `CompletedFantasyRealmIds`, `HasMagicRing`, `CanTranslateAnimals`, `CurrentObjective`, `PendingArrivalMarker`, `CheckpointId`, `CheckpointScenePath`, `CheckpointPosition`. Sets are in-memory `HashSet<string>` objects. |
| Global methods/signals | Same file | `AddGlimmers(int)` clamps to zero and returns void. `RescueFamilyMember(string)` and `BefriendAnimal(string)` deduplicate and return void. `FindTownClue(string)` and `CompleteFantasyRealm(string)` return bool. Signals include `GlimmersChanged`, `FamilyRescuedChanged`, `AnimalBefriended`, `TownClueFound`, `FantasyRealmCompleted`, `ObjectiveChanged`, `DialogueRequested`, `CheckpointChanged`. |
| Elaine | `Scripts/Player/ElaineController.cs`, `Scenes/Characters/Player/Elaine.tscn` | `CharacterBody3D`, group `player`, world-axis X/Z movement via `Input.GetVector`, acceleration/deceleration, ground jump, held-jump falling hover, `MoveAndSlide()`. `Visuals`, `AnimationTree`, `GlobalAnimationPlayer`, `Visuals/SpellOrigin`. |
| Elaine state/API | Same controller | `_currentHealth`, `_spellCooldownTimer`, `_pendingSpellTimer`, `_pendingSpellIndex`, `_castBufferTimer`, `_respawnPoint`, `_isHovering`, `_hoverTimer`, `_currentAnim` are private. Public methods include `GetCurrentHealth()`, `GetMaxHealth()`, `GetHealthRatio()`, `ReceiveDamage(float, Node)`, `RestoreHealth(float)`, `Respawn()`, `RespawnTo(Vector3)`, `SetRespawnPoint(Vector3)`, `SelectSpell(int)`, `TryCastSelectedSpell()`, `GetSelectedSpellIndex()`, `GetSpellCooldownRemaining()`, `GetSpellCooldownRatio()`, `GetSpellDisplayName()`, `GetSpellHudText()`. Elaine does not own a `CombatHealth` child. |
| Animation/spells | Same controller; `Scenes/Combat/` | States include `Idle`, `Walk`, `Run`, `Jump`, `Hover`, `Talk`, `Wave`, `Cast`, `CastBurst`, `CastLance`; clips use `Mixamo/...`. Index 0 Sun Ember releases one projectile, 1 Star Volley releases three, 2 Sky Lance releases one piercing projectile with two target hits. Signals: `HealthChanged`, `DamageTaken`, `SpellSelected`, `SpellCastStarted`, `SpellProjectilesReleased`, `SpellHit`, `InteractionPromptChanged`. |
| Combat | `Scripts/Combat/SpellProjectile.cs`, `CombatHurtbox.cs`, `CombatHealth.cs`, `EnemyCombatant.cs` | `SpellProjectile.Launch(...)`, hurtbox `ApplySpellHit(float)`, enemy `Health.ApplyDamage(float)`, per-projectile hurtbox deduplication, range cleanup, death delay. `GameManager.RequestHitStop()` temporarily changes `Engine.TimeScale`. |
| Interaction | `Scripts/Interactables/IPlayerInteractable.cs`, `LevelPortal.cs`, `TownClue.cs`, `Scripts/NPCs/NPC.cs` | Group `player_interaction`; Elaine selects a nearby valid interactable within `InteractionRadius = 4`. E opens/advances dialogue or enters a portal; portal interaction, not walking through alone, changes scenes. NPCs also join `npc_interaction`. |
| Dialogue | `Scripts/UI/DialogueUI.cs`, `Scenes/UI/DialogueUI.tscn` | `DialogueUI.Instance.Panel`, `NameLabel`, `DialogueLabel`; queued lines, E or Escape advances the queue and only hides the final line. Dialogue is not a game pause: movement physics are not globally paused. |
| HUD | `Scripts/UI/HUD.cs`, `Scenes/UI/HUD.tscn` | Native `CanvasLayer` with `Control/GlimmersLabel`, `FamilyLabel`, `HealthLabel`, `SpellLabel`, `CombatFeedLabel`, `InteractionPromptLabel`, `TownClueProgressLabel`, `ObjectiveLabel`; spell controls under `Control/SpellWheelContainer/SpellWheelMenu/SpellSlot0`, `SpellSlot1`, `SpellSlot2`. |
| HUD strings | Same HUD/controller | `Glimmers: N`; `Family Rescued: N / 5`; `Health: ceil(current) / ceil(max)`; `Town Clues: N / 3`; `Objective: ...` or empty; spell text comes from `GetSpellHudText()`. Runtime spell slots contain optional `> ` prefix, spell name, newline and role. |
| Spell wheel | Same HUD and `addons/advanced_radial_menu/radial_menu_class.gd` | Hold `spell_wheel`; wheel suppresses movement, uses movement/controller direction for selection, F for selection/casting. Native `mouse_enabled = false`, `controller_enabled = true`, and hold focus. Addon `move_back_action_name` defaults to `move_back`, while the project defines `move_backward`; HUD.tscn does not override that property. Test/report this existing mapping mismatch rather than assuming all wheel directions work. A QA HTML spell button is a proxy, not evidence of native mouse selection. |
| Layout | HUD and DialogueUI scenes | Many controls use absolute offsets: `FamilyLabel` X=924…1256; `ObjectiveLabel` Y=680…744; dialogue panel X=64…1216, Y=560…696; wheel is centred with a 420×420 container. These are risk inputs, not verified responsive layouts. |
| Forest | `Scenes/Levels/World_1_Forest.tscn`, `Scripts/Levels/ForestAdventureDirector.cs` | Root `World1Forest`. Toby → lower `SpellWispLower`, `JackoLower` → upper `SpellWispUpperA`, `JackoUpper`, `SpellWispUpperB` → `MatthewRescue` → `ReturnPortal`. `FirstEncounterGate` and `SecondEncounterGate` gate progress. Private booleans `_guideMet`, `_firstEncounterComplete`, `_secondEncounterComplete`, `_familyRescued`. Each clear calls `RestoreHealth(EncounterRecovery)` with default 1. |
| Rewards | `Collectible.cs`, `FamilyRescue.cs`, `TownClue.cs` | `GlimmerValue` defaults to 1; area entry awards and queues deletion. `FamilyRescue.BonusGlimmers` defaults to 10 but **Forest MatthewRescue overrides it to 20**. `TownClue.GlimmerReward` defaults to 1 and awards only when `FindTownClue()` returns true. |
| Checkpoint/defeat | `Checkpoint.cs`, `HazardVolume.cs`, Elaine controller | Checkpoint stores marker/global position and the actual `CurrentScene.SceneFilePath`. Explicit hazard `RespawnMarker` takes precedence; otherwise current respawn point. Lethal damage immediately calls `RespawnTo()`, resets velocity, pending cast and cooldown, restores full health and applies respawn invulnerability. There is no game-over screen. |
| Hub | `Scenes/Hub/HubWorld.tscn`, `Scripts/Hub/FamilyTree.cs`, `HubAnimalSpawner.cs` | `PortalToWorld1`, `TownTrailPortal`, `CaveTrailPortal`, `StarfallSanctumPortal`, `MatthewFinalLead/NPCData`, `FamilyTree`, `AnimalSpawner`. Tree stage clamps rescued count to available stages; family spawn selection currently follows count/array order, not rescued IDs. Animal spawner sorts IDs, has finite markers, defaults only Toby. |
| Town | `World_03_Town.tscn`, `TownAdventureDirector.cs` | `ClueHome`→`TownHomeNote`, `ClueStore`→`TownStoreReceipt`, `ClueSchool`→`TownSchoolMark`. All three exact clues awaken `FantasyPortal` with marker ID `TownPortalAwakened`; this marker is excluded by `GetTownClueCount()`. The getter otherwise counts unrelated IDs too; exact-clue unlock and displayed count are distinct contracts. |
| Later chapters | `MoonlitGardenRealm.tscn`, `World_02_Caves.tscn`, `StarfallSanctum.tscn`; respective directors | Realm IDs `MoonlitGarden`, `EchoingCaves`, `StarfallSanctum`. Moonlit opening enemies unlock `RustBucketBob`; Caves stage 0→1→2→3 progresses through `CorkrotAmbush`, `FungalPressure`, `BriarWarden`; final stage 0→1 defeats `StarfallGuardian`. Hub Cave portal requires MoonlitGarden, Starfall portal requires EchoingCaves. |
| Transitions | `Scripts/System/ElainesSceneTransition.cs`, `LevelPortal.cs` | Fade duration 0.18 seconds, private `_isTransitioning`; `QueueSceneArrival()`/`ConsumeSceneArrival()` apply named markers. Check actual destination/marker, not just a faded frame. |
| Assets | `RuntimeGltfModel.cs` and scene references | Elaine `res://Assets/Models/Elaine/Elaine-model.glb`; Toby `res://Assets/Models/Wildlife/Toby.glb`; animation library `res://Assets/Animations/MixamoLibrary.res`; combat `ElaineFirebolt.tscn`, `ElaineStarVolley.tscn`, `ElaineSkyLance.tscn`, `SpellImpact.tscn`; audio cue default `res://sounds/coin.ogg`. |
| Model loading | `Scripts/System/RuntimeGltfModel.cs` | Imported/cached/runtime GLTF loading, `ModelPath`, `PlaceholderRoot`, `GeneratedModelName = "__RuntimeModel"`. An invalid import remap takes the cached-scene branch; absence of an error is not proof a model loaded. Check generated descendant and required skeleton/animations; require a hidden placeholder only when PlaceholderRoot is assigned. Elaine and Toby currently leave PlaceholderRoot unassigned. |
| Existing diagnostics | `CombatSandboxVerifier.cs`, chapter directors, `EnemyCombatant.cs` | Native verification flags listed below; enemy `IsAnimationReady`, `HasAnimationClip()`, `HasResolvedAnimationClip()`, `DebugTravelAnimationState()`, `CaptureAnimationPose()`, `MeasureAnimationPoseDelta()`, `GetAnimationBindingSummary()`. Normal logs include `Glimmers:`, `Family Rescued:`, NPC speech and verification prefixes. |
| Utilities/addons | `Scripts/Check*.cs`, `Fix*.cs`, animation builders, `Scripts/dev_shots.gd`, shoreline scripts, `addons/`, `demo/` | Inspect scripts before running: many utilities rewrite scenes/animation libraries. `dev_shots.gd` is not a configured main-game autoload; its documented `shots=` command alone will not install it. Shoreline compute/Forward+ checks and addon demos do not prove adventure playability. |

### Existing control mapping

| Browser key to forward in a proposed QA page | Existing Godot action | Behaviour |
| --- | --- | --- |
| A / ArrowLeft; D / ArrowRight | `move_left`; `move_right` | X movement |
| W / ArrowUp; S / ArrowDown | `move_forward`; `move_backward` | Z movement |
| Space | `jump` | Ground jump; hold during descent for hover |
| E | `interact` | Nearby interactable or dialogue advance |
| F | `cast_magic` | Cast/buffer selected spell |
| Q | `emote` | Wave |
| Tab | `spell_wheel` | Hold wheel; prevent browser focus traversal only in QA input surface |
| Escape | `ui_cancel` | Advance/dismiss dialogue; no existing pause menu |

Native joypad bindings also exist in `project.godot`; browser keyboard forwarding does not certify Bluetooth/controller hardware or real-device touch.

### Native verification launch matrix — available now

Run from `/Users/kyleberry/Projects/ElainesQuest`, with an appropriate local Godot .NET installation. The inspected binary is `/Applications/Godot_mono.app/Contents/MacOS/Godot`. Build before launching; terminate a hung child process and preserve stdout, stderr and exit code. Do not pipe through a command that masks failure.

```sh
cd /Users/kyleberry/Projects/ElainesQuest
dotnet build ElainesQuest.sln
"/Applications/Godot_mono.app/Contents/MacOS/Godot" --path . res://Scenes/Levels/CombatSandbox.tscn -- --verify-spells
```

| Scene passed to Godot | User argument after `--` | Required terminal result | Suggested process deadline |
| --- | --- | --- | --- |
| `res://Scenes/Levels/CombatSandbox.tscn` | `--verify-spells` | Exit 0 and final `SPELL_VERIFY: PASS`; individual Sun Ember, Star Volley, Sky Lance success | 90 seconds |
| Same sandbox | `--verify-enemies` | Exit 0 and final `ENEMY_VERIFY: PASS`; Corkrot, Fungal Blight, RustBucketBob, Briar Warden contracts | 90 seconds |
| `res://Scenes/Levels/World_03_Town.tscn` | `--verify-town` | Exit 0 and `TOWN_VERIFY: PASS` | 30 seconds |
| `res://Scenes/Levels/MoonlitGardenRealm.tscn` | `--verify-moonlit` | Exit 0 and `MOONLIT_VERIFY: PASS` | 30 seconds |
| `res://Scenes/Levels/World_02_Caves.tscn` | `--verify-caves` | Exit 0 and `CAVE_VERIFY: PASS` | 30 seconds |
| `res://Scenes/Levels/StarfallSanctum.tscn` | `--verify-starfall` | Exit 0 and `STARFALL_VERIFY: PASS` | 30 seconds |

Use a subprocess timeout, for example Python `subprocess.run([...], timeout=90, capture_output=True, text=True)` with argument arrays. Never treat headless output as native visual proof. Build success validates C# compilation, not resources, input, collisions or rendering. Native verifiers directly damage enemies or seed state; their results are integration checks, not player-driven level completion.

## Proposed common bridge contract — specification, not existing API

V1.1–V1.4 instruct an agent to build this contract. Other browser prompts must first verify it exists and fail with a precise missing-prerequisite result if it does not. Do not install a mock that simply returns expected values.

### Transport and runtime

- Proposed opt-in user argument `--qa-bridge`, recognised only in debug builds. Bind `127.0.0.1` on port 4173 by default; proposed `--qa-port=4174` supports a second native process. Print the actual QA URL and native process/session identity. Never silently connect to an unrelated listener if the port is occupied.
- Serve `/qa/` and a same-origin command/snapshot/frame API from the local debug host. Browser JS sends allowlisted requests; enqueue engine operations and execute them on Godot’s main thread. A network worker must never dereference Godot nodes. Keep the browser as a client; no `window` exists inside native C#.
- Require the matching QA page origin and a per-launch capability token for commands, avoid wildcard CORS, and disable the host outside explicit debug opt-in. Do not expose arbitrary code execution, arbitrary file reads or arbitrary node setters.
- Commands have a unique request ID, session ID and scene epoch, a 5-second deadline, and a receipt `{requestId, sessionId, sceneEpoch, ok, value, error, sequence}`. Preserve the true method return in `value`, including `false`, zero and `null` for void. `ok` means dispatch succeeded, not that a cast returned true. Never automatically replay a mutating command after timeout/disconnect.
- `snapshot()` returns a fresh native snapshot promise. The host takes an immutable snapshot after UI processing, with a monotonic sequence, physics-frame number and session identity; the browser computes freshness using local receipt age, not clock subtraction across processes. Clear connected/ready state when samples are older than 2 seconds. During transitions return `player: null` where appropriate, rather than reusing freed nodes.

### Proposed JS methods and native mappings

| Proposed `window.__DEBUG_GAME__` member | Required mapping |
| --- | --- |
| `snapshot()` | Fresh, plain serialisable state described below; no direct live engine objects |
| `triggerInput(action, {pressed, strength = 1})` | Allowlisted `InputEventAction` through `Input.ParseInputEvent`; preserve pressed/released events for `_UnhandledInput` and held state for physics. Do not substitute only `Input.ActionPress` for interaction/casting. |
| `releaseInputs()` | Release all bridge-owned held keys/actions/buttons; also run on blur, disconnect, teardown and scene transition |
| `selectSpell(index)` | `ElaineController.SelectSpell(index)`; return its actual bool in receipt |
| `tryCastSelectedSpell()` | `ElaineController.TryCastSelectedSpell()`; acceptance is separate from later release/hit events |
| `addReward(amount)` | `GameManager.AddGlimmers(amount)`; report void as null plus before/after snapshots |
| `receiveDamage(amount)`; `restoreHealth(amount)` | `ElaineController.ReceiveDamage(amount)` and `RestoreHealth(amount)`; preserve bool and restored-float returns |
| `forceGameOver()` | Wait up to 3 seconds for damage invulnerability to expire, invoke lethal `ReceiveDamage(GetMaxHealth())`, then observe native respawn. Return failure if no damage was accepted. Never fabricate a game-over state. |
| `resizeViewport(width, height)` | Resize native window/viewport, report effective logical and rendered dimensions; reject unsupported geometry rather than resizing only HTML |
| `captureFrame()` | After a rendered frame, save actual native viewport PNG under `output/playwright/`, return frame ID, logical/rendered dimensions, scene epoch and same-origin image URL. Fail explicitly without a rendering device. |
| `pointer({type, x, y, button})` | Native viewport-local pointer move/down/up events; transform CSS image coordinates and letterboxing correctly; does not enable currently disabled mouse wheel selection |
| `fixture(name, args)` | Debug-only allowlist below; all usage appears in evidence as fixture-assisted coverage |

Allowed proposed fixtures: `loadScene({scenePath})` for this book’s adventure/sandbox scenes through existing transition logic; `placePlayer({position, lookAt})` sets position/velocity/facing without healing or awarding; `defeatEnemy({nodePath})` uses that enemy’s `CombatHealth.ApplyDamage()` and observes delayed cleanup; `requestDialogue({speaker, text})` calls `GameManager.RequestDialogue()`; `findTownClue({clueId})` and `completeFantasyRealm({realmId})` return existing method results; `arrangeSpellTargets({spellIndex})` prepares the sandbox target layout used by `CombatSandboxVerifier`, without manufacturing hits. The target fixture must first remove manual range targets/previous fixture targets so extra hurtboxes cannot contaminate counts. Resolve fixture node paths relative to the active world, reject queued/freed/missing nodes, and restrict paths/IDs to test scenarios (including `UnrelatedClue` for the negative unlock check). Extra fixture operations require explicit implementation and documentation; they do not already exist.

Snapshot shape uses **new bridge field names** below; each maps to native state instead of creating a second state manager:

```text
bridge: {version: 1, sessionId, sceneEpoch, sequence, physicsFrame, connected, ageMs}
scene: {currentScenePath, worldScenePath, worldNodePath, transitioning}
manager: {Glimmers, FamilyRescuedCount, TotalFamilyMembers, RescuedFamilyIds[],
          BefriendedAnimals[], TownClueIds[], CompletedFantasyRealmIds[], TownClueCount,
          HasMagicRing, CanTranslateAnimals, CurrentObjective, PendingArrivalMarker,
          CheckpointId, CheckpointScenePath, CheckpointPosition: {x,y,z}}
player: null OR {nodePath, GlobalPosition: {x,y,z}, Velocity: {x,y,z}, OnFloor,
          CurrentHealth, MaxHealth, SelectedSpellIndex, SpellDisplayName, SpellHudText,
          SpellCooldownRemaining, DamageInvulnerabilityRemaining, RespawnPoint: {x,y,z},
          AnimationState, OneShotState, OneShotRemaining, IsHovering, HoverRemaining,
          PendingSpellIndex, PendingSpellRemaining, CastBufferRemaining}
engine: {TimeScale}
ui: {nodes: {"HUD/Control/GlimmersLabel": {text, visible, rect: {x,y,width,height},
          mouseFilter, disabled}, ...}}
world: {nodes: {relativeNodePath: {exists, queuedForDeletion, visible, processMode,
          globalPosition, monitoring, monitorable, collisionShapes[], Health}, ...},
        forest: {guideMet, firstEncounterComplete, secondEncounterComplete, familyRescued},
        caveStage, finalStage}
events: [{seq, name, args, sceneEpoch, physicsFrame}, ...]
assets: [{path, status, modelNodePath, placeholderVisible, error}, ...]
viewport: {width, height, renderedWidth, renderedHeight, frameId}
```

Use sorted arrays for sets. Read private values through small debug-only read-only accessors in their owning class, not reflection, public mutable fields or hard-coded inferred success. `TownClueCount` maps to `GetTownClueCount()`; player health, spell and cooldown fields map to public getters. UI text/visibility/rectangles come from actual native controls (`GetGlobalRect()`, effective visibility), not strings reformatted from state by the browser. Resolve UI paths beneath the active world; record their full engine paths too. Event sequence numbers must never reset within a session; bound the event buffer and report truncation so an absent evicted event cannot pass a negative assertion.

### Proposed QA DOM — all selectors are new

| Locator | Meaning and limit |
| --- | --- |
| `page.locator('#BridgeStatus')` | Connected/stale/disconnected, current session; never just “page loaded” |
| `page.locator('#GameInput')` | Focusable native-input forwarding surface; surrounds rendered image; only this surface forwards gameplay keys |
| `page.locator('#NativeViewport')` | `<img>` displaying actual native captured frames with frame/epoch metadata; **not a game canvas** |
| `page.locator('[data-node-path="HUD/Control/GlimmersLabel"]')` | Inspection row mirroring native `GlimmersLabel.Text` |
| `page.locator('[data-node-path="DialogueUI/Control/Panel/DialogueLabel"]')` | Inspection row mirroring native dialogue text |
| `page.getByRole('button', {name: 'Select Star Volley', exact: true})` | QA proxy that calls `selectSpell(1)`, not a native button click |
| `page.getByRole('button', {name: 'Capture native frame', exact: true})` | QA capture control; does not prove native pointer hit testing |

Provide matching proxy buttons `Select Sun Ember`, `Select Sky Lance`, and one unique inspection row per native node. Keep inspection rows outside the input surface. Hidden native controls may remain in the inspector; assert native `visible` and rectangles, not inspector-row visibility. The image may letterbox but must not crop or distort the native frame.

### CLI-first setup and state-wait example — proposed bridge required

```sh
command -v npx >/dev/null 2>&1
export PWCLI="/Users/kyleberry/.codex/skills/playwright/scripts/playwright_cli.sh"
mkdir -p /Users/kyleberry/Projects/ElainesQuest/output/playwright/session
cd /Users/kyleberry/Projects/ElainesQuest/output/playwright/session
"$PWCLI" --session elaines-qa open http://127.0.0.1:4173/qa/ --headed
"$PWCLI" --session elaines-qa snapshot
"$PWCLI" --session elaines-qa tracing-start
```

If `npx` is unavailable, install Node/npm before using the wrapper. Select element references only from the latest snapshot; re-snapshot after navigation, dialogue changes or stale-ref failures. Use CLI `resize`, `keydown`, `keyup`, `press`, `console`, `network`, `screenshot` and tracing commands. Redirect/copy tool-generated artefacts into `output/playwright/<prompt-id>/` and record native captures there too. Use `run-code` only for telemetry/measurements or explicit locator assertions that CLI commands cannot express, after taking a fresh snapshot. No `@playwright/test` dependency is required for these CLI sessions.

The following is a `page` callback body for CLI `run-code`; it is conditional on Volume 1. No assertion package is assumed:

```js
await page.waitForFunction(async () => {
  const hook = window.__DEBUG_GAME__;
  if (!hook) return false;
  const s = await hook.snapshot();
  return s.bridge.connected && s.bridge.ageMs < 2000 &&
    s.scene.worldScenePath === 'res://Scenes/Levels/World_1_Forest.tscn' &&
    s.player !== null && !s.scene.transitioning;
}, null, {timeout: 15000, polling: 100});
const s = await page.evaluate(() => window.__DEBUG_GAME__.snapshot());
const counter = page.locator('[data-node-path="HUD/Control/GlimmersLabel"]');
await counter.waitFor({state: 'visible', timeout: 5000});
if (await counter.count() !== 1) throw new Error('Glimmers inspector row is not unique');
// Use a bounded retry for the DOM mirror; native s.ui remains the authoritative UI sample.
await page.waitForFunction(expected => {
  const el = document.querySelector('[data-node-path="HUD/Control/GlimmersLabel"]');
  return el?.textContent === expected;
}, s.ui.nodes['HUD/Control/GlimmersLabel'].text, {timeout: 5000});
```

Do not catch transport errors and turn them into passing conditions. Use 5 seconds per command, 15 seconds per scene transition, 5 seconds per ordinary state change, and the explicit longer budgets in individual prompts. Release inputs in `finally`. Track sample/event sequence numbers before an action, so old casts/rewards cannot satisfy new assertions.

---

## VOLUME 1: THE FOUNDATIONAL DEBUG & TELEMETRY KIT

### V1.1 — Expose live GameManager, Elaine and UI telemetry

```text
Work in /Users/kyleberry/Projects/ElainesQuest. Implement a debug-only native-to-browser QA telemetry bridge, using /Users/kyleberry/.codex/skills/playwright/SKILL.md for browser validation. Prerequisites: inspect git status and preserve existing WIP; verify npx, Godot .NET and dotnet are available. Read project.godot, Scripts/Core/GameManager.cs, Scripts/System/SceneBootstrap.cs, Scripts/Player/ElaineController.cs, Scripts/UI/HUD.cs and Scripts/UI/DialogueUI.cs. The game is native Godot C#; there is no existing browser hook.

1. Implement the proposed common bridge contract in PLAYWRIGHT_PROMPT_BOOK.md: explicit --qa-bridge debug opt-in, loopback port 4173, same-origin /qa/, per-launch session/token, bounded main-thread request queue and fresh snapshot(). Do not port the game or build a fake browser state manager. Without opt-in, no listener or browser telemetry is exposed.
2. Expose window.__DEBUG_GAME__.snapshot() as a promise for actual GameManager.Glimmers, FamilyRescuedCount, TotalFamilyMembers, all four progression sets, ring flags, CurrentObjective, PendingArrivalMarker and checkpoint fields. Add player GlobalPosition, Velocity, IsOnFloor(), GetCurrentHealth(), GetMaxHealth(), GetSelectedSpellIndex(), GetSpellCooldownRemaining() and GetSpellHudText(). Sort copied sets. Add narrowly scoped debug readers for private timers/state; do not make writable globals.
3. Find the active player in group player. Initially resolve worldScenePath beneath Bootstrap/WorldRoot/HubWorld instead of claiming CurrentScene.SceneFilePath is the Hub. Re-resolve after changes and return null player while unavailable. Read actual HUD/Control/GlimmersLabel, FamilyLabel, HealthLabel, SpellLabel, ObjectiveLabel and DialogueUI/Control/Panel values; never synthesize UI observations from manager values.
4. Build a QA inspector with unique proposed selectors #BridgeStatus, #GameInput, #NativeViewport and [data-node-path="HUD/Control/GlimmersLabel"]. Label native node paths separately from DOM selectors. Return sessionId, sceneEpoch, sequence, physicsFrame, connected and ageMs; mark snapshots stale after 2 seconds and make disconnect visible.
5. Build the C# solution. Launch the real Bootstrap path with -- --qa-bridge; open the printed /qa/ URL with the Playwright wrapper, take a fresh snapshot and start a trace. Use page.waitForFunction with a 15-second deadline for connected/fresh state, non-null player and worldScenePath === 'res://Scenes/Hub/HubWorld.tscn'. Verify the native and DOM-inspected GlimmersLabel are identical using page.locator('[data-node-path="HUD/Control/GlimmersLabel"]'), then close the native process and require stale/disconnected state within 3 seconds.

Evidence: output/playwright/V1.1/ with commands, source diff, build result, native stdout/stderr, two fresh snapshot JSON files, browser screenshot and trace. Fail on missing fields, fabricated UI, thread-unsafe access, stale snapshots reported connected, unexpected listener without opt-in, or missing prerequisites. A C# build and inspector screenshot alone do not prove rendered gameplay. Do not push or deploy.
```

### V1.2 — Add input and simulation commands with true return values

```text
Work in /Users/kyleberry/Projects/ElainesQuest; preserve WIP. Use /Users/kyleberry/.codex/skills/playwright/SKILL.md. Prerequisite: a real debug-only --qa-bridge host implementing PLAYWRIGHT_PROMPT_BOOK.md's snapshot contract; verify that capability and session identity first. If absent, report V1.1 as required rather than mocking telemetry. Read ElaineController._UnhandledInput(), _PhysicsProcess(), ReceiveDamage(), RestoreHealth(), RespawnTo(), SelectSpell(), TryCastSelectedSpell(), GameManager.AddGlimmers(), LevelPortal.Interact() and ElainesSceneTransition.TransitionTo().

1. Implement proposed triggerInput(action,{pressed,strength}), releaseInputs(), selectSpell(index), tryCastSelectedSpell(), addReward(amount), receiveDamage(amount), restoreHealth(amount), forceGameOver() and the book's allowlisted fixture(name,args). Dispatch on the main thread; validate finite numbers, action names, session/epoch and paths. Preserve native returns in {ok,value,error,...} receipts: false is not transport failure; void is null. Use request IDs and do not retry mutating calls automatically.
2. Feed InputEventAction through Input.ParseInputEvent so E/F/Q reach _UnhandledInput as well as held movement reaching physics. Press and release must span physics processing when needed. releaseInputs must run in finally and on scene transition, blur and disconnect. Direct method calls and fixture teleports must be labelled integration setup, not keyboard play.
3. addReward calls AddGlimmers and observes GlimmersChanged/HUD; it must not directly assign Glimmers. forceGameOver waits at most 3 seconds for _damageInvulnerabilityTimer to expire, calls lethal ReceiveDamage(GetMaxHealth()), then observes RespawnTo full health, zero velocity and cancelled pending cast. There is no game-over screen. Do not bypass invulnerability to make the command pass.
4. In an isolated fresh process, open the printed QA URL using the CLI wrapper, snapshot and trace. Demonstrate addReward(3) changes Glimmers by 3 within 5 seconds, selectSpell(-1) returns false without changing selection, and selectSpell(1) returns true. Drain dialogue and wait for grounded/idle/cooldown-ready state before tryCastSelectedSpell(); record its bool and later SpellProjectilesReleased separately. Exercise triggerInput('move_right',{pressed:true,strength:1}) until X increases by at least 0.5 within 2 seconds on safe ground, then release in finally. Require a native pressed event and position change, not just a receipt.
5. Subscribe to real HealthChanged, DamageTaken, SpellCastStarted, SpellProjectilesReleased and SpellHit events before actions, with monotonic sequence/epoch metadata. Cover duplicate request IDs without duplicate side effects and reject stale-epoch commands rather than controlling a replacement Elaine.

Evidence: output/playwright/V1.2/ with per-command receipts, before/after native snapshots, event sequence, trace, stdout/stderr and relevant native regression results. Fail on input stuck after cleanup, duplicate reward, invalid return conversion, manufactured hits, a 5-second command timeout or a lethal-damage test asserted as a persistent dead state. Build and run appropriate checks; do not push/deploy.
```

### V1.3 — Capture native frames, control geometry and browser input forwarding

```text
Work in /Users/kyleberry/Projects/ElainesQuest. Use /Users/kyleberry/.codex/skills/playwright/SKILL.md; preserve unrelated changes. Prerequisites: real --qa-bridge snapshot and command dispatch from V1.1/V1.2. Read Scenes/UI/HUD.tscn, Scenes/UI/DialogueUI.tscn, Scripts/UI/HUD.cs, Scripts/UI/DialogueUI.cs, Scripts/Camera/PlatformerCamera.cs and project.godot's input mappings. This game has native Controls, not DOM HUD elements.

1. Implement proposed resizeViewport(width,height), captureFrame() and pointer({type,x,y,button}) plus QA #GameInput keyboard forwarding. #NativeViewport is an image of an actual native frame, never a synthetic HUD/canvas recreation. Capture after rendering and report frameId, sceneEpoch, native logical dimensions and rendered pixel dimensions. Return explicit unsupported status when no rendering device is available.
2. Sample actual effective visibility and GetGlobalRect() for HUD/Control labels, HUD/Control/SpellWheelContainer/SpellWheelMenu/SpellSlot0..2 and DialogueUI/Control/Panel plus its NameLabel/DialogueLabel. Include native mouse_filter and disabled status. Preserve scene hierarchy/transforms and distinguish logical coordinates from image pixels, browser CSS scaling and letterbox offsets.
3. Add unique QA controls discoverable by page.getByRole('button',{name:'Select Star Volley',exact:true}) and 'Capture native frame'. Selection proxies call selectSpell(1); do not pretend they click native SpellSlot1. HUD.tscn has mouse_enabled=false on the wheel. Forward WASD/arrows, Space, E, F, Q, Tab and Escape only while #GameInput is focused. Suppress browser defaults there, avoid repeat-triggering discrete keydowns, and release all inputs on blur/disconnect/finally. Do not add an unrequested pause or mouse camera system.
4. In a CLI headed session, take a fresh snapshot before interactions. Resize browser and native viewport to 1280x720; wait up to 5 seconds for reported effective dimensions or fail explicitly. Click #GameInput, hold/release D using page.keyboard, and require native X motion within 2 seconds. Click the QA Star Volley proxy and require SelectedSpellIndex===1 within 5 seconds. Capture a native frame, compare frame metadata and image bounds, then blur while D is held and prove velocity settles on safe ground.
5. Exercise pointer down/move/up on the native image using page.mouse and verify forwarded native coordinates/events; do not infer native dragging or spell selection support from event delivery alone. Capture both browser chrome/inspector and actual game PNG.

Evidence: output/playwright/V1.3/ with geometry JSON, input receipts, coordinate-transform samples, native PNG, browser screenshot and trace. Fail on CSS-only resizing reported as native success, stale frames, distorted/cropped frames, stuck inputs, duplicate QA locators or proxy clicks claimed as native interaction. Run build and focused checks; no push/deploy.
```

### V1.4 — Make bridge lifecycle and asynchronous events trustworthy

```text
Work in /Users/kyleberry/Projects/ElainesQuest with /Users/kyleberry/.codex/skills/playwright/SKILL.md. Prerequisites: V1.1–V1.3's real opt-in bridge; check snapshot version/session first. Preserve WIP. Read GameManager signals, HUD.ResolvePlayer()/DisconnectPlayer(), DialogueUI._ExitTree(), LevelPortal.Interact(), ElainesSceneTransition.TransitionTo() and SceneBootstrap. There is no existing browser bridge to assume without checking.

1. Audit/repair the debug bridge's event subscriptions, queue bounds, 5-second command deadlines, unique request IDs and monotonic event sequence. Track sceneEpoch, bind the actual active-world Elaine once, unsubscribe old nodes and reject queued operations against an old session/epoch. Treat a legitimate transition as potentially having no player; never retain freed Godot objects.
2. Maintain a bounded event ring with truncation metadata for GlimmersChanged, FamilyRescuedChanged, AnimalBefriended, ObjectiveChanged, CheckpointChanged and player damage/spell events. Record actual args and frame numbers. A pending spell with no release is not successful because SpellCastStarted fired. Do not replay a timed-out mutation after reconnect.
3. Use CLI open/snapshot/trace on /qa/, then perform five fixture-assisted Hub→Forest→Hub cycles with existing transition logic, allowing 15 seconds per scene. Require fresh scene epoch/world path, one active Elaine and one event listener contribution per action after settling. Add one glimmer after each transition and require exactly +1, one GlimmersChanged event and matching actual HUD text within 5 seconds.
4. Disconnect the QA page while movement is held; verify bridge-owned input releases. Restart the native process on the same port and require a different sessionId; old receipts/events must not satisfy new waits. If the port is occupied by another service, require a clear startup error, not a connection to it. Stop/restart transport during a reward command: record an indeterminate result and re-read native state rather than resubmitting.
5. Launch a debug game without --qa-bridge and a release build, checking that this QA listener is absent. Keep the test limited to the inspected local process; do not kill unrelated services. Use process-scoped teardown and preserve failure evidence before cleanup.

Evidence: output/playwright/V1.4/ containing lifecycle log, event counts, session/epoch transitions, command receipts, trace and teardown checks. Fail on duplicate subscriptions, stale success, leaked held input, unbounded queues, unexpected listener or an action replay. Build and focused lifecycle/native checks must pass; do not push/deploy.
```

## VOLUME 2: ASSET VALIDATION & RESPONSIVE LAYOUT SMOKE TESTS

### V2.1 — Validate boot resources and real model delivery

```text
Audit /Users/kyleberry/Projects/ElainesQuest without changing gameplay. Use /Users/kyleberry/.codex/skills/playwright/SKILL.md; preserve WIP. Existing-native prerequisites: Godot .NET, dotnet and source assets. Browser prerequisites: the real Volume 1 --qa-bridge with assets telemetry and captureFrame(); if missing, run native checks and explicitly mark browser coverage blocked. Read Scripts/System/RuntimeGltfModel.cs, Scenes/System/Bootstrap.tscn, Scenes/Hub/HubWorld.tscn, Scenes/Levels/World_1_Forest.tscn and character/combat scene references.

1. Trace ext_resource and runtime ModelPath/LibraryToLoad references transitively from Bootstrap→Hub and Forest. Verify exact case and existence of res://Assets/Models/Elaine/Elaine-model.glb, res://Assets/Models/Wildlife/Toby.glb, res://Assets/Animations/MixamoLibrary.res, res://Scenes/Combat/ElaineFirebolt.tscn, ElaineStarVolley.tscn, ElaineSkyLance.tscn, SpellImpact.tscn and res://sounds/coin.ogg. Resolve abbreviated combat names under res://Scenes/Combat/. Inspect GLTF texture sidecars and imported resource dependencies too.
2. Launch the real Bootstrap path; preserve stdout/stderr and detect missing-resource/import/GDExtension/shader errors. RuntimeGltfModel can leave a visible placeholder when one is assigned and loading fails; require Visuals/__RuntimeModel to exist with required skeleton/animation data, and PlaceholderRoot to hide only when assigned. Elaine and Toby currently have no assigned PlaceholderRoot: record placeholder visibility as not applicable, still require their actual model. The invalid-remap cached branch must be inspected, not assumed to fall through to runtime GLTF. Do not delete .godot or rewrite imports as an initial diagnostic.
3. For the proposed QA page, register page.on('response') and page.on('requestfailed') before page.goto(); collect all HTTP statuses >=400 and failed requests during boot and Hub→Forest. Include stylesheet/script/image delivery if requested. Do not expect native res:// GLBs to appear in browser HTTP logs; correlate native asset records separately.
4. With a fresh CLI snapshot and trace, require connected/fresh native Hub state within 15 seconds after game-ready logging, load Forest through an explicitly labelled fixture and apply the same readiness deadline. Capture actual native frames. Distinguish cold-import timeout from pass; record an incomplete boot rather than silently extending every timeout. Report supplied rendering/model problems even when dotnet build succeeds.

Evidence: output/playwright/V2.1/ with resource manifest, missing/case-mismatch list, browser network log, native asset results, stdout/stderr, native PNGs and trace. Fail on any crucial missing asset, visible fallback for required character models, failed resource dependency, HTTP boot failure or missing prerequisites claimed as success. No pushes/deployment; propose fixes separately unless explicitly requested.
```

### V2.2 — Run a real native viewport and browser layout matrix

```text
Audit /Users/kyleberry/Projects/ElainesQuest using /Users/kyleberry/.codex/skills/playwright/SKILL.md. Prerequisites: real Volume 1 bridge with resizeViewport(), captureFrame() and actual ui.nodes control rectangles; fail with missing capability if absent. Preserve WIP; do not fix layouts in this audit. Read Scenes/UI/HUD.tscn, Scenes/UI/DialogueUI.tscn and project.godot display/stretch settings (record defaults if unset).

1. Open the printed /qa/ URL headed, snapshot and trace. For each viewport 1440x900, 1280x720, 768x1024, 390x844 and 844x390, set the browser size using CLI resize/page.setViewportSize and call window.__DEBUG_GAME__.resizeViewport(width,height). Require actual native logical/rendered dimensions within 5 seconds. An OS size clamp is an explicit unsupported geometry, not a responsive pass.
2. For fresh Hub and Forest states, read ui.nodes for HUD/Control/GlimmersLabel, FamilyLabel, HealthLabel, SpellLabel, ObjectiveLabel, InteractionPromptLabel and TownClueProgressLabel. Check only controls effectively visible in the native sample; rectangle edges must lie within the reported native safe rectangle (default full viewport with 8 logical pixels margin). Report the exact node/viewport/overflow. Absolute FamilyLabel X=924..1256 and ObjectiveLabel Y=680..744 deserve direct checks.
3. Compare native frame aspect ratio with page.locator('#NativeViewport').boundingBox(); require no cropping or unequal X/Y scale beyond 1% and no horizontal browser overflow beyond 1 CSS pixel. Letterboxing is valid only with correct pointer coordinate offsets. A browser image shrinking successfully does not prove the underlying native controls fit.
4. After each resize, wait for a frame newer than the resize receipt, captureFrame(), and capture the browser inspector separately. Read text minimum sizes/line counts or inspect rendered PNGs to detect truncated text that a bounding box alone misses. A DOM inspector row fitting the page cannot substitute for native HUD fitting.

Evidence: output/playwright/V2.2/ with a five-size result table, native/browser dimensions, per-control rectangles, actual native PNGs, browser screenshots and trace. Fail on native clipping, unreadable/overlapping counters, unsupported size recorded as pass, stale captures or altered aspect ratio. Report desktop-window resizing separately from actual mobile/tablet hardware verification. Leave source unchanged.
```

### V2.3 — Check dialogue and spell-wheel safe zones and input priority

```text
Audit /Users/kyleberry/Projects/ElainesQuest with /Users/kyleberry/.codex/skills/playwright/SKILL.md. Prerequisites: real Volume 1 bridge with input forwarding, fixture('requestDialogue',...), geometry snapshots and native capture. Preserve WIP. Read Scripts/UI/HUD.cs, Scripts/UI/DialogueUI.cs, Scenes/UI/HUD.tscn, Scenes/UI/DialogueUI.tscn and ElaineController._UnhandledInput(). Do not invent pause, upgrade or start buttons; MainMenu.tscn is a label-only unused scene.

1. At 1280x720, 390x844 and 844x390, resize both browser and native viewport and wait up to 5 seconds for a fresh frame. Snapshot before CLI interactions. Queue two real dialogues through GameManager.RequestDialogue, speakers 'Toby' then 'Magic Ring'. Require native DialogueUI/Control/Panel visible and NameLabel/DialogueLabel matching the current queued line. E or Escape advances the first line; a second press dismisses the last. Bound each update at 5 seconds.
2. Focus proposed #GameInput, hold Tab and verify HUD/Control/SpellWheelContainer remains hidden while dialogue is open. Capture the actual native panel. Check its rectangle (source offsets 64,560..1216,696), both text rectangles, and intersection with HUD/Control/InteractionPromptLabel and ObjectiveLabel. Flag overlap that obscures essential information or an actionable control; do not fail decorative containment automatically.
3. After dialogue fully closes, hold Tab and require the wheel visible. Check SpellSlot0/1/2 under HUD/Control/SpellWheelContainer/SpellWheelMenu fit native safe bounds and do not obscure another simultaneously actionable target. Test held movement while the wheel is open: native horizontal motion must decelerate then remain stationary; directional input may change selection. Test every configured wheel direction: addon move_back_action_name defaults to move_back while project.godot defines move_backward and HUD.tscn does not override it. Capture any resulting error/selection failure as an existing mapping defect; do not silently remap input in the bridge. Release Tab in finally.
4. Native wheel mouse_enabled=false; page.getByRole('button',{name:'Select Sky Lance',exact:true}) is only a QA API proxy. Record native selection through movement/controller separately. Use page.mouse down/move/up only to verify forwarding/hit-test telemetry; do not assert mouse/drag-based spell selection as current functionality. The scene transition overlay ignores mouse input and is not a native modal blocker.

Evidence: output/playwright/V2.3/ with queued-line state, native control rectangles/intersections, input/event logs, PNGs for dialogue and wheel, trace and any clipping findings. Fail on lost/duplicated lines, wheel appearing during dialogue, essential text clipping, stuck Tab or unexplained native hit-test differences. Do not claim dialogue globally pauses movement; report actual behaviour. No source changes/push.
```

### V2.4 — Smoke-test every playable chapter and arrival asset handoff

```text
Audit /Users/kyleberry/Projects/ElainesQuest; use /Users/kyleberry/.codex/skills/playwright/SKILL.md and preserve WIP. Prerequisites: native Godot .NET and a real Volume 1 bridge supporting loadScene, snapshots, asset records and captureFrame. Read HubWorld.tscn, World_1_Forest.tscn, World_03_Town.tscn, MoonlitGardenRealm.tscn, World_02_Caves.tscn, StarfallSanctum.tscn, their directors, LevelPortal.cs and SpaceAudioCue.cs. Starfall files may still be untracked; include their working-tree semantics.

1. In isolated fresh runs, boot each scene directly with an explicit native scene argument and -- --qa-bridge, using unique sessions/ports or sequential clean teardown. Collect native stdout/stderr from before boot; install browser response/requestfailed/console/pageerror listeners before navigation to the printed /qa/ URL. A QA page returning 200 is not a loaded chapter.
2. Within 15 seconds after game-ready, require the correct scene.worldScenePath, one active player, actual HUD/DialogueUI nodes, a fresh native frame and current objective from that chapter. Verify required environment/character resources load and capture shows a world with Elaine, not just a placeholder inspector. Record null/queued nodes as failures rather than skipping assertions.
3. Inspect exact portal contracts: Town FantasyPortal targets res://Scenes/Levels/MoonlitGardenRealm.tscn with MoonlitArrival; Hub CaveTrailPortal requires MoonlitGarden and targets World_02_Caves with CaveArrival; StarfallSanctumPortal requires EchoingCaves and targets StarfallSanctum with StarfallArrival; final ReturnPortal targets Hub with StarfallReturnArrival. Verify those destination Marker3D nodes actually exist. Fixture-load coverage does not certify locked portal entry.
4. Validate SpaceAudioCue.CuePath resource resolution, default res://sounds/coin.ogg, and record AudioStreamPlayer startup where applicable. This is an audio-resource check, not proof sound was audible. Run applicable native verification flags separately with process deadlines: sandbox spells/enemies 90 seconds each, chapter verifiers 30 seconds each. Require exit 0 plus final PASS marker.

Evidence: output/playwright/V2.4/ with per-scene boot/asset/marker table, native frames, network/log errors, verifier exits and trace. Fail on a wrong chapter/marker, missing model/audio resource, unready player, renderer failure or timeout; label audible audio and real devices unverified. Do not modify assets, source, imports or delivery state.
```

## VOLUME 3: CORE GAMEPLAY LOOP & PLAYABILITY AUTOMATION

### V3.1 — Play Hub → Forest → Toby through real input

```text
Test /Users/kyleberry/Projects/ElainesQuest with /Users/kyleberry/.codex/skills/playwright/SKILL.md. Prerequisites: real Volume 1 bridge with keyboard forwarding, fresh snapshots, world node positions, event history and native captures; report missing bridge rather than inventing DOM game controls. Preserve WIP. Read SceneBootstrap.cs, HubWorld.tscn, World_1_Forest.tscn, NPC.cs, LevelPortal.cs and ElaineController.TryInteract()/UpdateNearbyInteraction(). Start a fresh native Bootstrap process with --qa-bridge so Toby is not already befriended.

1. Open printed /qa/ headed, snapshot and start trace. Require scene.currentScenePath === 'res://Scenes/System/Bootstrap.tscn' and worldScenePath === 'res://Scenes/Hub/HubWorld.tscn', a grounded player and no dialogue within 15 seconds. Do not look for a DOM Start button; MainMenu is not in this boot path.
2. Use page.locator('#GameInput').click() after a fresh snapshot, then paired page.keyboard.down/up for A/D and W/S or arrows. Navigate by observed player/PortalToWorld1 global positions and collision geometry; use short, bounded holds and re-read position, not a hard-coded route through unverified terrain. Require progress toward the portal; fail if no displacement after 2 seconds or if unreachable within 60 seconds. Always release keys in finally.
3. Within InteractionRadius 4, require actual HUD/Control/InteractionPromptLabel for portal entry; press E once. Use page.waitForFunction with a 15-second bound for fresh worldScenePath === 'res://Scenes/Levels/World_1_Forest.tscn', non-null replacement Elaine and transition complete. No teleport or fixture load counts as this entry test.
4. Move from Forest's PlayerSpawn (-20,1.2,0) toward Toby (approximately -12,0.8,-3.2; verify current transforms). Resolve the actual nearest interactable; require prompt 'E  Talk', then press E. Within 5 seconds require BefriendedAnimals includes 'Toby', native NameLabel is Toby and the objective becomes 'Hold Tab to choose Star Volley, then clear the lower clearing and break the ridge seal.' Advance queued dialogue with bounded individual E/Escape presses; do not hold E.

Evidence: output/playwright/V3.1/ with position/input timeline, portal/Toby interaction events, objective snapshots, native captures and trace. Fail on unreachable portal, wrong nearest interactable, stale world path, no friendship, lost dialogue or timeout. If using fixture('placePlayer',...) for diagnosis, label that separate integration reproduction and keep the input-playthrough result failed/incomplete. No code edits or push.
```

### V3.2 — Prove traversal, hover, spell-wheel movement gating and respawn

```text
Test /Users/kyleberry/Projects/ElainesQuest using /Users/kyleberry/.codex/skills/playwright/SKILL.md; preserve WIP. Prerequisites: real Volume 1 bridge with player timers/animation/position, native input forwarding and captureFrame. Read ElaineController._PhysicsProcess(), RespawnTo(), RestoreHealth(), Checkpoint.cs, HazardVolume.cs, Scenes/Levels/World_1_Forest.tscn and HUD.cs. Start Forest fresh; an initial fixture placement may create an isolated safe-ground test, but label fixture assistance and do not call it a complete traversal pass.

1. Focus proposed page.locator('#GameInput') following a fresh CLI snapshot. On confirmed ground, hold D until X increases at least 0.5 within 2 seconds; release and require horizontal speed <0.1 within 2 seconds using Acceleration/GroundDeceleration semantics. Test diagonal input is normalised rather than faster than Speed. Record measured trajectories/tolerances.
2. Tap Space and require positive Y velocity followed by leaving ground within 2 seconds. On a separate jump hold Space into descent and require IsHovering, HoverRemaining decreasing from at most HoverDuration=2, and reduced downward acceleration using HoverGravityModifier=0.2. Require return to ground within 8 seconds and hover recharge on ground. Do not expect the starter README's double jump: this controller implements ground jump plus hover.
3. Hold Tab on ground and use directional keys to select a spell. Require wheel visible and horizontal velocity decelerating to <0.1 within 2 seconds; a QA 'Select Star Volley' button is not evidence of native wheel selection. Release all keys. Press Q while idle to exercise Wave; require the animation state/event observation without using it as proof the skeleton visibly animates.
4. Reach a real Checkpoint area by input, record its Name, stored CurrentScene.SceneFilePath and respawn point. Enter HazardVolume by input, or separately exercise fixture placement outside then physical movement into the area. Require actual overlap dispatch and respawn within 5 seconds. Compare respawn position within 0.25 units to explicit hazard marker if assigned, otherwise the stored point; require full health, near-zero initial velocity, no pending spell and invulnerability. Report any Bootstrap-vs-world checkpoint path difference rather than changing the assertion silently.

Evidence: output/playwright/V3.2/ with frame/time/position/velocity samples, checkpoint and hazard receipts, native PNGs and trace. Fail on airborne extra jump assumed as valid, hover timer misuse, wheel movement leak, wrong respawn, stuck inputs or bounded-wait failure. Separate native animation/render evidence and real hardware/controller evidence; no edits/push.
```

### V3.3 — Verify the three spells from acceptance to hits and cleanup

```text
Test /Users/kyleberry/Projects/ElainesQuest with /Users/kyleberry/.codex/skills/playwright/SKILL.md. Preserve WIP. Native prerequisites: Godot .NET/dotnet. Browser prerequisites: real Volume 1 bridge with selectSpell(), tryCastSelectedSpell(), fixture('arrangeSpellTargets',...), fresh events and native snapshots; mark browser coverage blocked if absent. Read ElaineController.cs, SpellProjectile.cs, CombatHealth.cs, CombatHurtbox.cs and Scripts/Levels/CombatSandboxVerifier.cs.

1. Build and run the existing native sandbox command with a 90-second subprocess deadline: '/Applications/Godot_mono.app/Contents/MacOS/Godot' --path . res://Scenes/Levels/CombatSandbox.tscn -- --verify-spells. Require exit 0 and final SPELL_VERIFY: PASS plus all three individual results. Preserve stdout/stderr and timeout separately from assertion failure.
2. Start a separate manual sandbox with --qa-bridge (no --verify-spells auto-exit flag), open /qa/ using the wrapper, snapshot and trace. Arrange isolated real CombatHurtbox/CombatHealth targets through the debug fixture matching existing verifier geometry: one target at 6 units for Sun Ember, left/centre/right at ±18°/0° and 6 units for Star Volley, collinear targets at 5 and 9 units for Sky Lance. Fixture setup must not modify damage/hit outcomes.
3. For indices 0,1,2 select the spell, require grounded, dialogue closed, one-shot inactive and cooldown ready, then record event sequence and target health. Call tryCastSelectedSpell(); preserve receipt.value===true. Within 5 seconds require a NEW matching SpellProjectilesReleased event: counts 1,3,1 respectively. Within 5 seconds require NEW matching SpellHit events and actual health decreases on 1,3,2 targets. Sky Lance's two hits must be distinct hurtboxes; verify no same-projectile duplicate damage. Accepted casting alone cannot pass.
4. Repeat through actual #GameInput F press/release. Attempt another cast during cooldown and confirm native return false/direct-call case; separately test F's CastBufferDuration behaviour near cooldown completion without equating it to the direct call. Airborne movement cancels pending release, so verify that scenario explicitly with sequence-bound events. After range/death deadlines, assert spent projectiles/targets are freed as appropriate. Keep direct API and input results separate.

Evidence: output/playwright/V3.3/ with native verifier result, command returns, per-cast event IDs, target health deltas, projectile lifecycle records, real native capture and trace. Fail on missing/extra releases, stale events, wrong hit counts, damage without a hurtbox event, cleanup leak or timeout. Use 90 seconds native and 60 seconds per browser spell case; do not push/deploy.
```

### V3.4 — Complete both Forest encounters, rescue Matthew and return home

```text
Test /Users/kyleberry/Projects/ElainesQuest using /Users/kyleberry/.codex/skills/playwright/SKILL.md. Prerequisites: real Volume 1 input/snapshot/capture bridge and a fresh process; preserve WIP. Read ForestAdventureDirector.cs, World_1_Forest.tscn, ElaineController.cs, FamilyRescue.cs, LevelPortal.cs, FamilyTree.cs and HubAnimalSpawner.cs. Entry may be a fixture-load for focused Forest testing, but the run must label that setup. Do not grant Toby friendship or rescue flags directly during the playthrough.

1. In Forest, reach/interact with Toby using E and drain dialogue. Observe forest.guideMet and actual lower enemies becoming active: SpellWispLower and JackoLower. Hold Tab, use movement direction to choose Star Volley, release Tab, and verify SelectedSpellIndex===1. In separate diagnostic proxy coverage use page.getByRole('button',{name:'Select Star Volley',exact:true}); never confuse that with native wheel coverage.
2. Fight with grounded F casts and movement/dodging, checking enemy Health and cleanup. Allow 120 seconds per encounter and fail if no measurable progress for 15 seconds. After lower enemies are freed/queued, require FirstEncounterGate inactive and second encounter active within 5 seconds. Verify healing by min(EncounterRecovery, MaxHealth-healthBefore) only after accounting for intervening damage; drain Forest Trail/Magic Ring dialogue.
3. Traverse the ridge/bridge with movement/jump/hover. Select Sky Lance (index 2), defeat SpellWispUpperA, JackoUpper and SpellWispUpperB through real projectile hits. Require SecondEncounterGate inactive, MatthewRescue active and objective exactly 'Rescue Matthew at the lantern overlook.' before rescue. Wait for deferred collision/monitoring changes; visibility alone does not certify a gate stops blocking.
4. Record a glimmer baseline isolated from collectible overlaps, enter MatthewRescue physically and require RescuedFamilyIds includes 'Matthew', FamilyRescuedCount===1 in this fresh run and exactly +20 glimmers within 5 seconds. Require ReturnPortal active and objective 'Matthew is safe. Return to the Hub and help the Family Tree bloom.' Enter the portal with E and wait up to 15 seconds for fresh Hub state. Verify progression survives, one spawned family member and Toby in group hub_spawned_animal after deferred cleanup; report native family count/array-order spawning as implemented.

Evidence: output/playwright/V3.4/ with input route, enemy health/hit/deletion events, gate collisions, reward isolation, actual HUD values, Hub spawn counts, native captures and trace. Fail on any skipped gate, wrong reward, unhandled dialogue, stale state, unreachable traversal or timeout. A fixture('defeatEnemy',...) reproduction may diagnose director logic separately; it cannot replace a failed combat/playability result. No edits/push.
```

## VOLUME 4: VALUE RETURN & REWARD SYSTEM VERIFICATION

### V4.1 — Check glimmer arithmetic and collectible single-award behaviour

```text
Test /Users/kyleberry/Projects/ElainesQuest with /Users/kyleberry/.codex/skills/playwright/SKILL.md; preserve WIP. Prerequisite: real Volume 1 bridge with addReward(), snapshots/events, native HUD text and input; if absent report blocked automation. Read GameManager.AddGlimmers(), Scripts/Interactables/Collectible.cs, Scenes/Interactables/Collectible.tscn, World_1_Forest.tscn and HUD.cs. Start a fresh isolated native process; disable nothing in production code.

1. Record Glimmers=0, event sequence and actual HUD/Control/GlimmersLabel.Text. Call proposed addReward(3), addReward(0), addReward(-99). Preserve receipt.value===null because native AddGlimmers returns void. Require values 3,3,0 and label strings 'Glimmers: 3','Glimmers: 3','Glimmers: 0'. Amount zero must not emit GlimmersChanged; negative nonzero clamps to zero and does emit. Observe complete event windows with no ring truncation, within 5 seconds per assertion.
2. Test a real Forest CollectibleStartA, using its instantiated GlimmerValue (default 1 unless overridden), not the debug addReward command. Record the baseline after all setup is stable. Walk into its collision area with #GameInput; an isolated fixture placePlayer outside then movement into it is allowed but labelled. Require exactly GlimmerValue delta, one reward event and actual node deletion after physics/deferred processing. Leave and re-enter the same location and require no second award from the consumed instance.
3. Use page.locator('[data-node-path="HUD/Control/GlimmersLabel"]') only as the proposed QA mirror. Assert uniqueness, compare its text to the snapshot's actual native label with a bounded 5-second wait, and independently compare that label to `Glimmers: ${s.manager.Glimmers}`. Never have the inspector regenerate a matching string from manager state.
4. Try simultaneous/repeated overlap delivery in a focused native regression fixture and check for double-award before QueueFree completes. If a race exists, preserve a failing reproduction and report it; don't change expected delta to accept duplicates. Record baseline/current sequences to exclude incidental nearby glimmers. Bound this scenario at 30 seconds with finally input release.

Evidence: output/playwright/V4.1/ with true return values, event counts, collectible node identity/lifecycle, arithmetic table, native/DOM text comparisons, trace and native frame. Fail on extra reward, clamping mismatch, label mismatch, stale events or insufficient event history. Do not implement an economy or save feature or push changes under this test request.
```

### V4.2 — Verify Matthew rescue, friendship deduplication and Hub value

```text
Test /Users/kyleberry/Projects/ElainesQuest with /Users/kyleberry/.codex/skills/playwright/SKILL.md. Prerequisites: real Volume 1 bridge, fresh process, Forest encounter-complete setup explicitly labelled if fixtures are used. Preserve WIP. Read FamilyRescue.cs, World_1_Forest.tscn's MatthewRescue override, GameManager.RescueFamilyMember()/BefriendAnimal(), NPC.cs, FamilyTree.cs and HubAnimalSpawner.cs.

1. Use native combat or labelled defeatEnemy fixture through CombatHealth and director progression to unlock MatthewRescue; do not simply set rescued IDs. Assert Matthew is absent from RescuedFamilyIds initially. Isolate the player's route from CollectibleSanctuaryA/B so rescue currency is measurable independently. Read MatthewRescue.BonusGlimmers at runtime: it must be 20, not FamilyRescue.cs's default 10.
2. Enter the rescue area. Within 5 seconds require FamilyRescuedCount = previous+1, RescuedFamilyIds includes Matthew once, Glimmers = previous+20, one FamilyRescuedChanged event and actual HUD 'Family Rescued: 1 / 5' in this fresh run. Native RescueFamilyMember returns void, so assert state and signals rather than inventing a success bool.
3. Re-enter the consumed area, revisit Forest after a Hub round trip, and confirm no second Matthew reward or count increment. On reload FamilyRescue._Ready() should remove an already-rescued ConsumeOnRescue instance. Track fresh node identity and wait for deferred deletion rather than assuming a missing cached reference proves this.
4. Interact twice with Toby's real NPC through E, handling queued dialogue between interactions. Require a single Toby entry and one AnimalBefriended event; speech may happen more than once without adding rewards. Return to Hub through LevelPortal with a 15-second deadline, then allow 5 seconds for queued old spawns to disappear. Verify FamilyTree visible stage index clamp(count,0,stages-1), one SpawnedFamily and one Toby hub_spawned_animal within available markers. Current family spawning uses count/array order and defaults to Matthew first; do not claim arbitrary identity-correct spawning for other rescued IDs.

Evidence: output/playwright/V4.2/ with rescue source/runtime reward, before/after arrays/counters, duplicate-attempt event windows, Hub tree/spawn identities, native captures and trace. Fail on duplicated value, wrong reward, missing tree growth, duplicate spawns after settling, stale state or timeout. No save-system, identity-spawn refactor or delivery changes.
```

### V4.3 — Assert true return values, health recovery and exact-clue unlock

```text
Test /Users/kyleberry/Projects/ElainesQuest with /Users/kyleberry/.codex/skills/playwright/SKILL.md; preserve WIP. Prerequisite: real Volume 1 bridge returning native values for receiveDamage(), restoreHealth(), selectSpell(), tryCastSelectedSpell(), fixture('findTownClue',...) and fixture('completeFantasyRealm',...). Missing capabilities block this test. Read GameManager.cs, ElaineController.cs, TownClue.cs, TownAdventureDirector.cs and HUD.cs.

1. In an isolated safe fresh player fixture, wait for invulnerability==0. receiveDamage(1.5) should return true and reduce health from 5 to 3.5; immediate damage during DamageInvulnerabilityDuration returns false. restoreHealth(0.75) must return 0.75 and yield 4.25; restoreHealth(99) returns 0.75 and caps at 5; restoreHealth(0), a negative amount and healing at full health return 0. Use actual MaxHealth if the scene overrides it, recomputing expected deltas explicitly. Verify native HealthLabel rounds with ceil(), so 4.25 displays 'Health: 5 / 5'.
2. selectSpell(-1) and selectSpell(3) return false without change. selectSpell(0..2) returns true; selecting the already-selected index remains true but emits no new SpellSelected. Ground/idle/closed-dialogue/cooldown-ready casting returns true, immediate direct recast false. Keep event windows sequence-bound and do not confuse input buffering with direct method return values. Bound each condition at 5 seconds.
3. Run Town cases in separate fresh processes. First use native ClueHome, ClueStore and ClueSchool interactions through E: expect each GlimmerReward once, IDs TownHomeNote/TownStoreReceipt/TownSchoolMark, no unlock after only two, and TownPortalAwakened after all three. Require native TownClueProgressLabel 'Town Clues: 3 / 3'; TownClueIds has four entries because the awakening marker is excluded by GetTownClueCount().
4. In another isolated Town run use the debug findTownClue fixture for duplicate/blank-ID return contracts: first valid ID true, repeat/blank false. An UnrelatedClue must not unlock FantasyPortal, although current GetTownClueCount() counts unrelated IDs. Report that distinction rather than forcing the display to stay 0. completeFantasyRealm('MoonlitGarden') returns true once, false on repeat; Town's FantasyPortal hides and CompletionBeacon/ReturnToHub activate within 5 seconds. Direct manager fixtures award no TownClue.GlimmerReward.

Evidence: output/playwright/V4.3/ with return-value matrix, health math/rounding, exact clue IDs, unlock/visibility states, actual UI text, events and trace. Fail on incorrect bool/float/void conversion, cap violation, extra reward, unrelated-clue unlock, stale state or missing prerequisites. No implementation/push unless separately requested.
```

### V4.4 — Separate scene persistence, browser reload and application restart

```text
Test /Users/kyleberry/Projects/ElainesQuest using /Users/kyleberry/.codex/skills/playwright/SKILL.md; preserve WIP. Prerequisites: native app plus real Volume 1 bridge, process-scoped launch/stop control and snapshots. Read Scripts/Core/GameManager.cs, LevelPortal.cs, ElaineController.ApplyPendingSceneArrival()/ApplyStoredCheckpoint(), Checkpoint.cs and ElainesSceneTransition.cs. There is currently no save/load, user:// progress file, localStorage or sessionStorage game persistence implementation.

1. In one fresh game process, acquire at least one collectible, befriend Toby and rescue Matthew through native interactions or clearly labelled integration fixtures through their existing paths. Record Glimmers, RescuedFamilyIds, FamilyRescuedCount, BefriendedAnimals, TownClueIds, CompletedFantasyRealmIds and checkpoint fields. Move Forest→Hub→Forest using portal interactions or labelled loadScene setup; require the same manager/session and preserved progression sets/currency with 15 seconds per scene transition. Treat chapter objectives and local encounter flags as scene-derived rather than requiring every field unchanged.
2. Exercise QueueSceneArrival/ConsumeSceneArrival through actual portal entry. Require PendingArrivalMarker consumed, player positioned at the destination marker within 0.25 units before movement, and velocity reset. Check the native CheckpointScenePath against what Checkpoint.cs actually stores: CurrentScene.SceneFilePath can initially be Bootstrap. Arrival placement takes priority over stored checkpoint in ElaineController. Report mismatches without rewriting expected semantics.
3. Reload only the QA browser page with the same native process. Within 15 seconds require reconnected same native sessionId and unchanged currency/progression; browser storage clearing must not reset native progress. Read browser localStorage/sessionStorage keys before/after and report them only as QA-page storage, not the game save. Do not add a save implementation to make this check pass.
4. Stop the exact native child, launch a fresh process and reattach /qa/. Require a new sessionId, Glimmers=0, FamilyRescuedCount=0, empty progression sets/checkpoint and default ring flags before test interactions. Explicitly document this reset as current behaviour, not data-loss protection or successful persistent saving. Check defaults before chapter _Ready actions can seed marker/completion state. Bound fresh readiness at 15 seconds and preserve stop/start logs.

Evidence: output/playwright/V4.4/ with three distinct lifecycle result groups, process/session IDs, baseline/scene/reload/restart snapshots, arrival coordinates, storage inventory and trace. Fail on progression lost within one native session, stale-process connection or incorrect assertions about existing persistent saves. List restart persistence as a future feature (V6.3), not a tested pass.
```

## VOLUME 5: AGENTIC SELF-HEALING & LOG DEBUGGING

### V5.1 — Triage a failure across browser, bridge and Godot

```text
Diagnose the current failing Elaine's Quest automation in /Users/kyleberry/Projects/ElainesQuest. Use /Users/kyleberry/.codex/skills/playwright/SKILL.md; preserve existing WIP. Prerequisites: a concrete failing command/scenario and its output; discover the latest evidence under output/playwright/ and rerun if absent. Browser gameplay assertions require the real Volume 1 bridge; missing bridge is an infrastructure prerequisite, not a game regression.

1. Capture the exact command, cwd, binary/runtime versions, git diff, native process/session, exit code and first meaningful error. Collect native stdout/stderr from process startup; browser console/pageerror/response/requestfailed listeners must attach before navigation. Correlate request IDs, sessionId, sceneEpoch, event sequence and native frames. Never expose capability tokens in published evidence.
2. Run the wrapper's console and network commands and inspect the fresh DOM snapshot. Start tracing before the reproduction and stop it after failure; save/relocate trace and screenshots under output/playwright/V5.1/. Inspect the actual trace with an available compatible trace viewer and report if the viewer is unavailable. Don't infer a native GLB 404 from an empty browser network log or equate an HTML 200 response with a live game.
3. Classify the failure as prerequisite/boot-resource, bridge transport/freshness, locator, input delivery, gameplay state, assertion or rendering. Use snapshot() and page.waitForFunction with 5 seconds for ordinary state or 15 seconds for scene transition. Missing currentScene/world distinction, vanished player during transition, invulnerability, queued dialogue and pending spell release are specific suspects. Preserve the original assertion and evidence.
4. Fix only the evidenced cause within the requested failure scope: test/bridge when it observes wrongly, game code when native behaviour violates an established contract. Show a reproducible failure first and retain a regression guard. Do not raise all timeouts, swallow exceptions, force state, skip assertions or accept stale samples. Rerun the original command plus the focused native verifier (90-second sandbox or 30-second chapter deadline), then stop if checks pass.

Evidence: output/playwright/V5.1/ with causal timeline, first error, before/after diff, original failure and successful rerun or precise unresolved blocker. Failure criteria remain the original invariant; report inconclusive infrastructure separately from game pass. No unrelated fixes, commits/pushes or deployment without separate authorisation.
```

### V5.2 — Repair stale references and locator collisions honestly

```text
Fix a locator-related Elaine's Quest automation failure in /Users/kyleberry/Projects/ElainesQuest using /Users/kyleberry/.codex/skills/playwright/SKILL.md. Prerequisites: reproduce the failing locator against an actual proposed /qa/ bridge page; if that page does not exist, report Volume 1 required instead of inventing selectors. Preserve WIP. Read the QA page implementation alongside Scenes/UI/HUD.tscn, HUD.cs and DialogueUI.cs.

1. Use CLI snapshot before any e-number reference and again after navigation, dialogue changes, wheel opening or stale-ref errors. Never reuse an e12 from an earlier snapshot. Preserve the failing command and snapshot in output/playwright/V5.2/. Count locator matches and inspect their accessible names/attributes before changing the locator.
2. Use page.locator('[data-node-path="HUD/Control/GlimmersLabel"]') for the proposed inspector row and page.getByRole('button',{name:'Select Star Volley',exact:true}) for the QA proxy. Require count()===1 within a 5-second bounded check. Do not use .first()/.nth(0) to conceal duplicate nodes. If the inspector duplicates rows after a scene change, fix its lifecycle rather than widening the selector. Runtime native SpellSlot1 text contains a prefix/newline/role, so it is not the same accessible HTML button.
3. If several intentional panes exist, scope to the actual QA instance identified by its session ID in DOM and prove uniqueness. If an explicit new test attribute is needed, add it to the QA page, document it as a new bridge selector and keep native node-path mapping intact. Godot 'HUD/Control/GlimmersLabel' is not a CSS selector for the game.
4. Reproduce Hub→Forest→Hub, queue/advance two dialogue lines and open/close Tab wheel, taking new snapshots at each significant change. Require one inspector row per active native node, current epoch and actual native visible/text values within 5 seconds each. Hidden native controls may still have visible inspector rows; use snapshot native visibility for gameplay assertions. Rerun the exact original locator failure and a second scene-cycle check, with finally releaseInputs().

Evidence: output/playwright/V5.2/ with collision counts, exact accessible names, snapshots before/after, trace and minimal locator/page diff. Fail if ambiguity persists, a positional selector masks it, assertions use stale native state or a QA proxy is described as native input. Run only relevant checks; no push/deploy.
```

### V5.3 — Diagnose asynchronous casts, dialogue and scene races

```text
Fix a reproducible asynchronous Elaine's Quest failure in /Users/kyleberry/Projects/ElainesQuest; use /Users/kyleberry/.codex/skills/playwright/SKILL.md and preserve WIP. Prerequisites: a real Volume 1 bridge with complete sequence-bound events and a failing scenario. Read ElaineController._PhysicsProcess()/StartOneShot()/TryCastSelectedSpell()/CancelQueuedSpellCast(), GameManager.RequestHitStop(), DialogueUI._UnhandledInput(), ForestAdventureDirector.SetNodeActive() and ElainesSceneTransition.TransitionTo().

1. Record snapshots/events around the failure with wall time, physicsFrame, Engine.TimeScale, sceneEpoch, OnFloor, current one-shot state, PendingSpellIndex, cooldown and invulnerability. Use debug read-only observations if missing; do not expose general setters. Distinguish cast acceptance from delayed release, hit confirmation and deferred enemy deletion. Initial casts require ground, no dialogue, no active one-shot and ready cooldown.
2. Replace brittle page.waitForTimeout assumptions with page.waitForFunction async predicates over fresh snapshot(): 5 seconds for release/hit/control-state changes, 15 seconds for world path plus live replacement Elaine after transitions. Track event seq > baseline and matching epoch/spell index. Use an 8-second landing budget where applicable. Preserve explicit bounded absence-observation windows for negative tests and fail if event history truncated.
3. Reproduce airborne pending-cast cancellation and F buffering near CastBufferDuration=0.14 separately from direct tryCastSelectedSpell(). Check that E/Escape advances queued dialogue rather than always closing it. Check gates' CollisionShape3D.disabled/Area3D.monitoring after deferred updates, not just visibility. Do not patch legitimate pending-cast cancellation, hit-stop or dialogue queue semantics simply to match an incorrect test.
4. Fix the responsible test/bridge or minimal game race, retaining the failing scenario as a regression. Run the original case and five controlled repetitions with fresh baselines; retries cannot turn intermittent failure into success. Release all inputs in finally and ensure subscriptions detach after scene changes. If repeated input while transitioning queues arrival markers incorrectly, preserve the exact marker/state timeline before any change.

Evidence: output/playwright/V5.3/ with original/after event timelines, predicate changes, native traces, fixed diff and each repetition's outcome. Fail on stale event satisfaction, hidden retries, unbounded waits, state-forcing or unresolved intermittent failure. Rerun relevant native spell/chapter verifier; no delivery changes.
```

### V5.4 — Debug native resource/animation failures with existing verifiers

```text
Diagnose/fix the evidenced native boot/combat/chapter failure in /Users/kyleberry/Projects/ElainesQuest. Use /Users/kyleberry/.codex/skills/playwright/SKILL.md for any available QA browser checks; preserve WIP. Prerequisites: Godot .NET/dotnet and the failing native log. Read RuntimeGltfModel.cs, EnemyCombatant.cs, CombatSandboxVerifier.cs, the affected scene/director and resource references. Do not run Fix*.cs or rebuild animation libraries before reading their mutation behaviour and establishing necessity.

1. Build ElainesQuest.sln and retain its result separately from runtime. Launch the relevant existing verification scene with a subprocess deadline, capture stdout/stderr and preserve exit code. CombatSandbox.tscn -- --verify-spells or --verify-enemies has a 90-second cap. World_03_Town.tscn -- --verify-town, MoonlitGardenRealm.tscn -- --verify-moonlit, World_02_Caves.tscn -- --verify-caves and StarfallSanctum.tscn -- --verify-starfall each have 30 seconds. Each requires exit 0 and its final SPELL_VERIFY/ENEMY_VERIFY/TOWN_VERIFY/MOONLIT_VERIFY/CAVE_VERIFY/STARFALL_VERIFY PASS prefix. Use /Applications/Godot_mono.app/Contents/MacOS/Godot --path . with the full res://Scenes/Levels/... scene argument.
2. For missing character visuals inspect ModelPath, .import remap validity, cached scenes, GLTF sidecars and Visuals/__RuntimeModel plus placeholder visibility. For enemy animations use GetAnimationBindingSummary(), HasAnimationClip(), HasResolvedAnimationClip(), DebugTravelAnimationState() and CaptureAnimationPose()/MeasureAnimationPoseDelta(). State travel without resolved clips or moving bones is insufficient. Do not copy arbitrary import cache outputs into source as a silent repair.
3. Reproduce from Bootstrap→Hub and the affected real chapter as well as an isolated verifier. Chapter verifiers directly damage enemies; their PASS does not prove reachable traversal or combat input. If a real Volume 1 browser bridge exists, open /qa/, snapshot, trace and capture actual native frames with a 15-second readiness deadline; otherwise mark browser/render coverage unavailable.
4. Apply the smallest source/asset-reference fix justified by the log. Preserve original assets unless replacement is explicitly required. Rerun the exact failing native command, affected resource launch and focused regression. A missing final marker, resource error, hung process or nonzero exit remains a failure even if other checks pass.

Evidence: output/playwright/V5.4/ with original error, dependency path, animation summaries/pose deltas, diff, verifier logs/exits and native PNGs where renderable. Report build, integration, rendered playability and device/audio checks separately. Do not erase failures by deleting assets/caches, skip the native scene path or push/deploy without separate authorisation.
```

## VOLUME 6: REFACTORING, OVERHAULS, & NEW FEATURE PROMPTS

### V6.1 — Overhaul movement with simultaneous native and Playwright guards

```text
Implement a movement-system refactor in /Users/kyleberry/Projects/ElainesQuest while preserving existing behaviour unless this request explicitly specifies a change. Use /Users/kyleberry/.codex/skills/playwright/SKILL.md; preserve unrelated WIP. Read ElaineController.cs, PlatformerCamera.cs, HUD.cs, Checkpoint.cs, HazardVolume.cs and Forest traversal geometry. This is native Godot C#: retain CharacterBody3D/MoveAndSlide and existing input/signal contracts. Prerequisite for browser regression: real Volume 1 bridge; implement the minimum missing bridge first if needed and clearly include it in scope/evidence, never use mocked position values.

1. Before editing, record current Speed=7, Acceleration/GroundDeceleration=18, Gravity=9.8, JumpVelocity=4.5, HoverDuration=2, HoverGravityModifier=0.2, analog RunInputThreshold, spell-wheel movement gating, grounded one-shots, checkpoint/arrival priority and native state/animation behaviour. Treat scene overrides as authoritative. Structure the controller into clear motion/interaction/combat boundaries without moving health into a new incompatible CombatHealth child.
2. In the same implementation, add a native regression verifier and a repeatable Playwright CLI-driven regression script using snapshot/input bridge commands. Capture a failing test or baseline contract before changing code. Test single/diagonal acceleration, release deceleration, jump→hover→land, wheel gating, Talk/Cast/Wave locking, queued-cast cancellation, hazard/defeat respawn and camera follow. Maintain keyboard and analog/controller mappings; synthetic analog checks are not hardware proof.
3. Automate real #GameInput A/D/W/S/Space/Tab/F/E inputs using fresh CLI snapshots; use page.waitForFunction over native position/velocity/health/epoch. Bound movement progress at 2 seconds, landing at 8 seconds, normal state at 5 seconds and scene handoffs at 15 seconds. Use labelled fixtures only for initial isolation; include an input-driven Forest ridge/checkpoint route as a separate acceptance case. Release keys in finally and record motion tolerances measured from the baseline.
4. Run dotnet build, the new native verifier, --verify-spells and --verify-enemies (90 seconds each) plus the targeted browser regression while editing, not after considering the feature complete. Inspect actual native captures for terrain collision, camera and animation regressions. A blocked bridge/runtime is incomplete validation, not a release pass.

Evidence: output/playwright/V6.1/ with baseline/failing guard, implementation diff, new regression commands, measured trajectories, native logs, browser trace and PNGs. Accept only preserved contracts and passing matching guards. Leave commits/push/deployment for separate authorisation; when authorised later, never push this refactor without these guards passing and material verification gaps resolved or explicitly accepted.
```

### V6.2 — Extend the spell system without breaking existing spells

```text
Implement a fourth spell, 'Moon Ward', in /Users/kyleberry/Projects/ElainesQuest with simultaneous regression coverage. Use /Users/kyleberry/.codex/skills/playwright/SKILL.md and preserve WIP. Read ElaineController.SpellConfig/GetSpellConfig()/GetSpellCount(), SpellProjectile.cs, HUD.cs, HUD.tscn, ElaineAnimStateMachine.tres and CombatSandboxVerifier.cs. Existing indices 0 Sun Ember, 1 Star Volley, 2 Sky Lance and their emitted signal contracts must remain stable. This prompt specifies a future feature; Moon Ward does not currently exist. Browser prerequisite: a real Volume 1 bridge, otherwise implement/document its minimal required support in this feature rather than claiming a mocked pass.

1. Add index 3 Moon Ward as a non-projectile heal using existing grounded one-shot/cooldown/health architecture: RestoreHealth(1), no overheal, 5-second cooldown, existing Cast animation, one heal application at the accepted cast's scheduled release. It remains selectable at full health, consumes cooldown when accepted, returns zero restored at full health and cancels without healing if the pending cast is cancelled by becoming airborne or respawning. Do not add currency cost, inventory or multiplayer authority. Keep existing cast acceptance, buffer and dialogue restrictions.
2. Extend wheel UI/selection and native role/HUD text for index 3, exposing actual native new slot data through the inspector. Define one true resolved-effect observation carrying accepted spell index and restored amount; keep existing projectile release/hit signals meaningful (no fake projectile for healing). Document any new public signal/bridge field. Reuse existing assets instead of promising an unprovided visual asset.
3. Write native tests and a Playwright CLI regression alongside the feature: damaged-player heal=1, full-health heal=0, cooldown rejection, cancellation before release, no duplicate healing, four-slot native selection and HUD update. Use real ReceiveDamage/RestoreHealth returns and event sequence, not direct health assignment. Select through Tab+direction for native input coverage; any new 'Select Moon Ward' QA button is explicitly a new proxy selector.
4. Preserve old 1/3/1 projectile counts and 1/3/2 target hit contracts by running --verify-spells and --verify-enemies with 90-second process deadlines. Add a matching Moon Ward native verifier case and browser test with 5-second state/command bounds and a 7-second cooldown-ready bound. Test wheel layout at 1280x720 and 390x844 with actual native resize/rectangles; existing responsive failures must be reported, not hidden by CSS scaling.

Evidence: output/playwright/V6.2/ with failing/new guards, true restored values, event timelines, native/DOM HUD observations, regressions, trace and native capture. Accept only one effect per accepted uncancelled cast, correct cooldown, intact older spells and matched UI. No push/deploy without separate authorisation and passing regression guardrails.
```

### V6.3 — Add native save persistence with restart and recovery tests

```text
Implement versioned native progression saving in /Users/kyleberry/Projects/ElainesQuest with matching tests in the same change. Use /Users/kyleberry/.codex/skills/playwright/SKILL.md and preserve WIP. Read GameManager.cs, SceneBootstrap.cs, HUD.cs, FamilyTree.cs, HubAnimalSpawner.cs, FamilyRescue.cs, TownClue.cs, LevelPortal.cs and chapter _Ready logic. Existing state is only in memory; browser localStorage is not a native game save. Prerequisite for Playwright regression: real Volume 1 bridge and process restart control; add necessary explicit debug-save isolation support if missing.

1. Store a version-1 JSON save at user://elainesquest-save.json containing Glimmers, RescuedFamilyIds, BefriendedAnimals, TownClueIds, CompletedFantasyRealmIds and checkpoint ID/scene/position. Derive FamilyRescuedCount from unique family IDs. Keep HasMagicRing/CanTranslateAnimals defaults, active spell/cooldown/health, CurrentObjective and PendingArrivalMarker out of the save; objectives/local encounters are reconstructed by scenes. Always start in Hub, preserving checkpoint data for later matching-scene entry, not an unsafe mid-combat resume.
2. Validate nonnegative bounded integer currency, finite checkpoint coordinates, known progression IDs from current content and known scene paths. Treat missing save as a fresh game. Write atomically through a temporary file and keep one last-good backup; on corrupt/unsupported newer version preserve the original and load a valid supported backup or fresh state with an explicit diagnostic. Do not silently overwrite an incompatible file on boot. Implement an explicit New Game reset path with a matching regression; do not delete unrelated user:// files.
3. Load before world/HUD _Ready and subscribe saving to progression/checkpoint changes without duplicate rewards or signal loops. Coalesce writes over 250ms and flush on clean quit. Give QA runs an opt-in isolated user-data directory unique to the test; no regression may clear or overwrite a player's real save. Expose save completion/error as native telemetry so tests await actual durable completion, not a browser-storage write.
4. Simultaneously add native serialization/validation/recovery tests and a Playwright CLI orchestration regression: earn a collectible, befriend Toby, rescue Matthew (+20), record a checkpoint, await durable save within 5 seconds, restart native process with the same isolated save directory and require new sessionId with restored currency/sets/HUD and no repeated rescue reward. Browser reload alone is insufficient. Test fresh directory, truncated JSON, backup recovery, unsupported version, duplicate IDs, invalid coordinates, write failure and New Game without losing unrelated files. Use 15-second boot/transition and bounded process teardown deadlines.

Evidence: output/playwright/V6.3/ with save schema, isolated paths, test fixtures, before/after process snapshots, recovery diagnostics, native tests, browser trace and UI captures. Accept only durable restart recovery and unchanged reward semantics. Build and relevant chapter/reward regressions must pass; no push/deploy without separate authorisation and regression guardrails.
```

### V6.4 — Add two-player native co-op with authority and deduplicated rewards

```text
Implement a local two-player co-op vertical slice in /Users/kyleberry/Projects/ElainesQuest with simultaneous native and Playwright regression guards. Use /Users/kyleberry/.codex/skills/playwright/SKILL.md; preserve WIP. Read GameManager.Instance, ElaineController.cs, HUD.ResolvePlayer(), PlatformerCamera.cs, CombatHurtbox.cs, SpellProjectile.cs, FamilyRescue.cs, TownClue.cs, NPC.cs and LevelPortal.cs. Multiplayer does not currently exist. Prerequisites: two launchable native Godot .NET processes and a real Volume 1 bridge per instance; implement/document missing minimal bridge support as part of this slice rather than mocking it. Default scope: two native Godot processes on loopback, host-authoritative ENet, no accounts/matchmaking/cloud or public deployment. Keep single-player startup intact.

1. Add explicit host/join launch options and replicated peer-owned Elaine instances. Separate local player lookup from current 'first node in group player' assumptions in HUD/camera/interaction. Host validates movement ownership and interaction range; host owns enemy damage, collectibles, shared Glimmers, rescue/clue/realm sets, gates and scene progression. Clients cannot invoke AddGlimmers/RescueFamilyMember as trusted authority. Define a shared session progression snapshot on join; no persistent save requirement in this slice.
2. Use authoritative instance/event IDs to make collectible/rescue/clue claims idempotent across duplicate/reordered requests. Simultaneous Matthew rescue awards the shared party +20 once and one Matthew ID, not +20 per peer. Preserve 0/1/2 spell selection, actual host projectile hits and per-projectile hurtbox deduplication. Host coordinates party scene transitions and arrival markers; clear obsolete requests on scene epoch change. On disconnect release that peer's held inputs and remove its avatar; remaining host play continues. Host departure closes the session cleanly; no host migration.
3. In the same change add native authority tests and a repeatable Playwright CLI-driven two-instance regression. Run separate QA servers with proposed --qa-port=4173 and 4174, separate capability tokens/sessionIds and named browser sessions. Each page must connect to its own native instance. Snapshot before interactions; browser contexts alone controlling one native game are not multiplayer proof. Isolate user data/process teardown.
4. Drive host and client inputs independently, verify only the owning local avatar moves, and require both peers converge on host-approved health/positions/progression within 5 seconds. Concurrently claim one collectible, Matthew and a Town clue; replay the same request ID and send an unauthorised currency claim. Require one legitimate award, rejected replay/cheat and matching actual HUD counters. Exercise a spell hit, guest join state, one coordinated Forest→Hub transition within 15 seconds, guest disconnect/rejoin and host shutdown. Use native authority checks plus actual captures of both processes.

Evidence: output/playwright/V6.4/ with failing/new authority guards, peer IDs and ownership, host/client request/event timelines, shared reward totals, both native logs, browser traces and PNGs. Accept only two real processes, independently controlled avatars, deduplicated host rewards and deterministic handoff/cleanup. Run single-player spell/chapter regressions too. No public networking, push or deploy without separate authorisation; missing runtime proof remains an incomplete feature.
```

---

## Delivery and verification checklist for future prompt execution

- Identify whether the prompt is an audit, targeted repair or feature implementation before editing. Preserve the checkout's unrelated tracked and untracked work.
- Record exact commands, cwd, process identity, native session/scene epoch, assertion deadlines and exit statuses. Keep secrets/capability tokens out of shared logs.
- Save native state/event/receipt JSON, native frame captures, browser evidence and diagnostics under `output/playwright/<prompt-id>/`. A browser inspector screenshot is not a native frame.
- Mark fixture-assisted integration, input-driven playability, rendered native UI, audio resources, audible audio, device/controller testing and multiplayer authority separately. Never upgrade one category's result into another.
- Missing bridge, unavailable renderer, hung process, inaccessible native window or missing test dependency is a precise incomplete result. Do not simulate success or weaken assertions.
- For implementation prompts, write/run the matching native and Playwright regression during implementation. Separately authorised Git delivery must include relevant passing guards and an honest record of material gaps.

## Authoring validation record

This edition was checked against the source working tree for referenced paths, state/method names, UI hierarchy, chapter IDs, scene overrides and verification arguments. Its six volumes contain four prompts each. The Volume 1 bridge and all QA HTML selectors remain proposed. The book adds documentation only; it does not install the bridge, change gameplay, create saves, run native/browser gameplay, commit, push or deploy.
