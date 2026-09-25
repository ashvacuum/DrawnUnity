using System.Collections.Generic;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

/// <summary>
/// Teaching sample: a procedural cube mesh drawn many times with
/// <see cref="Graphics.DrawMeshInstanced"/>, a simple AABB-based platformer
/// controller (<see cref="CollisionManager"/>), and a camera that follows the
/// player. Everything is driven by <see cref="Matrix4x4"/> transforms instead
/// of GameObjects/Transforms/Colliders — one draw call, no physics engine.
/// See <c>Assets/Scripts/GameplaySystems_README.md</c> for full documentation,
/// and the assignment brief in the repo root <c>README.md</c> for what to add
/// on top of this (jump, powerups, enemies, UI, ...).
/// </summary>
public class EnhancedMeshGenerator : MonoBehaviour
{
    #region Rendering

    [Header("Rendering")]
    [Tooltip("Material used for every box instance. Must support GPU instancing.")]
    public Material material;
    [Tooltip("Total number of boxes to draw, including the player and the ground.")]
    public int instanceCount = 100;

    [Tooltip("Box width in local space (X).")]
    public float width = 1f;
    [Tooltip("Box height in local space (Y).")]
    public float height = 1f;
    [Tooltip("Box depth in local space (Z).")]
    public float depth = 1f;

    private const int MAX_INSTANCES_PER_BATCH = 1023; // Graphics.DrawMeshInstanced hard limit.

    private Mesh cubeMesh;
    private readonly List<Matrix4x4> matrices = new List<Matrix4x4>();
    private readonly List<int> colliderIds = new List<int>();

    #endregion

    #region Player

    [Header("Player")]
    [Tooltip("Horizontal movement speed in units/second.")]
    public float movementSpeed = 5f;
    [Tooltip("Downward acceleration applied while airborne, in units/second^2.")]
    public float gravity = 9.8f;

    private int playerID = -1;
    private int playerMatrixIndex = -1; // Index into matrices/colliderIds for the player; set once in CreatePlayer.
    private Vector3 playerVelocity = Vector3.zero;
    private bool isGrounded = false;

    #endregion

    #region Camera

    [Header("Camera")]
    [Tooltip("Camera that follows the player. Auto-assigned/created in Start() if left empty.")]
    public PlayerCameraFollow cameraFollow;

    #endregion

    #region World Layout

    [Header("World Layout")]
    [Tooltip("Shared Z position for the player, ground, and every random box — keeps the whole scene on one plane.")]
    public float constantZPosition = 0f;

    [Tooltip("Random X range used when placing generated boxes.")]
    public float minX = -50f;
    public float maxX = 50f;
    [Tooltip("Random Y range used when placing generated boxes.")]
    public float minY = -50f;
    public float maxY = 50f;

    [Header("Ground")]
    public float groundY = -20f;
    public float groundWidth = 200f;
    public float groundDepth = 200f;

    #endregion

    #region Lifecycle

    void Start()
    {
        SetupCamera();
        CreateCubeMesh();
        CreatePlayer();
        CreateGround();
        GenerateRandomBoxes();
    }

    void Update()
    {
        UpdatePlayer();
        RenderBoxes();
    }

    #endregion

    #region Setup

    void SetupCamera()
    {
        if (cameraFollow != null)
            return;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraFollow = mainCamera.GetComponent<PlayerCameraFollow>();
            if (cameraFollow == null)
            {
                cameraFollow = mainCamera.gameObject.AddComponent<PlayerCameraFollow>();
            }
        }
        else
        {
            // No main camera in the scene yet — create one so the sample still runs.
            GameObject cameraObj = new GameObject("PlayerCamera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cameraFollow = cameraObj.AddComponent<PlayerCameraFollow>();
            cam.tag = "MainCamera";
        }

        cameraFollow.offset = new Vector3(0, 0, -15);
        cameraFollow.smoothSpeed = 0.1f;
    }

