using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer textureRenderer;
    
    public void DrawNoiseMap(float[,] noiseMap)
    {
        int width = noiseMap.GetLength(0);
        int height = noiseMap.GetLength(1);

        Texture2D tex = new Texture2D(width, height);

        Color[] colors = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colors[y * width + x] = Color.Lerp(Color.black, Color.white, noiseMap[x,y]);
            }
        }
        tex.SetPixels(colors);
        tex.Apply();

        textureRenderer.sharedMaterial.mainTexture = tex;
        textureRenderer.transform.localScale = new Vector3(width, 1, height);
    }
}
