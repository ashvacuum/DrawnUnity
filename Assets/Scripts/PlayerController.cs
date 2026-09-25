using UnityEngine;

// Only job: read input, apply gravity, and move the player against CollisionManager.
// This is where jump (assignment requirement 1) belongs.
public class PlayerController
{
    private readonly BoxWorld world;
    private readonly PlayerCameraFollow cameraFollow;
    private readonly float speed;
    private readonly float gravity;

    private Vector3 position;
    private Vector3 velocity;
    private bool isGrounded;

    public PlayerController(BoxWorld world, PlayerCameraFollow cameraFollow, Vector3 startPosition, float speed, float gravity)
    {
        this.world = world;
        this.cameraFollow = cameraFollow;
        this.speed = speed;
        this.gravity = gravity;
        position = startPosition;
    }

    public void Tick(float deltaTime)
    {
        ApplyGravity(deltaTime);
        MoveHorizontal(deltaTime);
        MoveVertical(deltaTime);

        world.MovePlayer(position);
        cameraFollow?.SetPlayerPosition(position);
    }

    private void ApplyGravity(float deltaTime)
    {
        if (isGrounded)
        {
            velocity.y = 0;
        }
        else
        {
            velocity.y -= gravity * deltaTime;
        }
    }

    private void MoveHorizontal(float deltaTime)
    {
        float input = 0;
        if (Input.GetKey(KeyCode.A)) input -= 1;
        if (Input.GetKey(KeyCode.D)) input += 1;

        Vector3 target = position;
        target.x += input * speed * deltaTime;

        if (!IsBlocked(target))
        {
            position.x = target.x;
        }
    }

    private void MoveVertical(float deltaTime)
    {
        Vector3 target = position;
        target.y += velocity.y * deltaTime;

        if (IsBlocked(target))
        {
            if (velocity.y < 0) isGrounded = true; // hit something while falling
            velocity.y = 0;
        }
        else
        {
            position.y = target.y;
            isGrounded = false;
        }
    }

    private bool IsBlocked(Vector3 proposedPosition)
    {
        return CollisionManager.Instance.CheckCollision(world.PlayerId, proposedPosition, out _);
    }
}
