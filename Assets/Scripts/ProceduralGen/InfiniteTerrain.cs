using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour
{
    public const float MAXVIEWDIST = 300;
    public Transform Viewer;
    public Material MapMaterial;

    public static Vector2 ViewPos;
    static MapGenerator mapGenerator;

    int chunkSize;
    int chunksVisibleInView;

    Dictionary<Vector2, TerrainChunk> chunks = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    private void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        chunkSize = MapGenerator.MAPCHUNKSIZE - 1;
        chunksVisibleInView = Mathf.RoundToInt(MAXVIEWDIST / chunkSize);
    }

    private void Update()
    {
        ViewPos = new Vector2(Viewer.position.x, Viewer.position.z);
        UpdateVisibleChunks();
    }

    private void UpdateVisibleChunks()
    {
        foreach(TerrainChunk chunk in terrainChunksVisibleLastUpdate) 
        {
            chunk.SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int curChunkCoordX = Mathf.RoundToInt(ViewPos.x / chunkSize);
        int curChunkCoordY = Mathf.RoundToInt(ViewPos.y / chunkSize);

        for(int yOffset = -chunksVisibleInView; yOffset <= chunksVisibleInView; yOffset++) 
        {
            for (int xOffset = -chunksVisibleInView; xOffset <= chunksVisibleInView; xOffset++)
            {
                Vector2 viewChunkCoord = new Vector2(curChunkCoordX + xOffset, curChunkCoordY + yOffset);

                if (chunks.ContainsKey(viewChunkCoord)) 
                {
                    chunks[viewChunkCoord].UpdateChunk();

                    if (chunks[viewChunkCoord].IsVisible()) 
                    {
                        terrainChunksVisibleLastUpdate.Add(chunks[viewChunkCoord]);
                    }
                }
                else 
                {
                    chunks.Add(viewChunkCoord, new TerrainChunk(viewChunkCoord, chunkSize, transform, MapMaterial));
                }
            }
        }
    }

    public class TerrainChunk 
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        public TerrainChunk(Vector2 coords, int size, Transform parent, Material material) 
        {
            position = coords * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 position3D = new Vector3(position.x,0,position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;

            meshObject.transform.position = position3D;
            meshObject.transform.parent = parent;
            SetVisible(false);

            mapGenerator.RequestMapData(OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData) 
        {
            mapGenerator.RequestMeshData(mapData, OnMeshDataReceived);
        }

        void OnMeshDataReceived(MeshData meshData) 
        {
            meshFilter.mesh = meshData.CreateMesh();
        }

        public void UpdateChunk() 
        {
            float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(ViewPos));
            bool visible = viewerDistanceFromNearestEdge <= MAXVIEWDIST;
            SetVisible(visible);
        }

        public void SetVisible(bool visible) 
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible() 
        {
            return meshObject.activeSelf;
        }
    }
}
