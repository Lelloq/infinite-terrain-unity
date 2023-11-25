using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.AI;

public static class MeshGen
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap) 
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        MeshData meshData = new MeshData(width, height);
        int index = 0;

        for(int y = 0; y < height; y++) 
        {
            for(int x = 0; x < width; x++) 
            {
                meshData.vertices[index] = new Vector3(topLeftX + x, heightMap[x, y], topLeftZ - y);
                meshData.uvs[index] = new Vector2(x / (float)width,y / (float)height);

                if(x < width - 1 && y < height - 1) //Ignoring bottom, rightmost edges
                {
                    meshData.AddTriangles(index, index + width + 1, index + width);
                    meshData.AddTriangles(index + width + 1, index, index + 1);
                }
                index++;
            }
        }

        return meshData;
    }
}

public class MeshData 
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    int triangleIndex;

    public MeshData(int meshWidth, int meshHeight) 
    {
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        //Quad consists of 2 triangles so there are 6 vertices per quad for the terrain
        triangles = new int[(meshWidth - 1) * (meshHeight-1) * 6];
    }

    public void AddTriangles(int a, int b, int c) 
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex+1] = b;
        triangles[triangleIndex+2] = c;
        triangleIndex += 3;
    }

    public Mesh CreateMesh() 
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        return mesh;
    }

}