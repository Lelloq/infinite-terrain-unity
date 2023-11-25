using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{ 
    public int MapWidth;
    public int MapHeight;
    public float NoiseScale;

    public int Octaves;
    [Range(0f, 1f)]
    public float Persistance;
    public float Frequency;

    public int Seed;
    public Vector2 Offset;

    public bool AutoUpdate = false;

    public void GenerateMap() 
    {
        float[,] noiseMap = GenerateNoise.GenerateNoiseMap(MapWidth, MapHeight, NoiseScale, Seed, Octaves, Persistance, Frequency, Offset);

        MapDisplay display = FindObjectOfType<MapDisplay>();
        display.DrawNoiseMap(noiseMap);
    }

    private void OnValidate()
    {
        MapWidth = Mathf.Clamp(MapWidth, 1, int.MaxValue);
        MapHeight = Mathf.Clamp(MapHeight, 1, int.MaxValue);
        Octaves = Mathf.Clamp(Octaves, 1, int.MaxValue);
        Frequency = Mathf.Clamp(Frequency, 1, float.MaxValue);
    }
}
