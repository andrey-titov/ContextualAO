using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;
using System;
using ContextualAmbientOcclusion.Runtime;

public class RayPatternPrinter : ScriptableObject
{
    private const int LAO_RAYS_COUNT = 512;

    public static string Print(int seed)
    {
        System.Random random = new System.Random(seed);
        List<Vector3>[] points = GenerateRayPoints(random);
        return JitterAndPrint(random, points);
    }

    static List<Vector3>[] GenerateRayPoints(System.Random random)
    {
        Func<Vector3, float> manhattenDistance = v => Mathf.Abs(v.x) + Mathf.Abs(v.y) + Mathf.Abs(v.z);

        List<Vector3>[] points = new List<Vector3>[5];

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

        Vector3 sum = Vector3.zero;

        points[4] = FibonacciSampleSphere();

        //// Demosntration of sphere
        //{ 
        //    try
        //    {
        //        DestroyImmediate(GameObject.Find("Sphere Center Clone"));
        //    }
        //    catch (Exception)
        //    {
        //        throw;
        //    }

        //    GameObject sphereCeneter = Instantiate(GameObject.Find("Sphere Center"));
        //    sphereCeneter.name = "Sphere Center Clone";

        //    GameObject sphere = GameObject.Find("Sphere");

        //    foreach (Vector3 p in points[4])
        //    {
        //        GameObject s = Instantiate(sphere, sphereCeneter.transform);
        //        s.transform.localPosition = p;
        //    }
        //}

        return points;
    }

    static List<Vector3> RandomSampleSphere(System.Random random)
    {
        List<Vector3> points = new List<Vector3>();

        // Random sphere coorindates
        for (int i = 0; i < LAO_RAYS_COUNT; i++)
        {
            float sphereZ = ((float)random.NextDouble() * 2f) - 1f;
            float sphereLongitude = (float)random.NextDouble() * Mathf.PI * 2f;

            Vector2 rotation2d = new Vector2(Mathf.Cos(sphereLongitude), Mathf.Sin(sphereLongitude));
            float norm2d = Mathf.Sqrt(1f - sphereZ * sphereZ);

            Vector2 rotation = rotation2d * norm2d;

            Vector3 spherePoint = new Vector3(rotation.x, rotation.y, sphereZ);
            points.Add(spherePoint);
        }

        return points;
    }

    static List<Vector3> FibonacciSampleSphere()
    {
        List<Vector3> points = new List<Vector3>();

        // Points generated using the Fibonacci Lattice
        for (int i = 0; i < LAO_RAYS_COUNT; i++)
        {
            float k = i + .5f;

            float phi = Mathf.Acos(1f - 2f * k / LAO_RAYS_COUNT);
            float theta = Mathf.PI * (1 + Mathf.Sqrt(5)) * k;

            float x = Mathf.Cos(theta) * Mathf.Sin(phi);
            float y = Mathf.Sin(theta) * Mathf.Sin(phi);
            float z = Mathf.Cos(phi);

            Vector3 spherePoint = new Vector3(x, y, z);
            points.Add(spherePoint);

            //GameObject s = Instantiate(sphere, sphereCeneter.transform);
            //s.transform.localPosition = spherePoint;
        }

        return points;
    }

    static string JitterAndPrint(System.Random random, List<Vector3>[] points)
    {
        StringBuilder sb = new StringBuilder();

        for (int m = 0; m < points.Length; m++)
        {
            if (m == 0)
            {
                sb.AppendLine($"#if KEYWORD_{m}");
            }
            else
            {
                sb.AppendLine($"#elif KEYWORD_{m}");
            }
            
            sb.AppendLine($"#define LAO_RAYS_COUNT {+points[m].Count}");
            sb.AppendLine($"static const float3 SphereVectors[{+points[m].Count}] = {{");

            for (int i = 0; i < points[m].Count; i++)
            {
                Vector3 randomPoint = Vector3.zero;

                if (m <= 3)
                {
                    randomPoint = new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
                    randomPoint -= new Vector3(0.5f, 0.5f, 0.5f);
                    randomPoint *= 0.05f;
                }

                Vector3 v = points[m][i] + randomPoint;

                v.Normalize();

                sb.AppendLine($"\tfloat3({v.x}, {v.y}, {v.z}),");
            }
            sb.AppendLine("};");

            if (m == points.Length - 1)
            {
                sb.AppendLine("#endif");
            }
        }

        return sb.ToString();
    }
}
