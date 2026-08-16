# Byteberry look mechanics

Byteberry is a compact strawberry-fox coding companion with a physical head, large physical eyeballs, leafy crown, pointed ears, planted feet, a fluffy tail, belt, gloves, and a small strawberry pouch. The lower body and feet are the stable registration anchor. Looking is driven by the eyes first, then a restrained head and neck turn; the upper torso follows slightly while the hips, belt, pouch, and feet stay grounded. The physical eye globes rotate together in their sockets so sclera, iris, pupil, eyelids, rim, and highlights remain one construction. Do not slide isolated pupils across fixed eye whites or add replacement eyes. The leafy crown and ears follow the head with a small lag; the tail remains attached at the hip and lags subtly without detaching or changing sides.

## Cardinal pose families

- `000` up: feet, hips, belt, pouch, and tail base stay planted; chin and muzzle lift toward the top of the image, both physical eyes rotate upward with more lower eye area visible, eyelids open slightly, and the crown/ears tip back with the head. This is clearly looking up, never neutral/front.
- `090` screen-right: nose tip and both eye globes turn toward the viewer's right edge; the right cheek and right-facing muzzle lead, the far cheek and far ear become more occluded, and the head/upper torso turn subtly right while the hips, belt, pouch, feet, and tail root remain anchored. The tail may trail slightly behind the turn.
- `180` down: feet and lower body remain planted; chin and muzzle lower toward the bottom of the image, both whole eye globes rotate downward with upper eyelids lowering slightly, the crown leans forward, and the upper torso compresses just enough to show attention downward. The belt, pouch, and tail root remain stable.
- `270` screen-left: nose tip and both eye globes turn toward the viewer's left edge; the left cheek and left-facing muzzle lead, the far cheek and far ear become more occluded, and the head/upper torso turn subtly left while the hips, belt, pouch, feet, and tail root remain anchored. This must visibly oppose `090` in screen coordinates.

## Intermediate motion budget

Interpolate the head turn, whole-eye rotation, eyelid change, muzzle aim, ear/crown follow-through, and small tail lag in even 22.5-degree steps. No adjacent pair should make a larger bend, scale change, occlusion shift, or prop jump than its neighbors. Preserve volume, baseline, berry-seed markings, belt, gloves, and strawberry pouch. The ordered loop is `000` up -> `090` screen-right -> `180` down -> `270` screen-left -> `000` up. Diagonals should combine the adjacent cardinal family cues rather than mirror or independently restyle cells.

## Generation constraints

Use viewer/image coordinates. Keep the full pet centered in each pose group, with the lower-body anchor and practical scale matching the approved standard contact sheet. Do not rotate, skew, or affine-tilt the entire sprite to fake gaze. No labels, arrows, clocks, shadows, glows, scenery, detached effects, or new props.
