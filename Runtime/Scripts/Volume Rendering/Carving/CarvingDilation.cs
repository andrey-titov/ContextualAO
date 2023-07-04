using ContextualAmbientOcclusion.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ContextualAmbientOcclusion.Runtime
{
    public class CarvingDilation : ComputeRoutines
    {
        RenderTexture dilationFront; //{ get; private set; }
        RenderTexture dilationBack; //{ get; private set; }    
                                    //public RenderTexture carvingDepth;
                                    //public RenderTexture carvingDepthDilation;

        private Camera carvingCamera;

        protected void Awake()
        {
            InitializeShaders();

            carvingCamera = GetComponent<Camera>();

            // Dilation Render Textures
            dilationFront = new RenderTexture(CarvingCamera.FBO_RESOLUTION, CarvingCamera.FBO_RESOLUTION, 0, RenderTextureFormat.R16, RenderTextureReadWrite.Linear);
            dilationFront.filterMode = FilterMode.Point;
            dilationFront.enableRandomWrite = true;
            dilationFront.Create();

            dilationBack = new RenderTexture(CarvingCamera.FBO_RESOLUTION, CarvingCamera.FBO_RESOLUTION, 0, RenderTextureFormat.R16, RenderTextureReadWrite.Linear);
            dilationBack.filterMode = FilterMode.Point;
            dilationBack.enableRandomWrite = true;
            dilationBack.Create();
        }

        private void OnDestroy()
        {
            Destroy(dilationFront);
            Destroy(dilationBack);
        }

        public CarvingConfiguration CreateCarvingConfiguration()
        {
            RenderTexture carvingDepth = new RenderTexture(CarvingCamera.FBO_RESOLUTION, CarvingCamera.FBO_RESOLUTION, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
            carvingDepth.filterMode = FilterMode.Bilinear;
            carvingDepth.enableRandomWrite = true;
            carvingDepth.Create();

            RenderTexture carvingDepthDilation = new RenderTexture(CarvingCamera.FBO_RESOLUTION, CarvingCamera.FBO_RESOLUTION, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
            carvingDepthDilation.filterMode = FilterMode.Bilinear;
            carvingDepthDilation.enableRandomWrite = true;
            carvingDepthDilation.Create();

            CarvingConfiguration carvingConfiguration = new CarvingConfiguration
            {
                carvingDepth = carvingDepth,
                carvingDepthDilation = carvingDepthDilation,
            };

            return carvingConfiguration;
        }

        public CarvingConfiguration DilateAndUnite(Dilation dilationKey, RenderTexture depthFront, RenderTexture depthBack)
        {
            CarvingConfiguration carvingConfiguration = CreateCarvingConfiguration();

            UpdateDilationParameters(dilationKey);

            SphericalDilation(depthFront, depthBack, true);
            SphericalDilation(depthFront, depthBack, false);

            UniteTextures(depthFront, depthBack, carvingConfiguration);

            return carvingConfiguration;
        }

        private void UpdateDilationParameters(Dilation dilationKey)
        {
            float dilationMm = dilationKey.spacingMagnitude; // Initial offset
            dilationMm += dilationKey.rayStepCountLAO;

            // XY
            float dilationXYratio = (dilationMm * 0.001f) / (carvingCamera.orthographicSize * 2f);

            float dilationXYpixels = dilationXYratio * CarvingCamera.FBO_RESOLUTION;
            int dilationXYpixelsInt = (int)Mathf.Ceil(dilationXYpixels);

            shader[0].SetInt("DilationXY", dilationXYpixelsInt);

            // Z
            float dilationZratio = (dilationMm * 0.001f) / (carvingCamera.farClipPlane - carvingCamera.nearClipPlane);

            shader[0].SetFloat("DilationZ", dilationZratio);
        }

        private void SphericalDilation(RenderTexture depthFront, RenderTexture depthBack, bool isFront)
        {
            shader[0].SetTexture(kernel[0], "DepthBuffer", isFront ? depthFront : depthBack);
            shader[0].SetTexture(kernel[0], "Result", isFront ? dilationFront : dilationBack);

            if (isFront)
            {
                shader[0].EnableKeyword("FRONT_FACES");
            }
            else
            {
                shader[0].DisableKeyword("FRONT_FACES");
            }

            ExecuteShader(0, depthFront.width, depthFront.height, 1);
        }

        private void UniteTextures(RenderTexture depthFront, RenderTexture depthBack, CarvingConfiguration carvingConfiguration)
        {
            shader[1].SetTexture(kernel[1], "DepthFront", depthFront);
            shader[1].SetTexture(kernel[1], "DepthBack", depthBack);
            shader[1].SetTexture(kernel[1], "DilationFront", dilationFront);
            shader[1].SetTexture(kernel[1], "DilationBack", dilationBack);
            shader[1].SetTexture(kernel[1], "ResultDepth", carvingConfiguration.carvingDepth);
            shader[1].SetTexture(kernel[1], "ResultDepthDilation", carvingConfiguration.carvingDepthDilation);

            ExecuteShader(1, depthFront.width, depthFront.height, 1);
        }
    }

    public struct Dilation
    {
        public int rayStepCountLAO;
        public float spacingMagnitude;

        public Dilation(int rayStepCountLAO, float spacingMagnitude)
        {
            this.rayStepCountLAO = rayStepCountLAO;
            this.spacingMagnitude = spacingMagnitude;
        }

        public override bool Equals(object obj)
        {
            Dilation k = (Dilation)obj;
            return k.rayStepCountLAO == rayStepCountLAO && k.spacingMagnitude == spacingMagnitude;
        }

        public override int GetHashCode()
        {
            return rayStepCountLAO.GetHashCode() + spacingMagnitude.GetHashCode();
        }
    }
}