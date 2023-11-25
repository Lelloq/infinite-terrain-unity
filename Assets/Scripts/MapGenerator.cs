using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{ 
    public enum DrawMode { NoiseMap, ColourMap, Mesh }
    public DrawMode drawMode;

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

    public TerrainType[] regions;

    public void Start()
    {
        GenerateMap();
    }

    public void GenerateMap() 
    {
        float[,] noiseMap = GenerateNoise.GenerateNoiseMap(MapWidth, MapHeight, NoiseScale, Seed, Octaves, Persistance, Frequency, Offset);

        Color[] colourMap = new Color[MapWidth * MapHeight];
        for(int y = 0; y < MapHeight; y++) 
        {
            for(int x = 0; x < MapWidth; x++) 
            {
                float curHeight = noiseMap[x, y];
                foreach(TerrainType region in regions)
                {
                    if(curHeight <= region.Height) 
                    {
                        colourMap[y * MapWidth + x] = region.Colour;
                        break;
                    }
                }
            }
        }

        MapDisplay display = FindObjectOfType<MapDisplay>();
        if(drawMode == DrawMode.NoiseMap) 
        {
            display.DrawTexture(TextureGen.TextureFromHeightMap(noiseMap));
        }
        else if(drawMode == DrawMode.ColourMap)
        {
            display.DrawTexture(TextureGen.TextureFromColourMap(colourMap, MapWidth, MapHeight));
        }
        else if(drawMode == DrawMode.Mesh) 
        {
            display.DrawMesh(MeshGen.GenerateTerrainMesh(noiseMap), TextureGen.TextureFromColourMap(colourMap, MapWidth, MapHeight));
        }
    }

    private void OnValidate()
    {
        MapWidth = Mathf.Clamp(MapWidth, 1, int.MaxValue);
        MapHeight = Mathf.Clamp(MapHeight, 1, int.MaxValue);
        Octaves = Mathf.Clamp(Octaves, 1, int.MaxValue);
        Frequency = Mathf.Clamp(Frequency, 1, float.MaxValue);
    }
}

[System.Serializable]
public struct TerrainType 
{
    public string Name;
    public float Height;
    public Color Colour;
}