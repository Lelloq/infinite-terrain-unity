using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class TerrainData : UpdatableData
{
    public float UniformScale = 1f;

    public float HeightMultiplier;
    public AnimationCurve MeshHeightCurve;
    public bool UseFalloffMap = false;

    /*Mesh has a limit of 65k triangles so this is the highest chunk size it can go whilst staying
     * Within LODs*/
    public bool UseFlatShading;
}
