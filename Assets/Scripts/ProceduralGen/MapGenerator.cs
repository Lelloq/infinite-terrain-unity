using System;
using System.Collections.Concurrent;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    { NoiseMap, Mesh, FallOffMap }
    public DrawMode drawMode;

    public TerrainData terrainData;
    public NoiseData noiseData;
    public TextureData textureData;

    public Material terrainMaterial;

    //+2 -2 to compensate for the borders
    const int COMPENSATION = 2;

    [Range(0, 6)]
    public int levelOfDetailPreview;

    public bool autoUpdate = false;

    float[,] falloffMap;

    private readonly ConcurrentQueue<MapThreadInfo<MapData>> mapThreadInfoQueue = new();
    private readonly ConcurrentQueue<MapThreadInfo<MeshData>> meshThreadInfoQueue = new();

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapEditor();
        }
    }

    void OnTexutureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    public int MapChunkSize
    {
        get
        {
            if (terrainData.UseFlatShading)
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
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGen.GenerateTerrainMesh(data.HeightMap,
                terrainData.HeightMultiplier, terrainData.MeshHeightCurve, levelOfDetailPreview, terrainData.UseFlatShading));
        }
        else if (drawMode == DrawMode.FallOffMap)
        {
            display.DrawTexture(TextureGen.TextureFromHeightMap(FallOffMap.GenerateFalloffMap(MapChunkSize)));
        }
    }

    private void OnValidate()
    {
        //I think theres a better way of doing this but ill come to it after refactoring all of this
        if (terrainData != null)
        {
            terrainData.OnValuesUpdated -= OnValuesUpdated;
            terrainData.OnValuesUpdated += OnValuesUpdated;
        }
        if (noiseData != null)
        {
            noiseData.OnValuesUpdated -= OnValuesUpdated;
            noiseData.OnValuesUpdated += OnValuesUpdated;
        }
        if (textureData != null)
        {
            textureData.OnValuesUpdated -= OnTexutureValuesUpdated;
            textureData.OnValuesUpdated += OnTexutureValuesUpdated;
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
        int compensatedChunkSize = MapChunkSize + COMPENSATION;

        GenerateNoise.NoiseMapData noiseDataObj = new()
        {
            width = compensatedChunkSize,
            height = compensatedChunkSize,
            seed = noiseData.Seed,
            scale = noiseData.NoiseScale,
            octaves = noiseData.Octaves,
            persistance = noiseData.Persistance,
            frequency = noiseData.Frequency,
            offset = centre + noiseData.Offset,
            normalizeMode = noiseData.NormalizeMode
        };

        float[,] noiseMap = GenerateNoise.GenerateNoiseMap(noiseDataObj);

        if (terrainData.UseFalloffMap)
        {
            if (falloffMap == null)
            {
                falloffMap = FallOffMap.GenerateFalloffMap(compensatedChunkSize);
            }

            for (int y = 0; y < compensatedChunkSize; y++)
            {
                for (int x = 0; x < compensatedChunkSize; x++)
                {
                    if (terrainData.UseFalloffMap)
                    {
                        noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                    }
                }
            }
        }


        return new MapData(noiseMap);
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

public struct MapData
{
    public readonly float[,] HeightMap;

    public MapData(float[,] heightMap)
    {
        HeightMap = heightMap;
    }
}