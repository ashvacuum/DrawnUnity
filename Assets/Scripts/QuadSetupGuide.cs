using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Teaching sample: build a procedural quad mesh and draw instances with
/// <see cref="Graphics.DrawMeshInstanced"/> and <see cref="Matrix4x4.TRS"/>.
/// See <c>Assets/Scripts/QuadSetup_README.md</c> for full documentation.
/// </summary>
public class QuadSetupGuide : MonoBehaviour
{
    #region Fields

    private const int MAX_INSTANCES_PER_BATCH = 1023;

    [SerializeField]
    [Tooltip("Material used for all instances. Enable GPU Instancing on the asset.")]
    private Material material;

    [SerializeField]
    [Tooltip("Quad width in local space (X).")]
    private float width = 1f;

    [SerializeField]
    [Tooltip("Quad height in local space (Y).")]
    private float height = 1f;

    private Mesh quadMesh;
    private List<Matrix4x4> matrices = new List<Matrix4x4>();
    private Matrix4x4[] matrixArray;

    #endregion

    #region Lifecycle

    void Start()
    {
        if (material == null)
        {
            Debug.LogError("QuadSetupGuide: Assign a Material in the Inspector.");
            enabled = false;
            return;
        }

        // DrawMeshInstanced requires GPU Instancing enabled on the material.
        if (!material.enableInstancing)
        {
            material.enableInstancing = true;
            Debug.LogWarning("QuadSetupGuide: Enabled GPU Instancing on the material.");
        }

        CreateQuadMesh();
        CreateQuadInstances();
    }

    void Update()
    {
        RenderQuads();
    }

    void OnDestroy()
    {
        if (quadMesh != null)
        {
            Destroy(quadMesh);
            quadMesh = null;
        }
    }

    #endregion

    #region Mesh

    /// <summary>
    /// Builds a unit-centered XY quad facing +Z (winding and normals aligned).
    /// </summary>
    void CreateQuadMesh()
    {
        quadMesh = new Mesh();
        quadMesh.name = "ProceduralQuad";

        // Quad centered at origin, front face toward +Z (matches default camera looking +Z).
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-width * 0.5f, -height * 0.5f, 0f), // Bottom-left  [0]
            new Vector3(width * 0.5f, -height * 0.5f, 0f),  // Bottom-right [1]
            new Vector3(-width * 0.5f, height * 0.5f, 0f),  // Top-left     [2]
            new Vector3(width * 0.5f, height * 0.5f, 0f)    // Top-right    [3]
        };

        // Counter-clockwise when viewed from +Z → front face +Z (matches normals).
        int[] triangles = new int[6]
        {
            0, 1, 2, // Bottom-left, bottom-right, top-left
            1, 3, 2  // Bottom-right, top-right, top-left
        };

        // Same index order as vertices (BL, BR, TL, TR) — not a clockwise ring.
        // Unity: U 0→1 left→right, V 0→1 bottom→top.
        Vector2[] uvs = new Vector2[4]
        {
            new Vector2(0f, 0f), // [0] bottom-left
            new Vector2(1f, 0f), // [1] bottom-right
            new Vector2(0f, 1f), // [2] top-left
            new Vector2(1f, 1f)  // [3] top-right
        };

        Vector3[] normals = new Vector3[4]
        {
            Vector3.forward,
            Vector3.forward,
            Vector3.forward,
            Vector3.forward
        };

        quadMesh.vertices = vertices;
        quadMesh.triangles = triangles;
        quadMesh.uv = uvs;
        quadMesh.normals = normals;
        quadMesh.RecalculateBounds();
    }

    #endregion

    #region Instances

    /// <summary>
    /// Seeds three demo instances: origin, Y-rotated, and non-uniform scale.
    /// </summary>
    void CreateQuadInstances()
    {
        // Origin, no rotation
        matrices.Add(Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one));

        // Rotated 45° on Y, offset right
        matrices.Add(Matrix4x4.TRS(
            new Vector3(3f, 0f, 0f),
            Quaternion.Euler(0f, 45f, 0f),
            Vector3.one));

        // Scaled, offset left
        matrices.Add(Matrix4x4.TRS(
            new Vector3(-3f, 0f, 0f),
            Quaternion.identity,
            new Vector3(2f, 0.5f, 1f)));
    }

    /// <summary>
    /// Appends a new instance transform.
    /// </summary>
    public void AddQuad(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        matrices.Add(Matrix4x4.TRS(position, rotation, scale));
    }

    /// <summary>
    /// Extracts position, rotation, and lossy scale from a TRS matrix.
    /// </summary>
    public void DecomposeMatrix(Matrix4x4 matrix, out Vector3 position,
                                out Quaternion rotation, out Vector3 scale)
    {
        position = matrix.GetPosition();
        rotation = matrix.rotation;
        scale = matrix.lossyScale;
    }

    #endregion

    #region Rendering

    void RenderQuads()
    {
        if (quadMesh == null || material == null || matrices.Count == 0)
        {
            return;
        }

        RefreshMatrixArray();

        // ponytail: one DrawMeshInstanced call; ceil = 1023. Split into batches if you go past that.
        int count = Mathf.Min(matrixArray.Length, MAX_INSTANCES_PER_BATCH);

        if (matrixArray.Length > MAX_INSTANCES_PER_BATCH)
        {
            Debug.LogWarning("QuadSetupGuide: DrawMeshInstanced max is 1023; extra instances skipped.");
        }

        Graphics.DrawMeshInstanced(quadMesh, 0, material, matrixArray, count);
    }

    void RefreshMatrixArray()
    {
        // Reuse the array; copy so in-place matrix edits (e.g. animation experiments) still render.
        if (matrixArray == null || matrixArray.Length != matrices.Count)
        {
            matrixArray = new Matrix4x4[matrices.Count];
        }

        for (int i = 0; i < matrices.Count; i++)
        {
            matrixArray[i] = matrices[i];
        }
    }

    #endregion
}
