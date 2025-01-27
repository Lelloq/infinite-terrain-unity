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

    public GenerateNoise.NormalizeMode NormalizeMode;

    //+2 -2 to compensate for the borders
    const int COMPENSATION = 2;

    /*Mesh has a limit of 65k triangles so this is the highest chunk size it can go whilst staying
    Within LODs*/
    public bool UseFlatShading;

    [Range(0, 6)]
    public int LevelOfDetailPreview;

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

    public bool UseFalloffMap = false;

    public TerrainType[] regions;
    static MapGenerator instance;

    float[,] falloffMap;

    private readonly ConcurrentQueue<MapThreadInfo<MapData>> mapThreadInfoQueue = new();
    private readonly ConcurrentQueue<MapThreadInfo<MeshData>> meshThreadInfoQueue = new();

    private void Awake()
    {
        falloffMap = FallOffMap.GenerateFalloffMap(MapChunkSize);
    }

    public static int MapChunkSize
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<MapGenerator>();
            }

            if (instance.UseFlatShading)
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
            display.DrawMesh(MeshGen.GenerateTerrainMesh(data.HeightMap, HeightMultiplier, MeshHeightCurve, LevelOfDetailPreview, UseFlatShading),
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
        MeshData meshData = MeshGen.GenerateTerrainMesh(mapData.HeightMap, HeightMultiplier, MeshHeightCurve, lod, UseFlatShading);
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
            seed = Seed,
            scale = NoiseScale,
            octaves = Octaves,
            persistance = Persistance,
            frequency = Frequency,
            offset = centre + Offset,
            normalizeMode = NormalizeMode
        };

        float[,] noiseMap = GenerateNoise.GenerateNoiseMap(noiseData);
        Color[] colourMap = new Color[MapChunkSize * MapChunkSize];

        for (int y = 0; y < MapChunkSize; y++)
        {
            for (int x = 0; x < MapChunkSize; x++)
            {
                if (UseFalloffMap)
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
        Octaves = Mathf.Clamp(Octaves, 1, int.MaxValue);
        Frequency = Mathf.Clamp(Frequency, 1, float.MaxValue);
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