using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GenerateNoise
{
    public static float[,] GenerateNoiseMap(int width, int height, float scale) 
    {
        float[,] noiseMap = new float[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; y++) 
            {
                float sampleX = x / Mathf.Max(.001f, scale);
                float sampleY = y / Mathf.Max(.001f, scale);

                float perlin = Mathf.PerlinNoise(sampleX, sampleY);

                noiseMap[x,y] = perlin;
            }
        }

        return noiseMap;
    }
}
