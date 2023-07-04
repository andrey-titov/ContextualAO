using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ContextualAmbientOcclusion.Runtime
{
    public static class ArrayHelper
    {
        private static void Iterate2D<T>(T[,,] array, int z, Action<int, int, int, int> func)
        {
            for (int y = 0; y < array.GetLength(1); y++)
            {
                for (int x = 0; x < array.GetLength(0); x++)
                {
                    int i = x + y * array.GetLength(0) + z * array.GetLength(0) * array.GetLength(1);
                    func(x, y, z, i);
                }
            }
        }

        //public static void Iterate3D<T>(this T[,,] array, Action<int, int, int> func)
        //{
        //    for (int z = 0; z < array.GetLength(2); z++)
        //    {
        //        for (int y = 0; y < array.GetLength(1); y++)
        //        {
        //            for (int x = 0; x < array.GetLength(0); x++)
        //            {
        //                func(x, y, z);
        //            }
        //        }
        //    }
        //}

        public static void Iterate3D<T>(this T[] array, Vector3Int dimensions, Action<int, int, int, int> func)
        {
            for (int z = 0; z < dimensions.z; z++)
            {
                for (int y = 0; y < dimensions.y; y++)
                {
                    for (int x = 0; x < dimensions.x; x++)
                    {
                        int i = x + y * dimensions.x + z * dimensions.x * dimensions.y;
                        func(x, y, z, i);
                    }
                }
            }
        }

        public static void Iterate3Dparallel<T>(this T[] array, Vector3Int dimensions, Action<int, int, int, int> func)
        {
            Parallel.For(0, dimensions.z, (z) =>
            {
                for (int y = 0; y < dimensions.y; y++)
                {
                    for (int x = 0; x < dimensions.x; x++)
                    {
                        int i = x + y * dimensions.x + z * dimensions.x * dimensions.y;
                        func(x, y, z, i);
                    }
                }
            });
        }

        public static void Iterate3D<T>(this T[,,] array, Action<int, int, int, int> func)
        {
            for (int z = 0; z < array.GetLength(2); z++)
            {
                Iterate2D(array, z, func);
            }
        }

        //public static void Iterate3Dparallel<T>(this T[,,] array, Action<int, int, int> func)
        //{
        //    Parallel.For(0, array.GetLength(2), (z) =>
        //    {
        //        for (int y = 0; y < array.GetLength(1); y++)
        //        {
        //            for (int x = 0; x < array.GetLength(0); x++)
        //            {
        //                func(x, y, z);
        //            }
        //        }
        //    });
        //}

        public static void Iterate3Dparallel<T>(this T[,,] array, Action<int, int, int, int> func)
        {
            Parallel.For(0, array.GetLength(2), (z) =>
            {
                Iterate2D(array, z, func);
            });
        }

        public static T Get<T>(this T[,,] array, int x, int y, int z) where T : new()
        {
            if (x >= 0 && y >= 0 && z >= 0
                && x < array.GetLength(0) && y < array.GetLength(1) && z < array.GetLength(2))
            {
                return array[x, y, z];
            }
            else
            {

                return new T();
            }
        }

        public static void Set<T>(this T[,,] array, int x, int y, int z, T value)
        {
            array[x, y, z] = value;
        }
    }
}