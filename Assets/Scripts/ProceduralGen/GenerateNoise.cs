using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GenerateNoise
{
    //Local calculates the noise height per chunk whereas global estimates through the entire terrain
    public enum NormalizeMode { Local, Global };

    public struct NoiseMapData{
        public int seed;
        public int width;
        public int height;
        public int octaves;
        public float scale;
        public float persistance;
        public float frequency;
        public Vector2 offset;
        public NormalizeMode normalizeMode;
    }

    public static float[,] GenerateNoiseMap(NoiseMapData noiseData)
    {
        int width = noiseData.width;
        int height = noiseData.height; 
        int octaves = noiseData.octaves;
        int seed = noiseData.seed;
        float scale = noiseData.scale;
        float persistance = noiseData.persistance;
        float frequency = noiseData.frequency;
        Vector2 offset = noiseData.offset;
        NormalizeMode normalizeMode = noiseData.normalizeMode;

        float[,] noiseMap = new float[width, height];
        float maxLocalHeight = float.MinValue;
        float minLocalHeight = float.MaxValue;

        float halfX = width / 2;
        float halfY = height / 2;

        float maxPossibleHeight = 0;
        float freq = 1;
        float amp = 1;

        System.Random random = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = random.Next(-100000, 100000) + offset.x;
            float offsetY = random.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amp;
            amp *= persistance;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                freq = 1;
                amp = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfX + octaveOffsets[i].x) / Mathf.Max(.0001f, scale) * freq;
                    float sampleY = (y - halfY + octaveOffsets[i].y) / Mathf.Max(.0001f, scale) * freq;

                    float perlin = Mathf.PerlinNoise(sampleX, sampleY);
                    noiseHeight += perlin * amp;

                    amp *= persistance;
                    freq *= frequency;
                }
                noiseMap[x, y] = noiseHeight;

                maxLocalHeight = Mathf.Max(maxLocalHeight, noiseHeight);
                minLocalHeight = Mathf.Min(minLocalHeight, noiseHeight);
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (normalizeMode == NormalizeMode.Local)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalHeight, maxLocalHeight, noiseMap[x, y]);
                }
                else
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight / 1.5f);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }

        return noiseMap;
    }
}
