using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    { NoiseMap, ColourMap, Mesh, FallOffMap }

    public DrawMode drawMode;

    public GenerateNoise.NormalizeMode NormalizeMode;

    /*Mesh has a limit of 65k triangles so this is the highest chunk size it can go whilst staying
    Within LODs*/
    public const int MAP_CHUNK_SIZE = 241;

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

    float[,] falloffMap;

    private readonly ConcurrentQueue<MapThreadInfo<MapData>> mapThreadInfoQueue = new();
    private readonly ConcurrentQueue<MapThreadInfo<MeshData>> meshThreadInfoQueue = new();

    private void Awake()
    {
        falloffMap = FallOffMap.GenerateFalloffMap(MAP_CHUNK_SIZE);
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
            display.DrawTexture(TextureGen.TextureFromColourMap(data.ColourMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGen.GenerateTerrainMesh(data.HeightMap, HeightMultiplier, MeshHeightCurve, LevelOfDetailPreview), TextureGen.TextureFromColourMap(data.ColourMap, MAP_CHUNK_SIZE, MAP_CHUNK_SIZE));
        }
        else if (drawMode == DrawMode.FallOffMap)
        {
            display.DrawTexture(TextureGen.TextureFromHeightMap(FallOffMap.GenerateFalloffMap(MAP_CHUNK_SIZE)));
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
        MeshData meshData = MeshGen.GenerateTerrainMesh(mapData.HeightMap, HeightMultiplier, MeshHeightCurve, lod);
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
            width = MAP_CHUNK_SIZE,
            height = MAP_CHUNK_SIZE,
            seed = Seed,
            scale = NoiseScale,
            octaves = Octaves,
            persistance = Persistance,
            frequency = Frequency,
            offset = centre + Offset,
            normalizeMode = NormalizeMode
        };

        float[,] noiseMap = GenerateNoise.GenerateNoiseMap(noiseData);
        Color[] colourMap = new Color[MAP_CHUNK_SIZE * MAP_CHUNK_SIZE];

        for (int y = 0; y < MAP_CHUNK_SIZE; y++)
        {
            for (int x = 0; x < MAP_CHUNK_SIZE; x++)
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
                        colourMap[y * MAP_CHUNK_SIZE + x] = region.Colour;
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
        falloffMap = FallOffMap.GenerateFalloffMap(MAP_CHUNK_SIZE);
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