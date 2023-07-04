using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;
using System;
using ContextualAmbientOcclusion.Runtime;

public class StringPrinter : ScriptableObject
{
    public static string PrintAntiAliasingSamples() 
    {
        System.Random random = new System.Random(1);

        StringBuilder sb = new StringBuilder();

        // Generate 32 random points
        for (int i = 0; i < 32; i++)
        {
            Vector3 v = new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());

            v *= 2f;
            v -= new Vector3(1f, 1f, 1f);

            sb.Append($"float3({v.x}, {v.y}, {v.z}), \n");
        }

        return sb.ToString();
    }

    public static string PrintAntiAliasingSamplesSinc()
    {
        System.Random random = new System.Random(1);
        StringBuilder sb = new StringBuilder();

        Plane[] cubeFaces = new Plane[] {
            new Plane(new Vector3(1, 0, 0), 1),
            new Plane(new Vector3(-1, 0, 0), 1),
            new Plane(new Vector3(0, 1, 0), 1),
            new Plane(new Vector3(0, -1, 0), 1),
            new Plane(new Vector3(0, 0, 1), 1),
            new Plane(new Vector3(0, 0, -1), 1),
        };

        // Generate 32 random points
        for (int i = 0; i < 32; i++)
        {
            Vector4 v = new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());

            v *= 2f;
            v -= new Vector4(1f, 1f, 1f);
            v.w = 0;
            //v.w = 1 - (v.magnitude / Mathf.Sqrt(3)) * (v.magnitude / Mathf.Sqrt(3));

            float distanceToCube = cubeFaces.Select(p => {
                Ray r = new Ray(Vector3.zero, v);
                float enter;
                p.Raycast(r, out enter);
                return enter;
            }).Where(d => d > 0).Min();

            v.w = MathHelper.Sinc(v.magnitude / distanceToCube);

            sb.Append($"float4({v.x}, {v.y}, {v.z}, {v.w}), \n");
        }

        return sb.ToString();
    }

    public static string PrintRayDirections()
    {
        System.Random random = new System.Random(1);

        StringBuilder sb = new StringBuilder();

        Func<Vector3, float> manhattenDistance = v => Mathf.Abs(v.x) + Mathf.Abs(v.y) + Mathf.Abs(v.z);



        List<Vector3>[] points = new List<Vector3>[4];

        for (int i = 0; i < points.Length; i++)
        {
            points[i] = new List<Vector3>();
        }

        //// Generate random LAO ray directions
        //Vector3[] points = new Vector3[] {
        //    new Vector3(0, 0, 1), new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, -1), new Vector3(0, -1, 0), new Vector3(-1, 0, 0),
        //    new Vector3(1, 1, 1), new Vector3(1, 1, -1), new Vector3(1, -1, 1), new Vector3(1, -1, -1), new Vector3(-1, 1, 1), new Vector3(-1, 1, -1), new Vector3(-1, -1, 1), new Vector3(-1, -1, -1), };

        // Manhatten distance 1-2-3
        for (int m = 0; m < points.Length; m++)
        {
            new Vector3[3, 3, 3].Iterate3D((x, y, z, i) => {
                if (x != 1 || y != 1 || z != 1)
                {
                    Vector3 point = new Vector3(x, y, z) - Vector3.one;
                    float md = manhattenDistance(point);
                    bool shouldAdd;

                    switch (m)
                    {
                        case 0:
                            shouldAdd = (md == 1.0f);
                            break;
                        case 1:
                            shouldAdd = (md == 1.0f || md == 3.0f);
                            break;
                        case 2:
                            shouldAdd = true;
                            break;
                        default:
                            shouldAdd = false;
                            break;
                    }

                    if (shouldAdd)
                    {
                        points[m].Add(point);
                    }
                }
            });
        }

        // Rubik's cube
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                points[3].Add(new Vector3(0, 0, 1.5f) + new Vector3(i, j, 0));
                points[3].Add(new Vector3(0, 0, -1.5f) + new Vector3(i, j, 0));
                points[3].Add(new Vector3(0, 1.5f, 0) + new Vector3(i, 0, j));
                points[3].Add(new Vector3(0, -1.5f, 0) + new Vector3(i, 0, j));
                points[3].Add(new Vector3(1.5f, 0, 0) + new Vector3(0, i, j));
                points[3].Add(new Vector3(-1.5f, 0, 0) + new Vector3(0, i, j));
            }
        }

        Debug.Log("Count points[3]: " + points[3].Count);

        for (int m = 0; m < points.Length; m++)
        {
            sb.AppendLine($"#if KEYWORD_{m}");
            sb.AppendLine($"#define LAO_RAYS_COUNT {+ points[m].Count}");
            sb.AppendLine($"static const float3 SphereVectors[{+points[m].Count}] = {{");

            for (int i = 0; i < points[m].Count; i++)
            {
                Vector3 randomPoint = new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
                randomPoint -= new Vector3(0.5f, 0.5f, 0.5f);
                randomPoint *= 0.05f;

                Vector3 v = points[m][i] + randomPoint;

                v.Normalize();

                sb.AppendLine($"\tfloat3({v.x}, {v.y}, {v.z}),");
            }
            sb.AppendLine("};");
            sb.AppendLine("#endif");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
