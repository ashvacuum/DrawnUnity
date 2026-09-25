using UnityEngine;

// Only job: read Inspector settings, build the pieces (mesh, world, player, renderer),
// and drive them from Start()/Update(). See GameplaySystems_README.md for how they fit together.
public class EnhancedMeshGenerator : MonoBehaviour
{
    [Header("Rendering")]
    public Material material;
    public int instanceCount = 100;
    public float width = 1f;
    public float height = 1f;
    public float depth = 1f;

    [Header("Player")]
    public float movementSpeed = 5f;
    public float gravity = 9.8f;

    [Header("Camera")]
    public PlayerCameraFollow cameraFollow;

    [Header("World Layout")]
    [Tooltip("Player, ground, and random boxes all share this Z so the scene stays on one plane.")]
    public float constantZPosition = 0f;
    public float minX = -50f;
    public float maxX = 50f;
    public float minY = -50f;
    public float maxY = 50f;

    [Header("Ground")]
    public float groundY = -20f;
    public float groundWidth = 200f;
    public float groundDepth = 200f;

    private BoxWorld world;
    private PlayerController player;
    private InstancedBoxRenderer boxRenderer;

    void Start()
    {
        SetupCamera();

        Mesh mesh = CubeMeshBuilder.Build(width, height, depth);
        boxRenderer = new InstancedBoxRenderer(mesh, material);

        world = new BoxWorld(width, height, depth);

        Vector3 playerStart = new Vector3(0, 10, constantZPosition);
        world.CreatePlayer(playerStart);
        world.CreateGround(new Vector3(0, groundY, constantZPosition), groundWidth, groundDepth);
        world.SpawnRandomBoxes(instanceCount - 2, new Vector2(minX, maxX), new Vector2(minY, maxY), constantZPosition);

        player = new PlayerController(world, cameraFollow, playerStart, movementSpeed, gravity);
    }

    void Update()
    {
        player.Tick(Time.deltaTime);
        boxRenderer.Render(world.Matrices);
    }

    private void SetupCamera()
    {
        if (cameraFollow != null) return;

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
            // No camera in the scene yet — create one so the sample still runs.
            GameObject cameraObj = new GameObject("PlayerCamera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cameraFollow = cameraObj.AddComponent<PlayerCameraFollow>();
            cam.tag = "MainCamera";
        }

        cameraFollow.offset = new Vector3(0, 0, -15);
        cameraFollow.smoothSpeed = 0.1f;
    }

    // Hook this up to a button to test spawning more boxes at runtime.
    public void AddRandomBox()
    {
        world.AddRandomBox(new Vector2(minX, maxX), new Vector2(minY, maxY), constantZPosition);
    }
}
