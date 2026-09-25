using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An axis-aligned bounding box (AABB): a non-rotated box described by a
/// center and size. Used here instead of Unity's BoxCollider/Rigidbody so
/// the collision math stays visible in one small class.
/// </summary>
public class AABBBounds
{
    public Vector3 Center { get; private set; }
    public Vector3 Size { get; private set; }
    public Vector3 Extents { get; private set; }
    public Vector3 Min { get; private set; }
    public Vector3 Max { get; private set; }
    public int ID { get; private set; }
    public bool IsPlayer { get; private set; }
    public Matrix4x4 Matrix { get; set; }

    public AABBBounds(Vector3 center, Vector3 size, int id, bool isPlayer = false)
    {
        ID = id;
        IsPlayer = isPlayer;
        UpdateBounds(center, size);
    }

    public void UpdateBounds(Vector3 center, Vector3 size)
    {
        Center = center;
        Size = size;
        Extents = size * 0.5f; // Half-size for efficient calculations
        Min = center - Extents;
        Max = center + Extents;
    }

    /// <summary>
    /// True if this box and <paramref name="other"/> overlap on all three axes.
    /// Two AABBs overlap unless one is entirely to one side of the other on
    /// at least one axis — that's what each clause below checks (negated).
    /// </summary>
    public bool Intersects(AABBBounds other)
    {
        return !(Max.x < other.Min.x || Min.x > other.Max.x ||
                 Max.y < other.Min.y || Min.y > other.Max.y ||
                 Max.z < other.Min.z || Min.z > other.Max.z);
    }
}

/// <summary>
/// Central registry of every AABB in the scene, keyed by an int handle.
/// A lightweight stand-in for Unity's physics system: objects register a
/// box here instead of adding a BoxCollider, and ask
/// <see cref="CheckCollision"/> "would I overlap anything at this
/// position?" before actually moving. See
/// <c>Assets/Scripts/GameplaySystems_README.md</c> for how this fits together
/// with <c>EnhancedMeshGenerator</c>.
/// </summary>
public class CollisionManager : MonoBehaviour
{
    private static CollisionManager _instance;

    /// <summary>Lazily creates a persistent CollisionManager the first time it's used.</summary>
    public static CollisionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("CollisionManager");
                _instance = go.AddComponent<CollisionManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private readonly Dictionary<int, AABBBounds> _colliders = new Dictionary<int, AABBBounds>();
    private int nextID = 0;

    /// <summary>Adds a new box and returns the handle used for every other call below.</summary>
    public int RegisterCollider(Vector3 center, Vector3 size, bool isPlayer = false)
    {
        int id = nextID++;
        _colliders[id] = new AABBBounds(center, size, id, isPlayer);
        return id;
    }

    public void UpdateCollider(int id, Vector3 center, Vector3 size)
    {
        if (_colliders.TryGetValue(id, out AABBBounds bounds))
        {
            bounds.UpdateBounds(center, size);
        }
    }

    public void RemoveCollider(int id)
    {
        _colliders.Remove(id);
    }

    public void UpdateMatrix(int id, Matrix4x4 matrix)
    {
        if (_colliders.TryGetValue(id, out AABBBounds bounds))
        {
            bounds.Matrix = matrix;
        }
    }

    /// <summary>
    /// "If box <paramref name="id"/> moved to <paramref name="newCenter"/>, would it hit anything?"
    /// Checks against every other registered box (O(n) — fine for a
    /// classroom-sized scene, not meant to scale to thousands of colliders).
    /// </summary>
    public bool CheckCollision(int id, Vector3 newCenter, out List<int> collidingIds)
    {
        collidingIds = new List<int>();
        if (!_colliders.TryGetValue(id, out AABBBounds current))
            return false;

        // Temporary bounds at the proposed position; never registered.
        AABBBounds temp = new AABBBounds(newCenter, current.Size, -1);

        foreach (var kvp in _colliders)
        {
            if (kvp.Key == id) continue; // Skip self-collision

            if (temp.Intersects(kvp.Value))
            {
                collidingIds.Add(kvp.Key);
            }
        }
        return collidingIds.Count > 0;
    }

    public Matrix4x4 GetMatrix(int id)
    {
        if (_colliders.TryGetValue(id, out AABBBounds bounds))
        {
            return bounds.Matrix;
        }
        return Matrix4x4.identity;
    }
}
