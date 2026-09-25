using System.Collections.Generic;
using UnityEngine;

// Only job: draw a list of matrices with one mesh + material.
// Splits into batches of 1023 because that's the GPU instancing limit.
public class InstancedBoxRenderer
{
    private const int MaxBatchSize = 1023;

    private readonly Mesh mesh;
    private readonly Material material;
    private readonly Matrix4x4[] batch = new Matrix4x4[MaxBatchSize];

    public InstancedBoxRenderer(Mesh mesh, Material material)
    {
        this.mesh = mesh;
        this.material = material;
    }

    public void Render(List<Matrix4x4> matrices)
    {
        for (int i = 0; i < matrices.Count; i += MaxBatchSize)
        {
            int count = Mathf.Min(MaxBatchSize, matrices.Count - i);
            for (int j = 0; j < count; j++)
            {
                batch[j] = matrices[i + j];
            }
            Graphics.DrawMeshInstanced(mesh, 0, material, batch, count);
        }
    }
}
