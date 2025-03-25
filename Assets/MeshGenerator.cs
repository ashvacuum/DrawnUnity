using System.Numerics;
using Unity.Mathematics;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


public class MeshGenerator : MonoBehaviour
{
    public Material material;
    public int instanceCount = 100;
    private Mesh quadMesh;
    private Matrix4x4[] matrices;
    public float width;
    public float height;

    void Start()
    {
        // Create the cube mesh
        CreateQuadMesh();
        
        // Set up matrices for instancing
        SetupCubeInstances();
    }

    void CreateQuadMesh()
    {
        quadMesh = new Mesh();
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(0, 0, 0),
            new Vector3(width, 0, 0),
            new Vector3(0, height, 0),
            new Vector3(width, height, 0)
        };
        
        int[] tris = new int[6]
        {
            // lower left triangle
            0, 2, 1,
            // upper right triangle
            2, 3, 1
        };
        
        Vector3[] normals = new Vector3[4]
        {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward
        };
        
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };

        // Assign data to the mesh
        quadMesh.vertices = vertices;
        quadMesh.triangles = tris;
        quadMesh.normals = normals;
        quadMesh.uv = uv;
        quadMesh.RecalculateNormals();
    }

    void SetupCubeInstances()
    {
        matrices = new Matrix4x4[instanceCount];
        
        for (int i = 0; i < instanceCount; i++)
        {
            // Random position
            Vector3 position = new Vector3(
                Random.Range(-100f, 100f),
                Random.Range(-100f, 100f),
                0
            );
            
            // Random rotation
            Quaternion rotation = Quaternion.Euler(
                0,
                0,
                Random.Range(0f, 360f)
            );
            
            // Random scale
            Vector3 scale = Vector3.one * Random.Range(0.2f, 2f);
            
            // Create transformation matrix
            matrices[i] = Matrix4x4.TRS(position, rotation, scale);
        }
    }

    void Update()
    {
        for(var i = 0; i < matrices.Length; i++)
        {
            DecomposeMatrix(matrices[i], out var pos, out var rot, out var scale);
            //pos += Vector3.up * 10f * Mathf.Sin(Time.time) * Time.deltaTime;
            //rot *= Quaternion.AngleAxis(20 * Time.deltaTime, Vector3.up);
            matrices[i] = Matrix4x4.TRS(pos, rot, scale);
        }
        
        for (int i = 0; i < matrices.Length; i += 1023) {
            int batchSize = Mathf.Min(1023, matrices.Length - i);
            Graphics.DrawMeshInstanced(quadMesh, 0, material, matrices, batchSize);
        }
        
        // Draw all instances every frame
        //Graphics.DrawMeshInstanced(cubeMesh, 0, material, matrices);
    }

    void DecomposeMatrix(Matrix4x4 matrix, out Vector3 position, out Quaternion rotation, out Vector3 scale)
    {
        position = matrix.GetColumn(3);

        // Extract rotation
        rotation = matrix.rotation;

        // Extract scale
        scale = matrix.lossyScale;

        position = matrix.GetPosition();

    }
}

