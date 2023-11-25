using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public static class MeshGen
{
    public static void GenerateTerrainMesh(float[,] heightMap) 
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        MeshData meshData = new MeshData(width, height);
        int index = 0;

        for(int y = 0; y < height; y++) 
        {
            for(int x = 0; x < width; x++) 
            {
                meshData.vertices[index] = new Vector3(x, heightMap[x, y], y);
                index++;
            }
        }
    }
}

public class MeshData 
{
    public Vector3[] vertices;
    public int[] triangles;

    int triangleIndex;

    public MeshData(int meshWidth, int meshHeight) 
    {
        vertices = new Vector3[meshWidth * meshHeight];
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
}