    /// <summary>
    /// Builds one unit cube (0,0,0) to (width,height,depth) — every instance
    /// reuses this mesh and just gets a different Matrix4x4.
    /// </summary>
    void CreateCubeMesh()
    {
        cubeMesh = new Mesh();

        Vector3[] vertices = new Vector3[8]
        {
            // Bottom face vertices
            new Vector3(0, 0, 0),       // Bottom front left - 0
            new Vector3(width, 0, 0),   // Bottom front right - 1
            new Vector3(width, 0, depth),// Bottom back right - 2
            new Vector3(0, 0, depth),   // Bottom back left - 3

            // Top face vertices
            new Vector3(0, height, 0),       // Top front left - 4
            new Vector3(width, height, 0),   // Top front right - 5
            new Vector3(width, height, depth),// Top back right - 6
            new Vector3(0, height, depth)    // Top back left - 7
        };

        // Triangles for the 6 faces (2 triangles per face)
        int[] triangles = new int[36]
        {
            // Front face triangles (facing -Z)
            0, 4, 1,
            1, 4, 5,

            // Back face triangles (facing +Z)
            2, 6, 3,
            3, 6, 7,

            // Left face triangles (facing -X)
            0, 3, 4,
            4, 3, 7,

            // Right face triangles (facing +X)
            1, 5, 2,
            2, 5, 6,

            // Bottom face triangles (facing -Y)
            0, 1, 3,
            3, 1, 2,

            // Top face triangles (facing +Y)
            4, 7, 5,
            5, 7, 6
        };

        Vector2[] uvs = new Vector2[8];
        for (int i = 0; i < 8; i++)
        {
            uvs[i] = new Vector2(vertices[i].x / width, vertices[i].z / depth);
        }

        cubeMesh.vertices = vertices;
        cubeMesh.triangles = triangles;
        cubeMesh.uv = uvs;
        cubeMesh.RecalculateNormals();
        cubeMesh.RecalculateBounds();
    }

    #endregion

    #region Box Creation

    /// <summary>
    /// Registers a box with <see cref="CollisionManager"/> and appends its
    /// draw matrix. Every box in the scene — player, ground, and random
    /// boxes — is created through this one path so there's a single place
    /// that keeps <c>matrices</c> and <c>colliderIds</c> in sync.
    /// </summary>
    int CreateBox(Vector3 position, Quaternion rotation, Vector3 scale, Vector3 colliderSize, bool isPlayer)
    {
        int id = CollisionManager.Instance.RegisterCollider(position, colliderSize, isPlayer);

        Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);
        matrices.Add(matrix);
        colliderIds.Add(id);
        CollisionManager.Instance.UpdateMatrix(id, matrix);

