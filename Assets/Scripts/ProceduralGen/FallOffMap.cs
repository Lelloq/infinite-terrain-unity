using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallOffMap : MonoBehaviour
{
    public static float[,] GenerateFalloffMap(int size) 
    {
        float[,] map = new float[size,size];

        for(int i = 0; i < size; i++) 
        {
            for(int j = 0; j < size; j++) 
            {
                float x = i / (float)size * 2 - 1;
                float y = j / (float)size * 2 - 1;

                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                //map[i,j] = RadialEvaluate(value, 100, i, j, size / 2f, size / 2f);
                map[i,j] = Evaluate(value);
            }
        }

        return map;
    }

    static float Evaluate(float value) 
    {
        float a = 3;
        float b = 2.2f;

        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b-b*value, a));
    }

    static float RadialEvaluate(float value, float radius, int x, int y, float cx, float cy) 
    {
        float dx = cx - x;
        float dy = cy - y;
        float distSqr = dx * dx + dy * dy;
        float radSqr = radius * radius;

        if (distSqr > radSqr) return 1f;
        return value;
    }
}
