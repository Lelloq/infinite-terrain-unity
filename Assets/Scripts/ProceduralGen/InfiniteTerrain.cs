using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour
{
    const float SCALE = 1f;

    const float VIEWER_CHUNKUP_DATE_RATE = 16f;
    const float VIEWER_CHUNK_UPDATE_RATE_SQUARE = VIEWER_CHUNKUP_DATE_RATE * VIEWER_CHUNKUP_DATE_RATE;

    public Transform Viewer;
    public Material MapMaterial;

    public LODInfo[] LodLevels;
    static float maxViewDist;

    static Vector2 viewPos;
    Vector2 viewPosOld;
    static MapGenerator mapGenerator;

    int chunkSize;
    int chunksVisibleInView;

    readonly Dictionary<Vector2, TerrainChunk> chunks = new();
    static readonly List<TerrainChunk> terrainChunksVisibleLastUpdate = new();

    private void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        maxViewDist = LodLevels.Last().VisibleDistThreshold;
        chunkSize = MapGenerator.MAP_CHUNK_SIZE - 1;
        chunksVisibleInView = Mathf.RoundToInt(maxViewDist / chunkSize);

        UpdateVisibleChunks();
    }

    private void Update()
    {
        viewPos = new Vector2(Viewer.position.x, Viewer.position.z) / SCALE;

        if ((viewPosOld - viewPos).sqrMagnitude > VIEWER_CHUNK_UPDATE_RATE_SQUARE)
        {
            viewPosOld = viewPos;
            UpdateVisibleChunks();
        }
    }

    private void UpdateVisibleChunks()
    {
        foreach (TerrainChunk chunk in terrainChunksVisibleLastUpdate)
        {
            chunk.SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        int curChunkCoordX = Mathf.RoundToInt(viewPos.x / chunkSize);
        int curChunkCoordY = Mathf.RoundToInt(viewPos.y / chunkSize);

        for (int yOffset = -chunksVisibleInView; yOffset <= chunksVisibleInView; yOffset++)
        {
            for (int xOffset = -chunksVisibleInView; xOffset <= chunksVisibleInView; xOffset++)
            {
                Vector2 viewChunkCoord = new(curChunkCoordX + xOffset, curChunkCoordY + yOffset);

                if (chunks.ContainsKey(viewChunkCoord))
                {
                    chunks[viewChunkCoord].UpdateChunk();
                }
                else
                {
                    chunks.Add(viewChunkCoord, new TerrainChunk(viewChunkCoord, chunkSize, LodLevels, transform, MapMaterial));
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
        MeshCollider meshCollider;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        LODMesh collisionLODMesh;

        MapData mapData;

        int previousLODIndex = -1;
        bool mapDataReceived;

        public TerrainChunk(Vector2 coords, int size, LODInfo[] detaillevels, Transform parent, Material material)
        {
            position = coords * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 position3D = new(position.x, 0, position.y);
            detailLevels = detaillevels;

            meshObject = new GameObject("Terrain Chunk");
            meshObject.layer = 3;
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;

            meshObject.transform.position = position3D * SCALE;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * SCALE;
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].Lod, UpdateChunk);
                if (detailLevels[i].useForCollider)
                {
                    collisionLODMesh = lodMeshes[i];
                }
            }

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapdata)
        {
            mapData = mapdata;
            mapDataReceived = true;

            int chunkSize = MapGenerator.MAP_CHUNK_SIZE;

            Texture2D texture = TextureGen.TextureFromColourMap(mapdata.ColourMap, chunkSize, chunkSize);
            meshRenderer.material.mainTexture = texture;

            UpdateChunk();
        }

        public void UpdateChunk()
        {
            if (!mapDataReceived) return;

            float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewPos));
            bool visible = viewerDistanceFromNearestEdge <= maxViewDist;

            if (visible)
            {
                int lodIndex = 0;

                for (int i = 0; i < detailLevels.Length - 1; i++)
                {
                    if (viewerDistanceFromNearestEdge > detailLevels[i].VisibleDistThreshold)
                    {
                        lodIndex = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }

                if (lodIndex != previousLODIndex)
                {
                    LODMesh lodMesh = lodMeshes[lodIndex];
                    if (lodMesh.HasMesh)
                    {
                        previousLODIndex = lodIndex;
                        meshFilter.mesh = lodMesh.Mesh;
                    }
                    else if (!lodMesh.HasRequestedMesh)
                    {
                        lodMesh.RequestMesh(mapData);
                    }
                }

                if (lodIndex == 0)
                {
                    if (collisionLODMesh.HasMesh)
                    {
                        meshCollider.sharedMesh = collisionLODMesh.Mesh;
                    }
                    else if (!collisionLODMesh.HasRequestedMesh)
                    {
                        collisionLODMesh.RequestMesh(mapData);
                    }
                }

                terrainChunksVisibleLastUpdate.Add(this);
            }

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

    class LODMesh
    {
        public Mesh Mesh;
        public bool HasRequestedMesh;
        public bool HasMesh;
        int LodLevel;

        Action updateCallback;

        public LODMesh(int lod, Action callback)
        {
            LodLevel = lod;
            updateCallback = callback;
        }

        void OnMeshDataReceived(MeshData meshdata)
        {
            Mesh = meshdata.CreateMesh();
            HasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapdata)
        {
            HasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapdata, LodLevel, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int Lod;
        public float VisibleDistThreshold;
        public bool useForCollider;
    }
}