        return id;
    }

    /// <summary>Collider size for a box whose mesh scale is <paramref name="scale"/> (mesh dimensions times scale, per axis).</summary>
    Vector3 ScaledColliderSize(Vector3 scale) => new Vector3(width * scale.x, height * scale.y, depth * scale.z);

    Vector3 RandomBoxPosition() => new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), constantZPosition);
    Quaternion RandomBoxRotation() => Quaternion.Euler(0, 0, Random.Range(0f, 360f)); // Z-only, since the whole scene sits on one Z plane.
    Vector3 RandomBoxScale() => new Vector3(Random.Range(0.5f, 3f), Random.Range(0.5f, 3f), Random.Range(0.5f, 3f));

    void CreatePlayer()
    {
        Vector3 playerPosition = new Vector3(0, 10, constantZPosition);
        playerID = CreateBox(playerPosition, Quaternion.identity, Vector3.one, ScaledColliderSize(Vector3.one), isPlayer: true);
        playerMatrixIndex = matrices.Count - 1; // CreatePlayer always runs before any other box, so this is 0 — stored explicitly rather than re-searched every frame.
    }

    void CreateGround()
    {
        Vector3 groundPosition = new Vector3(0, groundY, constantZPosition);
        Vector3 groundScale = new Vector3(groundWidth, 1f, groundDepth);
        CreateBox(groundPosition, Quaternion.identity, groundScale, new Vector3(groundWidth, 1f, groundDepth), isPlayer: false);
    }

    void GenerateRandomBoxes()
    {
        for (int i = 0; i < instanceCount - 2; i++) // -2: player and ground already used two of the instanceCount slots.
        {
            Vector3 scale = RandomBoxScale();
            CreateBox(RandomBoxPosition(), RandomBoxRotation(), scale, ScaledColliderSize(scale), isPlayer: false);
        }
    }

    /// <summary>Adds one more random box at runtime — wire this up to a button or trigger to test.</summary>
    public void AddRandomBox()
    {
        Vector3 scale = RandomBoxScale();
        CreateBox(RandomBoxPosition(), RandomBoxRotation(), scale, ScaledColliderSize(scale), isPlayer: false);
    }

    #endregion

    #region Player Movement

    /// <summary>
    /// Reads input, resolves horizontal then vertical movement against
    /// <see cref="CollisionManager"/> one axis at a time (so sliding along a
    /// wall doesn't also block vertical motion), and reports the result to
    /// the camera. This is the extension point for assignment requirement 1
    /// (jump): apply an upward velocity here in response to input while
    /// <c>isGrounded</c> is true.
    /// </summary>
    void UpdatePlayer()
    {
        if (playerID == -1) return;

        Matrix4x4 playerMatrix = matrices[playerMatrixIndex];
        DecomposeMatrix(playerMatrix, out Vector3 pos, out Quaternion rot, out Vector3 scale);

        if (isGrounded)
        {
            playerVelocity.y = 0;
        }
        else
        {
            playerVelocity.y -= gravity * Time.deltaTime;
        }

        // --- Horizontal movement ---
        float horizontal = 0;
        if (Input.GetKey(KeyCode.A)) horizontal -= 1;
        if (Input.GetKey(KeyCode.D)) horizontal += 1;

        Vector3 horizontalTarget = pos;
        horizontalTarget.x += horizontal * movementSpeed * Time.deltaTime;

        if (!CheckCollisionAt(playerID, new Vector3(horizontalTarget.x, pos.y, pos.z)))
        {
            pos.x = horizontalTarget.x;
        }

        // --- Vertical movement (gravity) ---
        Vector3 verticalTarget = pos;
        verticalTarget.y += playerVelocity.y * Time.deltaTime;

        if (CheckCollisionAt(playerID, new Vector3(pos.x, verticalTarget.y, pos.z)))
        {
            if (playerVelocity.y < 0)
            {
                isGrounded = true; // Hit something below while falling.
            }
            playerVelocity.y = 0;
        }
        else
        {
            pos.y = verticalTarget.y;
            isGrounded = false;
        }

        Matrix4x4 newMatrix = Matrix4x4.TRS(pos, rot, scale);
        matrices[playerMatrixIndex] = newMatrix;

        CollisionManager.Instance.UpdateCollider(playerID, pos, ScaledColliderSize(scale));
        CollisionManager.Instance.UpdateMatrix(playerID, newMatrix);

        if (cameraFollow != null)
        {
            cameraFollow.SetPlayerPosition(pos);
        }
    }

    bool CheckCollisionAt(int id, Vector3 position)
    {
        return CollisionManager.Instance.CheckCollision(id, position, out _);
    }

    #endregion

    #region Rendering

    void RenderBoxes()
    {
        Matrix4x4[] matrixArray = matrices.ToArray();

        // Graphics.DrawMeshInstanced caps out at 1023 matrices per call, so large
        // instance counts are split into batches.
        for (int i = 0; i < matrixArray.Length; i += MAX_INSTANCES_PER_BATCH)
        {
            int batchSize = Mathf.Min(MAX_INSTANCES_PER_BATCH, matrixArray.Length - i);
            Matrix4x4[] batchMatrices = new Matrix4x4[batchSize];
            System.Array.Copy(matrixArray, i, batchMatrices, 0, batchSize);
            Graphics.DrawMeshInstanced(cubeMesh, 0, material, batchMatrices, batchSize);
        }
    }

    void DecomposeMatrix(Matrix4x4 matrix, out Vector3 position, out Quaternion rotation, out Vector3 scale)
    {
        position = matrix.GetPosition();
        rotation = matrix.rotation;
        scale = matrix.lossyScale;
    }

    #endregion
}
