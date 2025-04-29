using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Vector3Extenttion
{
    public static Vector3 ScaleVec (this Vector3 vec, Vector3 otherVec)
    {
        return new Vector3(vec.x * otherVec.x, vec.y * otherVec.y, vec.z * otherVec.z);
    }
}
