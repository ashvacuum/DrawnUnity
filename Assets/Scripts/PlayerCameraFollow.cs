using UnityEngine;

/// <summary>
/// Smoothly follows a target position on selected axes, with an optional
/// rectangular clamp. The target is pushed in via <see cref="SetPlayerPosition"/>
/// (called each frame from <c>EnhancedMeshGenerator.UpdatePlayer</c>) rather
/// than read from a Transform, because the "player" here is a Matrix4x4
/// entry drawn with <c>Graphics.DrawMeshInstanced</c>, not a GameObject.
/// See <c>Assets/Scripts/GameplaySystems_README.md</c> for the full picture.
/// </summary>
public class PlayerCameraFollow : MonoBehaviour
{
    [Header("Follow Target")]
    [Tooltip("Offset from the player position, e.g. (0, 0, -15) to sit back and look down the Z axis.")]
    public Vector3 offset = new Vector3(0, -5, -10);
    [Tooltip("Lerp factor per frame: 0 = camera never moves, 1 = camera snaps instantly.")]
    public float smoothSpeed = 0.125f;

    [Header("Follow Axes")]
    public bool followX = true;
    public bool followY = true;
    public bool followZ = false;

    [Header("Bounds")]
    [Tooltip("Clamp the camera's final X/Y position inside xConstraint/yConstraint.")]
    public bool useConstraints = true;
    public Vector2 xConstraint = new Vector2(-100f, 100f);
    public Vector2 yConstraint = new Vector2(-50f, 100f);

    private Vector3 playerPosition;
    private bool hasTarget;

    /// <summary>Reports the player's current position. Call this every frame from whatever owns the player.</summary>
    public void SetPlayerPosition(Vector3 position)
    {
        playerPosition = position;
        hasTarget = true;
    }

    void LateUpdate()
    {
        // Nothing to follow until SetPlayerPosition has run at least once.
        // Tracked with an explicit flag rather than "playerPosition == Vector3.zero",
        // since a player standing at the world origin would otherwise freeze the camera.
        if (!hasTarget)
            return;

        Vector3 desiredPosition = transform.position;

        if (followX) desiredPosition.x = playerPosition.x + offset.x;
        if (followY) desiredPosition.y = playerPosition.y + offset.y;
        if (followZ) desiredPosition.z = playerPosition.z + offset.z;

        if (useConstraints)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, xConstraint.x, xConstraint.y);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, yConstraint.x, yConstraint.y);
        }

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Look at player (optional - uncomment for a camera that rotates to face the player):
        // transform.LookAt(playerPosition);
    }
}
