using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{ 
    public enum DrawMode { NoiseMap, ColourMap, Mesh }
    public DrawMode drawMode;

    /*Mesh has a limit of 65k triangles so this is the highest chunk size it can go whilst staying
    Within LODs*/
    const int MAPCHUNKSIZE = 241;

    [Range(0,6)]
    public int LevelOfDetail;

    public float NoiseScale;
    public float HeightMultiplier;

    public int Octaves;
    [Range(0f, 1f)]
    public float Persistance;
    public float Frequency;

    public int Seed;
    public Vector2 Offset;

    public AnimationCurve MeshHeightCurve;

    public bool AutoUpdate = false;

    public TerrainType[] regions;

    public void Start()
    {
        GenerateMap();
    }

    public void GenerateMap() 
    {
        float[,] noiseMap = GenerateNoise.GenerateNoiseMap(MAPCHUNKSIZE, MAPCHUNKSIZE, NoiseScale, Seed, Octaves, Persistance, Frequency, Offset);

        Color[] colourMap = new Color[MAPCHUNKSIZE * MAPCHUNKSIZE];
        for(int y = 0; y < MAPCHUNKSIZE; y++) 
        {
            for(int x = 0; x < MAPCHUNKSIZE; x++) 
            {
                float curHeight = noiseMap[x, y];
                foreach(TerrainType region in regions)
                {
                    if(curHeight <= region.Height) 
                    {
                        colourMap[y * MAPCHUNKSIZE + x] = region.Colour;
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
            display.DrawTexture(TextureGen.TextureFromColourMap(colourMap, MAPCHUNKSIZE, MAPCHUNKSIZE));
        }
        else if(drawMode == DrawMode.Mesh) 
        {
            display.DrawMesh(MeshGen.GenerateTerrainMesh(noiseMap, HeightMultiplier, MeshHeightCurve, LevelOfDetail), TextureGen.TextureFromColourMap(colourMap, MAPCHUNKSIZE, MAPCHUNKSIZE));
        }
    }

    private void OnValidate()
    {
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