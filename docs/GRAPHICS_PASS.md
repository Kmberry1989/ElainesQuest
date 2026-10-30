# Hub and Forest graphics pass

The previous desktop preview rendered Elaine at roughly 7.2 m, Sasha at 5.7 m,
and Matthew partly underground in a T-pose. The Hub spawn and several props were
far from the actual activities. Two environment controllers and multiple suns
competed over a noisy terrain surface.

The Hub is now a compact garden clearing with paths to its four portals, grounded
props, a tree line, a single daylight environment, and distant hills. The Forest
uses the supplied grassy platform artwork fitted to each collision footprint,
rock foundations, and scenery outside the jump route. Foreground trees that hid
Elaine have moved behind the course.

Elaine's idle pose is fitted to 1.7 m and her collision capsule has its feet at
her root. The two introductory animals and family members have explicit display
heights. Family members use the supplied idle animation instead of the bind pose.
The display fitter measures the animated mesh in graphical runs; the invisible
headless renderer falls back to bind bounds because it cannot bake a posed skin.
Original GLBs are unchanged. The additional grassy tile uses the existing
lossless runtime-derivative process and its source hash is in the manifest.

The camera starts in place, uses interpolated physics positions, and smooths its
look-ahead. Four-sample antialiasing reduces edge shimmer. Jump clearance now
supports the raised Forest steps. HUD text has an outline, dialogue has a solid
panel and a close hint, and objective/prompt text hides while dialogue is open.

## Reproduce native screenshots

Build the C# project, then run a graphical Godot 4.7 .NET executable:

```sh
dotnet build ElainesQuest.csproj
godot --path . --script tools/review_environment.gd -- --review-output=/absolute/path/hub.png
godot --path . --script tools/review_environment.gd -- --review-overview --review-output=/absolute/path/hub-overview.png
godot --path . --script tools/review_environment.gd -- --review-forest --review-output=/absolute/path/forest.png
godot --path . --script tools/review_environment.gd -- --review-forest --review-overview --review-output=/absolute/path/forest-overview.png
godot --path . --script tools/review_environment.gd -- --review-dialogue --review-output=/absolute/path/dialogue.png
```

Add `--review-profile` for a ten-second warm-up, or `--review-stay` to keep the
window open for up to forty seconds. The review logs measured posed bounds and
frame rate. Overview views are review cameras; the other views use the gameplay
camera. Screenshots come directly from the native viewport, without image edits.

## Validation scope

Native Metal testing on Apple M4 confirmed Hub/Forest rendering, character scale,
ground alignment, Sasha dialogue, keyboard movement, the Forest portal, and a
jump from the meadow to the first step. The Hub and Forest each reached 60 FPS
in stationary native samples after warm-up; this is not a full performance audit.
The export contract also checks jump/landing stability and a single daylight rig.
A complete quest playthrough, audio, gamepad, Windows graphics, and the appearance
of later realms are outside this pass.
