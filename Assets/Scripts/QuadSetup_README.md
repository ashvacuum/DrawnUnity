# QuadSetupGuide

Procedural quad mesh + GPU-instanced drawing with `Matrix4x4` transforms. Teaching sample for mesh vertices, winding, normals, UVs, and `Graphics.DrawMeshInstanced`.

| | |
|---|---|
| **Script** | `Assets/Scripts/QuadSetupGuide.cs` |
| **Related** | `EnhancedMeshGenerator.cs` (cube + collision; same drawing pattern) |
| **Engine** | Unity 6 / URP (this project) |

---

## Contents

1. [Quick start](#quick-start)
2. [Inspector](#inspector)
3. [Runtime API](#runtime-api)
4. [What runs at Play](#what-runs-at-play)
5. [Mesh layout](#mesh-layout)
6. [Matrices and drawing](#matrices-and-drawing)
7. [Limits and gotchas](#limits-and-gotchas)
8. [Troubleshooting](#troubleshooting)
9. [Experiments](#experiments)
10. [Unity references](#unity-references)

---

## Quick start

1. Create an empty GameObject named `QuadGenerator` (transform at origin).
2. Add component **Quad Setup Guide**.
3. Create a Material (`URP/Lit` or `Unlit/Color`), set a color, check **Enable GPU Instancing**.
4. Assign that material to the component’s **Material** field.
5. Set the Main Camera to about `(0, 2, -5)`, rotation `(10, 0, 0)`.
6. Press Play — you should see three quads (center, rotated right, scaled left).

The script turns on `material.enableInstancing` at runtime if it is off, and logs a warning. Prefer enabling it on the asset.

---

## Inspector

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| Material | `Material` | — | Required. Must support GPU instancing. |
| Width | `float` | `1` | Quad width in local space (X). |
| Height | `float` | `1` | Quad height in local space (Y). |

---

## Runtime API

```csharp
// Append one instance (position / rotation / scale → Matrix4x4.TRS)
void AddQuad(Vector3 position, Quaternion rotation, Vector3 scale);

// Split a matrix back into TRS (useful for animation experiments)
void DecomposeMatrix(Matrix4x4 matrix, out Vector3 position,
                     out Quaternion rotation, out Vector3 scale);
```

Instance list is private. Mutate existing instances by keeping your own index and rebuilding with `AddQuad`, or edit `CreateQuadInstances()` in the script for demos.

---

## What runs at Play

```
Start
  ├─ validate Material
  ├─ enable GPU Instancing if needed
  ├─ CreateQuadMesh()      → one Mesh (4 verts, 2 tris, UVs, +Z normals)
  └─ CreateQuadInstances() → three Matrix4x4 entries

Update (every frame)
  └─ RenderQuads()
       ├─ copy List → Matrix4x4[] (reuse buffer)
       └─ Graphics.DrawMeshInstanced(mesh, 0, material, matrices, count)
```

No MeshFilter / MeshRenderer on the GameObject — drawing is procedural via `Graphics`.

---

## Mesh layout

Quad sits in the **XY** plane, centered on the origin, **front face toward +Z** (toward a default camera looking +Z from negative Z).

### Vertices

| Index | Local position | Role |
|-------|----------------|------|
| 0 | `(-w/2, -h/2, 0)` | Bottom-left |
| 1 | `( w/2, -h/2, 0)` | Bottom-right |
| 2 | `(-w/2,  h/2, 0)` | Top-left |
| 3 | `( w/2,  h/2, 0)` | Top-right |

### Triangles (winding)

```csharp
0, 1, 2,   // BL → BR → TL
1, 3, 2    // BR → TR → TL
```

Counter-clockwise when viewed from **+Z** → front face +Z. Matches `Vector3.forward` normals. Wrong winding + single-sided materials = **invisible** quads (back-face culling).

### UVs and normals

- UVs use the **same index order as vertices** (`BL, BR, TL, TR`): `(0,0)` → `(1,0)` → `(0,1)` → `(1,1)`.
- Unity UV space: **U** left→right, **V** bottom→top (`V = 0` is the texture bottom).
- Normals: all `Vector3.forward` `(0, 0, 1)` for flat lighting on the front face.
- If a texture looks upside-down, flip **V** (e.g. swap `0`/`1` on V) — don’t reorder vertices to “fix” it.

---

## Matrices and drawing

Each instance is one `Matrix4x4.TRS(position, rotation, scale)`:

| Demo instance | Position | Rotation | Scale |
|---------------|----------|----------|-------|
| 1 | `(0, 0, 0)` | identity | `(1, 1, 1)` |
| 2 | `(3, 0, 0)` | Y 45° | `(1, 1, 1)` |
| 3 | `(-3, 0, 0)` | identity | `(2, 0.5, 1)` |

`Graphics.DrawMeshInstanced` draws one mesh many times. Same mesh, different matrices, one (or few) draw calls.

---

## Limits and gotchas

| Topic | Detail |
|-------|--------|
| Batch size | Max **1023** matrices per `DrawMeshInstanced` call. This guide uses one call and warns if you exceed that. |
| GPU Instancing | Material must have it enabled or nothing draws. |
| Winding vs camera | Front must face the camera (or use a double-sided shader). |
| Allocations | Matrix buffer is reused; grow reallocates only when instance count changes. |
| Mesh lifetime | Procedural mesh is `Destroy`ed in `OnDestroy`. |
| Not a collider | Visual only — no physics. See `CollisionManager` / `EnhancedMeshGenerator` for that path. |

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| Nothing visible | Missing material / Instancing off / winding / camera | Assign material, enable Instancing, keep winding `0,1,2` / `1,3,2`, camera `(0, 2, -5)` |
| Black quads | Lit shader, no light, bad normals | Add Directional Light, or use Unlit; check normals face the light |
| Looks flipped | Viewing back face or inverted winding | Match winding to normals, or flip with `Vector3.back` |
| Console error about instancing | Material flag | Check **Enable GPU Instancing** |
| Only first 1023 appear | API limit | Split into multiple `DrawMeshInstanced` calls |

---

## Experiments

### Grid

Replace `CreateQuadInstances()` body with nested loops placing `Matrix4x4.TRS` on a 5×5 grid (`x * 2`, `y * 2`).

### Spin one instance

In `Update`, before `RenderQuads()`:

```csharp
if (matrices.Count > 0)
{
    DecomposeMatrix(matrices[0], out Vector3 pos, out Quaternion rot, out Vector3 scale);
    rot *= Quaternion.Euler(0f, 0f, 50f * Time.deltaTime);
    matrices[0] = Matrix4x4.TRS(pos, rot, scale);
}
```

(`matrices` is private — edit inside the class for this experiment.)

### Wave

Place ~20 instances along X with `y = Mathf.Sin(i * 0.5f) * 2f`.

---

## Unity references

- [Mesh](https://docs.unity3d.com/ScriptReference/Mesh.html)
- [Matrix4x4](https://docs.unity3d.com/ScriptReference/Matrix4x4.html)
- [Graphics.DrawMeshInstanced](https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstanced.html)
