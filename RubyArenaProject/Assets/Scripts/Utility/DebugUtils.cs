using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Utility
{
    public static class DebugUtils
    {
    public static void DrawBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, Color color, float duration = 2f)
    {
            // All 8 local corners of the box
            Vector3[] localCorners = new Vector3[8]
            {
        new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z),
        new Vector3( halfExtents.x, -halfExtents.y, -halfExtents.z),
        new Vector3( halfExtents.x, -halfExtents.y,  halfExtents.z),
        new Vector3(-halfExtents.x, -halfExtents.y,  halfExtents.z),

        new Vector3(-halfExtents.x,  halfExtents.y, -halfExtents.z),
        new Vector3( halfExtents.x,  halfExtents.y, -halfExtents.z),
        new Vector3( halfExtents.x,  halfExtents.y,  halfExtents.z),
        new Vector3(-halfExtents.x,  halfExtents.y,  halfExtents.z)
            };

            // Transform corners into world space
            for (int i = 0; i < localCorners.Length; i++)
            {
                localCorners[i] = center + orientation * localCorners[i];
            }

            // Bottom square
            Debug.DrawLine(localCorners[0], localCorners[1], color, duration);
            Debug.DrawLine(localCorners[1], localCorners[2], color, duration);
            Debug.DrawLine(localCorners[2], localCorners[3], color, duration);
            Debug.DrawLine(localCorners[3], localCorners[0], color, duration);

            // Top square
            Debug.DrawLine(localCorners[4], localCorners[5], color, duration);
            Debug.DrawLine(localCorners[5], localCorners[6], color, duration);
            Debug.DrawLine(localCorners[6], localCorners[7], color, duration);
            Debug.DrawLine(localCorners[7], localCorners[4], color, duration);

            // Vertical lines
            Debug.DrawLine(localCorners[0], localCorners[4], color, duration);
            Debug.DrawLine(localCorners[1], localCorners[5], color, duration);
            Debug.DrawLine(localCorners[2], localCorners[6], color, duration);
            Debug.DrawLine(localCorners[3], localCorners[7], color, duration);
        }
    }
}
