using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolumeBoundariesMeshTool : ScriptableObject
{
    public static Mesh Create()
    {
        Mesh volumeBoundariesMesh = new Mesh();

        volumeBoundariesMesh.vertices = new Vector3[]{
                ScaledVec3(0,0,0),
                ScaledVec3(1,0,0),
                ScaledVec3(0,1,0),
                ScaledVec3(1,1,0),
                ScaledVec3(0,0,1),
                ScaledVec3(1,0,1),
                ScaledVec3(0,1,1),
                ScaledVec3(1,1,1)
            };

        volumeBoundariesMesh.triangles = new int[]{
                2, 1, 0,
                1, 2, 3,
                4, 6, 0,
                2, 0, 6,
                1, 7, 5,
                7, 1, 3,
                6, 4, 7,
                4, 5, 7,
                2, 6, 3,
                6, 7, 3,
                4, 0, 1,
                1, 5, 4
            };

        volumeBoundariesMesh.colors = new Color[] {
                new Color(0,0,0),
                new Color(1,0,0),
                new Color(0,1,0),
                new Color(1,1,0),
                new Color(0,0,1),
                new Color(1,0,1),
                new Color(0,1,1),
                new Color(1,1,1)
            };

        volumeBoundariesMesh.RecalculateNormals();
        volumeBoundariesMesh.RecalculateBounds();
        volumeBoundariesMesh.RecalculateTangents();

        //AssetDatabase.CreateAsset(volumeBoundariesMesh, "Assets/Volume Boundaries Mesh.mesh");

        //AssetDatabase.SaveAssets();
        //AssetDatabase.Refresh();

        return volumeBoundariesMesh;
    }

    private static Vector3 ScaledVec3(float r, float g, float b)
    {
        return new Vector3(r, g, b) - new Vector3(0.5f, 0.5f, 0.5f);
    }
}
