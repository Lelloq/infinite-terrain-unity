using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour
{
    public const float MAXVIEWDIST = 300;
    public Transform Viewer;

    public static Vector2 ViewPos;
    int chunkSize;
    int chunksVisibleInView;

    Dictionary<Vector2, TerrainChunk> chunks = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    private void Start()
    {
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
                    chunks.Add(viewChunkCoord, new TerrainChunk(viewChunkCoord, chunkSize));
                }
            }
        }
    }

    public class TerrainChunk 
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        public TerrainChunk(Vector2 coords, int size) 
        {
            position = coords * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 position3D = new Vector3(position.x,0,position.y);

            meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObject.transform.position = position3D;
            meshObject.transform.localScale = Vector3.one * size / 10f;

            SetVisible(false);
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
