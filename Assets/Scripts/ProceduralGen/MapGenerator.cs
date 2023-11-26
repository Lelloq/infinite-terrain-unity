using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using System.Collections.Concurrent;

public class MapGenerator : MonoBehaviour
{ 
    public enum DrawMode { NoiseMap, ColourMap, Mesh }
    public DrawMode drawMode;

    /*Mesh has a limit of 65k triangles so this is the highest chunk size it can go whilst staying
    Within LODs*/
    public const int MAPCHUNKSIZE = 241;

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

    ConcurrentQueue<MapThreadInfo<MapData>> mapThreadInfoQueue = new ConcurrentQueue<MapThreadInfo<MapData>>();
    ConcurrentQueue<MapThreadInfo<MeshData>> meshThreadInfoQueue = new ConcurrentQueue<MapThreadInfo<MeshData>>();

    public void Start()
    {
        DrawMapEditor();
    }

    public void DrawMapEditor() 
    {
        MapData data = GenerateMap();
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGen.TextureFromHeightMap(data.HeightMap));
        }
        else if (drawMode == DrawMode.ColourMap)
        {
            display.DrawTexture(TextureGen.TextureFromColourMap(data.ColourMap, MAPCHUNKSIZE, MAPCHUNKSIZE));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGen.GenerateTerrainMesh(data.HeightMap, HeightMultiplier, MeshHeightCurve, LevelOfDetail), TextureGen.TextureFromColourMap(data.ColourMap, MAPCHUNKSIZE, MAPCHUNKSIZE));
        }
    }

    public void RequestMapData(Action<MapData> callback) 
    {
        ThreadStart threadStart = delegate { 
            MapDataThread(callback);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Action<MapData> callback) 
    {
        MapData mapData = GenerateMap();
        mapThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
    }

    public void RequestMeshData(MapData mapData, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate {
            MeshDataThread(mapData, callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, Action<MeshData> callback)
    {
        MeshData meshData = MeshGen.GenerateTerrainMesh(mapData.HeightMap, HeightMultiplier, MeshHeightCurve, LevelOfDetail);
        meshThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
    }

    private void Update()
    {
        if(mapThreadInfoQueue.Count > 0) 
        {
            for(int i = 0; i < mapThreadInfoQueue.Count; i++) 
            {
                mapThreadInfoQueue.TryDequeue(out MapThreadInfo<MapData> mapThreadInfo);
                mapThreadInfo.Callback(mapThreadInfo.Parameter);
            }
        }

        if(meshThreadInfoQueue.Count > 0) 
        {
            for (int i = 0; i < meshThreadInfoQueue.Count; i++)
            {
                meshThreadInfoQueue.TryDequeue(out MapThreadInfo<MeshData> meshThreadInfo);
                meshThreadInfo.Callback(meshThreadInfo.Parameter);
            }
        }
    }

    MapData GenerateMap() 
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

        return new MapData(noiseMap, colourMap);
    }

    private void OnValidate()
    {
        Octaves = Mathf.Clamp(Octaves, 1, int.MaxValue);
        Frequency = Mathf.Clamp(Frequency, 1, float.MaxValue);
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> Callback;
        public readonly T Parameter;

        public MapThreadInfo(Action<T> callback, T parameter) 
        {
            Callback = callback;
            Parameter = parameter;
        }
    }
}

[System.Serializable]
public struct TerrainType 
{
    public string Name;
    public float Height;
    public Color Colour;
}

public struct MapData 
{
    public readonly float[,] HeightMap;
    public readonly Color[] ColourMap;

    public MapData(float[,] heightMap, Color[] colourMap) 
    {
        HeightMap = heightMap;
        ColourMap = colourMap;
    }
}