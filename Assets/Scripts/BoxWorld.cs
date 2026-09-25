using System.Collections.Generic;
using UnityEngine;

// Only job: create and hold every box in the world (player, ground, random boxes),
// keeping each one's render matrix and CollisionManager entry in sync.
public class BoxWorld
{
    public List<Matrix4x4> Matrices { get; } = new List<Matrix4x4>();
    public int PlayerId { get; private set; } = -1;

    private readonly float width;
    private readonly float height;
    private readonly float depth;
    private int playerMatrixIndex = -1;

    public BoxWorld(float width, float height, float depth)
    {
        this.width = width;
        this.height = height;
        this.depth = depth;
    }

    public void CreatePlayer(Vector3 position)
    {
        PlayerId = AddBox(position, Quaternion.identity, Vector3.one, ScaledSize(Vector3.one), isPlayer: true);
        playerMatrixIndex = Matrices.Count - 1;
    }

    public void CreateGround(Vector3 position, float groundWidth, float groundDepth)
    {
        Vector3 scale = new Vector3(groundWidth, 1f, groundDepth);
        AddBox(position, Quaternion.identity, scale, scale, isPlayer: false);
    }

    public void SpawnRandomBoxes(int count, Vector2 xRange, Vector2 yRange, float z)
    {
        for (int i = 0; i < count; i++)
        {
            AddRandomBox(xRange, yRange, z);
        }
    }

    public void AddRandomBox(Vector2 xRange, Vector2 yRange, float z)
    {
        Vector3 position = new Vector3(Random.Range(xRange.x, xRange.y), Random.Range(yRange.x, yRange.y), z);
        Quaternion rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        Vector3 scale = new Vector3(Random.Range(0.5f, 3f), Random.Range(0.5f, 3f), Random.Range(0.5f, 3f));
        AddBox(position, rotation, scale, ScaledSize(scale), isPlayer: false);
    }

    // Called every frame by PlayerController to write the player's new position back.
    public void MovePlayer(Vector3 position)
    {
        Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
        Matrices[playerMatrixIndex] = matrix;
        CollisionManager.Instance.UpdateCollider(PlayerId, position, ScaledSize(Vector3.one));
        CollisionManager.Instance.UpdateMatrix(PlayerId, matrix);
    }

    private Vector3 ScaledSize(Vector3 scale) => new Vector3(width * scale.x, height * scale.y, depth * scale.z);

    private int AddBox(Vector3 position, Quaternion rotation, Vector3 scale, Vector3 colliderSize, bool isPlayer)
    {
        int id = CollisionManager.Instance.RegisterCollider(position, colliderSize, isPlayer);
        Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);
        Matrices.Add(matrix);
        CollisionManager.Instance.UpdateMatrix(id, matrix);
        return id;
    }
}
