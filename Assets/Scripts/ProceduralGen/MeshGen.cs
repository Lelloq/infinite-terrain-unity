using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AI;

public static class MeshGen
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMult, AnimationCurve heightCurve, int lod, bool useFlatShading)
    {
        AnimationCurve localHeightCurve = new(heightCurve.keys);
        int meshSimplificationIncrement = Mathf.Max(1, lod * 2);

        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * meshSimplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;

        float topLeftX = (meshSizeUnsimplified - 1) / -2f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

        int vertsPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new(vertsPerLine, useFlatShading);
        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;

        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;
                if (isBorderVertex)
                {
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                int vertexIndex = vertexIndicesMap[x, y];
                Vector2 percent = new((x - meshSimplificationIncrement) / (float)meshSize, (y - meshSimplificationIncrement) / (float)meshSize);
                float height = localHeightCurve.Evaluate(heightMap[x, y]) * heightMult;
                Vector3 vertexPos = new(topLeftX + percent.x * meshSizeUnsimplified, height, topLeftZ - percent.y * meshSizeUnsimplified);

                meshData.AddVertex(vertexPos, percent, vertexIndex);

                if (x < borderedSize - 1 && y < borderedSize - 1) //Ignoring bottom, rightmost edges
                {
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + meshSimplificationIncrement, y];
                    int c = vertexIndicesMap[x, y + meshSimplificationIncrement];
                    int d = vertexIndicesMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];
                    meshData.AddTriangles(a, d, c);
                    meshData.AddTriangles(d, a, b);
                }
            }
        }

        if (meshData.IsFlatShaded())
        {
            meshData.FlatShading();
        }
        else
        {
            meshData.BakeNormals();
        }

        return meshData;
    }
}

public class MeshData
{
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;
    Vector3[] bakedNormals;

    readonly Vector3[] borderVertices;
    readonly int[] borderTriangles;

    int triangleIndex;
    int borderTriangleIndex;

    bool useFlatShading;

    public MeshData(int vertsPerLine, bool useFlatShading)
    {
        this.useFlatShading = useFlatShading;
        vertices = new Vector3[vertsPerLine * vertsPerLine];
        uvs = new Vector2[vertsPerLine * vertsPerLine];
        //Quad consists of 2 triangles so there are 6 vertices per quad for the terrain
        triangles = new int[(vertsPerLine - 1) * (vertsPerLine - 1) * 6];

        //Sides of the borders + 4 corners
        borderVertices = new Vector3[vertsPerLine * 4 + 4];
        borderTriangles = new int[24 * vertsPerLine];
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        if (vertexIndex < 0)
        {
            borderVertices[-vertexIndex - 1] = vertexPosition;
        }
        else
        {
            vertices[vertexIndex] = vertexPosition;
            uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangles(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex += 3;
        }
        else
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }
    }

    Vector3[] CalculateNormals()
    {
        Vector3[] normals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length / 3;

        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriIndex = i * 3;
            int vertexIndexA = triangles[normalTriIndex];
            int vertexIndexB = triangles[normalTriIndex + 1];
            int vertexIndexC = triangles[normalTriIndex + 2];

            Vector3 normal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            normals[vertexIndexA] += normal;
            normals[vertexIndexB] += normal;
            normals[vertexIndexC] += normal;
        }

        int borderTriangleCount = borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriIndex];
            int vertexIndexB = borderTriangles[normalTriIndex + 1];
            int vertexIndexC = borderTriangles[normalTriIndex + 2];

            Vector3 normal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            if (vertexIndexA >= 0) normals[vertexIndexA] += normal;
            if (vertexIndexB >= 0) normals[vertexIndexB] += normal;
            if (vertexIndexC >= 0) normals[vertexIndexC] += normal;
        }

        foreach (Vector3 normal in normals)
        {
            normal.Normalize();
        }

        return normals;
    }

    Vector3 SurfaceNormalFromIndices(int a, int b, int c)
    {
        Vector3 pA = (a < 0) ? borderVertices[-a - 1] : vertices[a];
        Vector3 pB = (b < 0) ? borderVertices[-b - 1] : vertices[b];
        Vector3 pC = (c < 0) ? borderVertices[-c - 1] : vertices[c];

        Vector3 AB = pB - pA;
        Vector3 AC = pC - pA;

        return Vector3.Cross(AB, AC).normalized;
    }

    public bool IsFlatShaded()
    {
        return useFlatShading;
    } 

    public void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }

    public void FlatShading()
    {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUVs = new Vector2[triangles.Length];

        for (int i = 0; i < triangles.Length; i++)
        {
            flatShadedVertices[i] = vertices[triangles[i]];
            flatShadedUVs[i] = uvs[triangles[i]];
            triangles[i] = i;
        }

        vertices = flatShadedVertices;
        uvs = flatShadedUVs;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        if (useFlatShading)
        {
            mesh.RecalculateNormals();
        }
        else
        {
            mesh.normals = bakedNormals;
        }

        return mesh;
    }

}
