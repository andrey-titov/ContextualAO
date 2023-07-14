using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ContextualAmbientOcclusion.Runtime
{
    public class VolumeCao : MonoBehaviour
    {
        public int lastFrameVoxelClipping { get; set; }
        public int lastFrameRayCastLAO { get; set; }
        public int lastFrameNormals { get; set; }

        public RenderTexture normals;
        public RenderTexture opacityOutput;
        public RenderTexture laoMask;
        public RenderTexture laoOutput;
        public RenderTexture laoPrecalculated;

        public RayPatternLAO? precalculatedRayPattern { get; set; }
        public VolumeShadingMode? precalculatedShadingMode { get; set; }
        public bool rayCastLaoPrecalculated { get; set; }

        public void Clear()
        {
            lastFrameVoxelClipping = -1;
            lastFrameRayCastLAO = -1;
            lastFrameNormals = -1;
            precalculatedRayPattern = null;
            precalculatedShadingMode = null;
            rayCastLaoPrecalculated = false;

            //Destroy(normals);
            //Destroy(opacityOutput);
            //Destroy(laoMask);
            //Destroy(laoOutput);
            //Destroy(laoPrecalculated);
        }

        //private void OnDestroy()
        //{
        //    Destroy(normals);
        //    Destroy(opacityOutput);
        //    Destroy(laoMask);
        //    Destroy(laoOutput);
        //    Destroy(laoPrecalculated);
        //}
    }
}