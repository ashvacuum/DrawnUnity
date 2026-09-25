# DrawnUnity

Finals Exam for Applied Mathematics — a Unity 6 / URP project. Boxes and quads are drawn procedurally with `Graphics.DrawMeshInstanced` and `Matrix4x4` transforms rather than GameObjects/Colliders, so the math (vertices, winding, TRS matrices, AABB collision) stays visible in code instead of being hidden behind editor components.

---

## Assignment requirements

1. Add jump functionality
   * jump must be fast when going up and slow when going down
   * player speed reduced in half when in the air.
2. Add Powerups
   * fireball that goes in a straight line
   * gives the player an extra life
   * gives the player invincibility and kills anything it collides with.
3. Add Enemies that will damage the player
   * Enemies can move left/right
4. Obstacles for the player to move around in
5. Some obstacles can instakill the player
6. The game has an end goal
7. For meshes outside of camera range, change the scale to zero (dot product)
8. Timer UI
9. HP UI
10. Add a video of the game in your readme.md file

Notes: no art required / no need to draw the enemies in the same instanced mesh so they can have separate colors.

Up to teams of two people.

---

## Project structure

```
Assets/
├── Scripts/           Gameplay code (see Systems below)
├── Scenes/            SampleScene.unity
├── Art/                Sprites/materials
└── Settings/           URP render pipeline assets
```

## Systems

| Script | What it does | Docs |
|---|---|---|
| `QuadSetupGuide.cs` | Procedural quad mesh + GPU instancing. Minimal example of the drawing pattern used everywhere else. | [`QuadSetup_README.md`](Assets/Scripts/QuadSetup_README.md) |
| `EnhancedMeshGenerator.cs` | Procedural cube mesh, instanced rendering, and the player movement loop. Start here to add jump/powerups/enemies. | [`GameplaySystems_README.md`](Assets/Scripts/GameplaySystems_README.md) |
| `CollisionManager.cs` | Singleton registry of AABB (axis-aligned bounding box) colliders; answers "would I overlap anything at this position?" | [`GameplaySystems_README.md`](Assets/Scripts/GameplaySystems_README.md) |
| `PlayerCameraFollow.cs` | Smoothed camera follow with optional axis locking and bounds clamping. | [`GameplaySystems_README.md`](Assets/Scripts/GameplaySystems_README.md) |

Read `QuadSetup_README.md` first — it's the smallest example of the instancing pattern. `GameplaySystems_README.md` builds on the same pattern and adds movement/collision, and includes a table mapping each assignment requirement above to where in the code it likely belongs.

## Getting started

1. Open the project in Unity 6 (URP) via Unity Hub.
2. Open `Assets/Scenes/SampleScene.unity`.
3. Follow the "Quick start" section in whichever doc above matches what you're working on — the gameplay scripts aren't pre-wired into the scene; you add the component and a material yourself (this keeps the setup steps visible instead of hidden in scene data).
4. Press Play.
