using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GenerateNoise
{
    public static float[,] GenerateNoiseMap(int width, int height, float scale, int seed, int octaves, float persistance, float frequency, Vector2 offset) 
    {
        float[,] noiseMap = new float[width, height];
        float maxHeight = float.MinValue;
        float minHeight = float.MaxValue;

        float halfX = width / 2;
        float halfY = height / 2;

        System.Random random = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        for(int i = 0; i < octaves; i++) 
        {
            float offsetX = random.Next(-100000, 100000) + offset.x;
            float offsetY = random.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++) 
            {
                float freq = 1;
                float amp = 1;
                float noiseHeight = 0;

                for(int i = 0; i < octaves; i++) 
                {
                    float sampleX = (x - halfX) / Mathf.Max(.0001f, scale) * freq + octaveOffsets[i].x;
                    float sampleY = (y - halfY) / Mathf.Max(.0001f, scale) * freq + octaveOffsets[i].y;

                    float perlin = Mathf.PerlinNoise(sampleX, sampleY);
                    noiseHeight += perlin * amp;

                    amp *= persistance;
                    freq *= frequency;
                }
                noiseMap[x, y] = noiseHeight;

                maxHeight = Mathf.Max(maxHeight, noiseHeight);
                minHeight = Mathf.Min(minHeight, noiseHeight);
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minHeight, maxHeight, noiseMap[x,y]);
            }
        }

        return noiseMap;
    }
}
