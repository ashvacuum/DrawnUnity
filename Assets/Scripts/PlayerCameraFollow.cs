using UnityEngine;

// Smoothly follows a target position on chosen axes, with an optional clamp.
// Position is pushed in via SetPlayerPosition (called each frame by PlayerController),
// since the player is a Matrix4x4, not a GameObject with a Transform.
public class PlayerCameraFollow : MonoBehaviour
{
    [Header("Follow Target")]
    public Vector3 offset = new Vector3(0, -5, -10);
    [Range(0f, 1f)] public float smoothSpeed = 0.125f;

    [Header("Follow Axes")]
    public bool followX = true;
    public bool followY = true;
    public bool followZ = false;

    [Header("Bounds")]
    public bool useConstraints = true;
    public Vector2 xConstraint = new Vector2(-100f, 100f);
    public Vector2 yConstraint = new Vector2(-50f, 100f);

    private Vector3 playerPosition;
    private bool hasTarget;

    public void SetPlayerPosition(Vector3 position)
    {
        playerPosition = position;
        hasTarget = true;
    }

    void LateUpdate()
    {
        if (!hasTarget) return; // SetPlayerPosition hasn't run yet

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

        // transform.LookAt(playerPosition); // uncomment to rotate the camera toward the player
    }
}
