# GameplaySystems (Player, Collision, Camera)

Procedural cube mesh + GPU-instanced drawing, a simple AABB platformer controller, and a camera that follows the player. Same `Matrix4x4`/`Graphics.DrawMeshInstanced` drawing pattern as `QuadSetupGuide`, extended with movement and collision.

| | |
|---|---|
| **Scripts** | `EnhancedMeshGenerator.cs`, `CollisionManager.cs`, `PlayerCameraFollow.cs` |
| **Related** | `QuadSetupGuide.cs` (same instancing pattern, no movement/collision) |
| **Engine** | Unity 6 / URP (this project) |

---

## Contents

1. [Quick start](#quick-start)
2. [How the three scripts fit together](#how-the-three-scripts-fit-together)
3. [Inspector](#inspector)
4. [Collision system](#collision-system)
5. [What runs each frame](#what-runs-each-frame)
6. [Extending this for the assignment](#extending-this-for-the-assignment)
7. [Limits and gotchas](#limits-and-gotchas)
8. [Troubleshooting](#troubleshooting)
9. [Unity references](#unity-references)

---

## Quick start

1. Create an empty GameObject named `World` (transform at origin).
2. Add component **Enhanced Mesh Generator**.
3. Create a Material (`URP/Lit` or `Unlit/Color`), check **Enable GPU Instancing**, assign it to **Material**.
4. Leave **Camera** empty — `Start()` finds `Camera.main` (or creates one) and attaches `PlayerCameraFollow` automatically.
5. Press Play. A falling box (the player) lands on a large ground box, surrounded by random scattered boxes. Move with **A** / **D**.

`CollisionManager` needs no setup — it's a singleton that creates itself the first time anything calls `CollisionManager.Instance`.

---

## How the three scripts fit together

```
EnhancedMeshGenerator          CollisionManager (singleton)      PlayerCameraFollow
  owns: mesh + matrices          owns: one AABBBounds per box       owns: camera transform
  ------------------------       ------------------------------     --------------------------
  CreateBox(...)          ---->  RegisterCollider(...) -> id
  UpdatePlayer()
    CheckCollisionAt(id,   ---->  CheckCollision(id, pos)
      proposedPos)                  (tests pos against every
                                     other registered box)
    on success: move player,
    matrices[playerMatrixIndex]
      = new Matrix4x4.TRS(...)
    cameraFollow.SetPlayerPosition(pos) -------------------------->  LateUpdate() lerps
                                                                      transform.position
                                                                      toward pos + offset
  RenderBoxes()
    Graphics.DrawMeshInstanced(mesh, matrices)
```

Nothing here uses a `Transform`, `Rigidbody`, or `Collider` for the boxes — position/rotation/scale live in a `List<Matrix4x4>`, and "does this overlap anything" is answered by `CollisionManager`'s plain AABB math. Only the *camera* is a real GameObject, because `Camera` itself needs one.

---

## Inspector

**Enhanced Mesh Generator**

| Group | Field | Default | Description |
|-------|-------|---------|-------------|
| Rendering | Material | — | Required. Must support GPU instancing. |
| Rendering | Instance Count | `100` | Total boxes drawn, **including** the player and the ground. |
| Rendering | Width / Height / Depth | `1 / 1 / 1` | Local-space size of the cube mesh. |
| Player | Movement Speed | `5` | Horizontal speed, units/second. |
| Player | Gravity | `9.8` | Downward acceleration while airborne. |
| Camera | Camera Follow | — | Auto-assigned in `Start()` if left empty. |
| World Layout | Constant Z Position | `0` | Shared Z for every box — the whole scene is one plane. |
| World Layout | Min/Max X, Min/Max Y | `-50..50` | Random placement range for generated boxes. |
| Ground | Ground Y / Width / Depth | `-20 / 200 / 200` | Size and position of the ground box. |

**Player Camera Follow** — see the header groups in the script (`Follow Target`, `Follow Axes`, `Bounds`); the tooltips there cover each field.

---

## Collision system

`CollisionManager` is a singleton (`CollisionManager.Instance`) holding a `Dictionary<int, AABBBounds>` — one entry per box, keyed by the `int` id returned from `RegisterCollider`.

An **AABB** (axis-aligned bounding box) is just a center and a size; `Min`/`Max` are derived once per update. Two AABBs overlap unless one is entirely to one side of the other on *any* axis — that's the whole of `AABBBounds.Intersects`.

The movement pattern in `UpdatePlayer` is **query before you move**:

```csharp
if (!CheckCollisionAt(playerID, proposedPosition))
{
    pos = proposedPosition; // only commit the move if it's clear
}
```

Horizontal and vertical movement are checked as two separate steps, so sliding along a wall doesn't also stop you from falling (and vice versa).

This is a teaching-scale system: `CheckCollision` compares against every other registered box (O(n)). Fine for the box counts here; not meant to scale to thousands of colliders (that's what spatial partitioning / physics engines solve).

---

## What runs each frame

```
Update()  (EnhancedMeshGenerator)
  ├─ UpdatePlayer()
  │    ├─ apply gravity to playerVelocity
  │    ├─ resolve horizontal move against CollisionManager
  │    ├─ resolve vertical move against CollisionManager (sets isGrounded)
  │    ├─ write the new Matrix4x4 back into matrices[playerMatrixIndex]
  │    └─ cameraFollow.SetPlayerPosition(pos)
  └─ RenderBoxes()
       └─ Graphics.DrawMeshInstanced(...) in batches of ≤1023

LateUpdate()  (PlayerCameraFollow, runs after Update)
  └─ lerp transform.position toward the reported player position + offset
```

---

## Extending this for the assignment

The root `README.md` lists the finals-exam requirements. Rough pointers into this code for each (no gameplay code included here — this is a starting point, not the assignment):

| Requirement | Where to look |
|---|---|
| Jump (fast up, slow down, half speed in air) | `UpdatePlayer()` in `EnhancedMeshGenerator.cs` — apply an upward `playerVelocity.y` on input while `isGrounded`, and scale `movementSpeed` by whether `isGrounded` is true. |
| Powerups / enemies / obstacles | New boxes registered through `CreateBox` (or your own variant), with their own ids tracked separately from the scenery boxes so you can tell what the player just hit. |
| Instakill obstacles / end goal | `CheckCollision` already returns *which* ids you're overlapping (`collidingIds` out-param) — use that to distinguish "solid ground" from "hazard" from "goal". |
| Camera-range culling via dot product | `PlayerCameraFollow` (or a new script) knows the camera position/forward; per-instance culling means filtering the `matrices` list before `RenderBoxes()` draws it. |
| Timer / HP UI | Separate UI script reading state (health, elapsed time) that `EnhancedMeshGenerator` exposes — keep it out of the rendering/movement code above. |

---

## Limits and gotchas

| Topic | Detail |
|-------|--------|
| Batch size | Max **1023** matrices per `DrawMeshInstanced` call; `RenderBoxes()` splits into batches automatically. |
| GPU Instancing | Material must have it enabled or nothing draws. |
| Collision cost | O(n) per query — fine for classroom scenes, not for large ones. |
| No rotation in collision | `AABBBounds` is axis-aligned; a rotated box's *visual* mesh can extend past its (unrotated) collider. |
| `CollisionManager` lifetime | Persists across scene loads (`DontDestroyOnLoad`) — stop/reset Play mode to clear it, don't rely on reloading the scene alone. |

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| Player falls through the ground | Ground box created after the player checks collision, or collider size mismatch | Confirm `CreateGround()` ran in `Start()` before `Update()`, and that `groundWidth`/`groundDepth` match the visible ground box. |
| Player doesn't move | No `Material` assigned, or Instancing off | Assign material, enable **Enable GPU Instancing**. |
| Camera doesn't follow | `cameraFollow` never received a position | Confirm `SetPlayerPosition` is called every frame in `UpdatePlayer()`; check `followX`/`followY`/`followZ`. |
| Camera frozen at start | (Historical bug, fixed) — used to check `playerPosition == Vector3.zero` | `PlayerCameraFollow` now tracks a `hasTarget` flag instead. |
| Only first 1023 boxes appear per batch | API limit | Expected — `RenderBoxes()` issues one `DrawMeshInstanced` call per 1023 instances. |

---

## Unity references

- [Mesh](https://docs.unity3d.com/ScriptReference/Mesh.html)
- [Matrix4x4](https://docs.unity3d.com/ScriptReference/Matrix4x4.html)
- [Graphics.DrawMeshInstanced](https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstanced.html)
- [Bounding Volume (AABB) — general concept](https://docs.unity3d.com/ScriptReference/Bounds.html)
