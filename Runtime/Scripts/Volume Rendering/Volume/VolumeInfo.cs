using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ContextualAmbientOcclusion.Runtime
{
    public class VolumeInfo
    {
        public int voxelCount;
        public Vector3Int dimensions;
        public Vector3 spacing;
        public Vector3 origin;
        public float min;
        public float max;
    }
}