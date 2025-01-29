using System;
using System.Collections.Concurrent;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    { NoiseMap, ColourMap, Mesh, FallOffMap }
    public DrawMode drawMode;

    public TerrainData terrainData;
    public NoiseData noiseData;

    //+2 -2 to compensate for the borders
    const int COMPENSATION = 2;

    [Range(0, 6)]
    public int levelOfDetailPreview;

    public bool autoUpdate = false;

    public TerrainType[] regions;
    static MapGenerator instance;

    float[,] falloffMap;

    private readonly ConcurrentQueue<MapThreadInfo<MapData>> mapThreadInfoQueue = new();
    private readonly ConcurrentQueue<MapThreadInfo<MeshData>> meshThreadInfoQueue = new();

    private void Awake()
    {
        falloffMap = FallOffMap.GenerateFalloffMap(MapChunkSize);
    }

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapEditor();
        }
    }

    public static int MapChunkSize
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<MapGenerator>();
            }

            if (instance.terrainData.UseFlatShading)
            {
                return 97 - COMPENSATION;
            }
            return 241 - COMPENSATION;
        }
    }

    public void Start()
    {
        DrawMapEditor();
    }

    #region EDITOR

    public void DrawMapEditor()
    {
        MapData data = GenerateMap(Vector2.zero);
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGen.TextureFromHeightMap(data.HeightMap));
        }
        else if (drawMode == DrawMode.ColourMap)
        {
            display.DrawTexture(TextureGen.TextureFromColourMap(data.ColourMap, MapChunkSize, MapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGen.GenerateTerrainMesh(data.HeightMap,
                terrainData.HeightMultiplier, terrainData.MeshHeightCurve, levelOfDetailPreview, terrainData.UseFlatShading),
                TextureGen.TextureFromColourMap(data.ColourMap, MapChunkSize, MapChunkSize));
        }
        else if (drawMode == DrawMode.FallOffMap)
        {
            display.DrawTexture(TextureGen.TextureFromHeightMap(FallOffMap.GenerateFalloffMap(MapChunkSize)));
        }
    }

    #endregion EDITOR

    public void RequestMapData(Vector2 centre, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(centre, callback);
        };

        new Thread(threadStart).Start();
    }

    private void MapDataThread(Vector2 centre, Action<MapData> callback)
    {
        MapData mapData = GenerateMap(centre);
        mapThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    private void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGen.GenerateTerrainMesh(mapData.HeightMap, terrainData.HeightMultiplier, 
            terrainData.MeshHeightCurve, lod, terrainData.UseFlatShading);
        meshThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
    }

    private void Update()
    {
        if (mapThreadInfoQueue.Count > 0)
        {
            mapThreadInfoQueue.TryDequeue(out MapThreadInfo<MapData> mapThreadInfo);
            mapThreadInfo.Callback(mapThreadInfo.Parameter);
        }

        if (meshThreadInfoQueue.Count > 0)
        {
            meshThreadInfoQueue.TryDequeue(out MapThreadInfo<MeshData> meshThreadInfo);
            meshThreadInfo.Callback(meshThreadInfo.Parameter);
        }
    }

    private MapData GenerateMap(Vector2 centre)
    {
        GenerateNoise.NoiseMapData noiseData = new()
        {
            width = MapChunkSize + COMPENSATION,
            height = MapChunkSize + COMPENSATION,
            seed = instance.noiseData.Seed,
            scale = instance.noiseData.NoiseScale,
            octaves = instance.noiseData.Octaves,
            persistance = instance.noiseData.Persistance,
            frequency = instance.noiseData.Frequency,
            offset = centre + instance.noiseData.Offset,
            normalizeMode = instance.noiseData.NormalizeMode
        };

        float[,] noiseMap = GenerateNoise.GenerateNoiseMap(noiseData);
        Color[] colourMap = new Color[MapChunkSize * MapChunkSize];

        for (int y = 0; y < MapChunkSize; y++)
        {
            for (int x = 0; x < MapChunkSize; x++)
            {
                if (instance.terrainData.UseFalloffMap)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }
                float curHeight = noiseMap[x, y];
                foreach (TerrainType region in regions)
                {
                    if (curHeight >= region.Height)
                    {
                        colourMap[y * MapChunkSize + x] = region.Colour;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colourMap);
    }

    private void OnValidate()
    {
        //I think theres a better way of doing this but ill come to it after refactoring all of this
        if(terrainData != null)
        {
            terrainData.OnValuesUpdated -= OnValuesUpdated;
            terrainData.OnValuesUpdated += OnValuesUpdated;
        }
        if(noiseData != null)
        {
            noiseData.OnValuesUpdated -= OnValuesUpdated;
            noiseData.OnValuesUpdated += OnValuesUpdated;
        }

        falloffMap = FallOffMap.GenerateFalloffMap(MapChunkSize);
    }

    private struct MapThreadInfo<T>
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