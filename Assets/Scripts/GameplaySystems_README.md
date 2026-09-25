# Gameplay Systems (Player, Collision, Camera)

A falling/moving box (the player) on top of a ground box, surrounded by random boxes, all drawn with `Graphics.DrawMeshInstanced`. Same drawing pattern as `QuadSetupGuide`, plus movement and collision.

Each script has one job:

| Script | Job |
|---|---|
| `CubeMeshBuilder` | Builds the cube mesh. |
| `InstancedBoxRenderer` | Draws a list of matrices (batches past the 1023 GPU limit). |
| `BoxWorld` | Creates and stores every box (player, ground, random boxes) and their colliders. |
| `PlayerController` | Reads input, applies gravity, moves the player against `CollisionManager`. |
| `PlayerCameraFollow` | Smoothly follows the player. |
| `CollisionManager` | Answers "would this box overlap anything at this position?" |
| `EnhancedMeshGenerator` | Wires the above together in `Start()`/`Update()`. Attach this one to a GameObject. |

## Quick start

1. Create an empty GameObject, add **Enhanced Mesh Generator**.
2. Make a Material with **Enable GPU Instancing** checked, assign it to **Material**.
3. Leave **Camera Follow** empty — it's set up automatically.
4. Press Play. Move with **A** / **D**.

## How it flows each frame

```
EnhancedMeshGenerator.Update()
  → PlayerController.Tick()      reads input, checks CollisionManager, moves the player
      → BoxWorld.MovePlayer()    writes the new matrix + collider
      → PlayerCameraFollow       gets the new player position
  → InstancedBoxRenderer.Render() draws every box in BoxWorld
```

## Why it's split up

Before, one script built the mesh, spawned boxes, read input, ran gravity, set up the camera, and rendered — all in one class. That made it hard to find or change one thing without touching the rest. Now each script does one job (single responsibility), so e.g. adding jump only touches `PlayerController`, and adding a new kind of box only touches `BoxWorld`.

## Extending for the assignment

| Requirement | Where |
|---|---|
| Jump | `PlayerController` — apply upward velocity on input while grounded. |
| Powerups / enemies / obstacles | `BoxWorld` — add new box types with their own ids. |
| Instakill / end goal | `CollisionManager.CheckCollision` already tells you which ids you hit. |
| Camera-range culling | Filter `BoxWorld.Matrices` before `InstancedBoxRenderer.Render()` runs. |
| Timer / HP UI | A separate UI script — keep it out of the systems above. |

## Gotchas

- Material needs **Enable GPU Instancing** on, or nothing draws.
- Max 1023 matrices per draw call — `InstancedBoxRenderer` batches automatically.
- Collision checks are O(n) per query — fine for a classroom scene, not for thousands of boxes.
- `CollisionManager` persists across scene loads (it's a singleton) — stop Play mode to reset it.
