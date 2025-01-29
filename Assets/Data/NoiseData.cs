using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class NoiseData : UpdatableData
{
    public GenerateNoise.NormalizeMode NormalizeMode;

    public float NoiseScale;

    public int Octaves;

    [Range(0f, 1f)]
    public float Persistance;
    public float Frequency;

    public int Seed;
    public Vector2 Offset;

    protected override void OnValidate()
    {
        Octaves = Mathf.Clamp(Octaves, 1, int.MaxValue);
        Frequency = Mathf.Clamp(Frequency, 1, float.MaxValue);

        base.OnValidate();
    }
}
