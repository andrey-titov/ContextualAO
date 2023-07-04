using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace ContextualAmbientOcclusion.Runtime
{
    public class RayCasting : ComputeRoutine
    {
        private VolumeCamera volumeCamera;
        public RenderTexture positionDepth;
        public RenderTexture directionSteps;
        //public RenderTexture carvingUsed { get; private set; }

        private const string DITHERING_KEYWORD = "DITHERING";

        private void Awake()
        {

            VolumeRendering.OnVolumeRenderingReady += OnVolumeRenderingReady;
        }

        private void OnVolumeRenderingReady(VolumeRendering volumeRendering)
        {
            volumeCamera = GetComponent<VolumeCamera>();
            InitializeShader();
        }

        // Start is called before the first frame update
        void Start()
        {

            ResolutionObservable.OnResolutionChanged += OnResolutionChanged;
        }

        public void OnVolumeLoaded(Volume volume)
        {

        }


        public void Perform(Volume volume)
        {
            // Init Shader
            shader.SetTexture(kernel, "Volume", volume.intensities);
            shader.SetFloat("IntensityThreshold", volume.transferFunction.GetIntensityThreshold(volume.info));

            shader.SetFloat("StepSize", volume.raySamplingStep * volume.sampleStepSize);

            Matrix4x4 matrixPinv = volumeCamera.GetProjectionMatrix().inverse;
            Matrix4x4 matrixV = volumeCamera.boundariesFrontCamera.worldToCameraMatrix;
            Matrix4x4 matrixM = volume.raycastedVolume.transform.GetComponent<Renderer>().localToWorldMatrix;
            shader.SetMatrix("MatrixMV", matrixV * matrixM);
            shader.SetMatrix("MatrixPinv", matrixPinv);

            //Debug.Log($"Frame {Time.frameCount}: {matrixPinv}");

            shader.SetFloat("CameraFarClip", Camera.main.farClipPlane);

            RectInt rect = volume.CalculateClosestDepthAndBoundingBox();

            //int workAreaWidth = rect.width; // volumeCamera.boundariesFrontCamera.targetTexture.width;
            //int workAreaHeight = rect.height; // volumeCamera.boundariesFrontCamera.targetTexture.height;
            shader.SetInt("Xstart", rect.xMin);
            shader.SetInt("Ystart", rect.yMin);

            //int screenWidth = volumeCamera.boundariesFrontCamera.targetTexture.width;
            //int screenHeight = volumeCamera.boundariesFrontCamera.targetTexture.height;
            //shader.SetInt("Xstart", 0);
            //shader.SetInt("Ystart", 0);

            if (volume.dithering)
            {
                shader.EnableKeyword(DITHERING_KEYWORD);
            }
            else
            {
                shader.DisableKeyword(DITHERING_KEYWORD);
            }

            ExecuteShader(rect.width, rect.height, 1);
        }

        private void OnResolutionChanged(Vector2Int newRes)
        {
            shader.SetTexture(kernel, "BoundariesFront", volumeCamera.boundariesFrontCamera.targetTexture);
            shader.SetTexture(kernel, "BoundariesBack", volumeCamera.boundariesBackCamera.targetTexture);
            shader.SetTexture(kernel, "Occluders", volumeCamera.occludersCamera.targetTexture);
            //shader.SetTexture(kernel, "CarvingNormals", volumeCamera.carvingCamera.targetTexture);

            //volumeShading.SetTexture(kernelInit, "PositionDepth", shadingTexture);
            if (positionDepth != null)
            {
                Destroy(positionDepth);
                Destroy(directionSteps);
            }

            positionDepth = new RenderTexture(newRes.x, newRes.y, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            positionDepth.filterMode = FilterMode.Point;
            positionDepth.enableRandomWrite = true;
            positionDepth.Create();

            directionSteps = new RenderTexture(newRes.x, newRes.y, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            directionSteps.filterMode = FilterMode.Point;
            directionSteps.enableRandomWrite = true;
            directionSteps.Create();

            //carvingUsed = new RenderTexture(newRes.x, newRes.y, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
            //carvingUsed.filterMode = FilterMode.Point;
            //carvingUsed.enableRandomWrite = true;
            //carvingUsed.Create();

            shader.SetTexture(kernel, "PositionDepth", positionDepth);
            shader.SetTexture(kernel, "DirectionSteps", directionSteps);
            //shader.SetTexture(kernel, "CarvingUsed", carvingUsed);

        }

        private void OnDestroy()
        {
            if (positionDepth)
            {
                Destroy(positionDepth);
                Destroy(directionSteps);
            }
        }
    }
}