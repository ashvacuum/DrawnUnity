using UnityEngine;

// Only job: build a cube mesh. Nothing else needs to know how vertices/triangles/UVs work.
public static class CubeMeshBuilder
{
    public static Mesh Build(float width, float height, float depth)
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[8]
        {
            new Vector3(0, 0, 0),
            new Vector3(width, 0, 0),
            new Vector3(width, 0, depth),
            new Vector3(0, 0, depth),

            new Vector3(0, height, 0),
            new Vector3(width, height, 0),
            new Vector3(width, height, depth),
            new Vector3(0, height, depth)
        };

        int[] triangles = new int[36]
        {
            0, 4, 1,  1, 4, 5,  // front
            2, 6, 3,  3, 6, 7,  // back
            0, 3, 4,  4, 3, 7,  // left
            1, 5, 2,  2, 5, 6,  // right
            0, 1, 3,  3, 1, 2,  // bottom
            4, 7, 5,  5, 7, 6   // top
        };

        Vector2[] uvs = new Vector2[8];
        for (int i = 0; i < 8; i++)
        {
            uvs[i] = new Vector2(vertices[i].x / width, vertices[i].z / depth);
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
