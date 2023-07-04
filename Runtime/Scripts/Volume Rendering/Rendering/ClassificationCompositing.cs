using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ContextualAmbientOcclusion.Runtime
{
    public class ClassificationCompositing : ComputeRoutine
    {
        // Properties of volume
        //private Volume volume;
        //private MeshRenderer meshRenderer;

        // Properties of camera
        private VolumeCamera volumeCamera;
        public RenderTexture volumeImage;

        private Dictionary<VolumeShadingMode, string> shadingModesKeywords;


        private List<Volume> volumes = new List<Volume>();

        // Start is called before the first frame update
        protected void Awake()
        {
            shadingModesKeywords = new Dictionary<VolumeShadingMode, string> {
            {VolumeShadingMode.SolidColor, "SOLID_COLOR" },
            {VolumeShadingMode.Phong, "PHONG" },
            {VolumeShadingMode.CAO, "LAO" },
            {VolumeShadingMode.PhongAndCAO, "PHONG_LAO" },
            {VolumeShadingMode.LAO, "LAO" },
            //{VolumeShadingMode.LAOFull1pass, "LAO" },
        };




            //Volume.OnVolumeLoaded += OnVolumeLoaded;
            //Volume.OnVolumeDestroyed += OnVolumeDestroyed;
            VolumeRendering.OnVolumeRenderingReady += OnVolumeRenderingReady;
        }

        private void OnVolumeRenderingReady(VolumeRendering volumeRendering)
        {
            volumeCamera = GetComponent<VolumeCamera>();
            InitializeShader(shadingModesKeywords[VolumeShadingMode.SolidColor], VoxelClipping.CARVING_KEYWORD + 0);
        }

        private void Start()
        {

            ResolutionObservable.OnResolutionChanged += OnResolutionChanged;
        }

        public void OnVolumeLoaded(Volume volume)
        {
            if (!volumes.Contains(volume))
            {
                if (volumeImage)
                {
                    volume.volumeImage = Instantiate(volumeImage);
                    volume.volumeImage.enableRandomWrite = true;
                    volume.volumeImage.Create();
                    volume.raycastedVolume.GetComponent<MeshRenderer>().material.SetTexture("_RaycastedImage", volume.volumeImage);
                }

                volumes.Add(volume);
            }
        }

        private void OnVolumeDestroyed(Volume volume)
        {
            volumes.Remove(volume);
        }

        private void ApplyVolume(Volume volume)
        {
            // Init Shader
            shader.SetTexture(kernel, "Intensities", volume.intensities);
            //shader.SetTexture(kernel, "Normals", volume.normals);
            //shader.SetTexture(kernel, "Lao", volume.lao);
            shader.SetFloat("IntensityThreshold", volume.transferFunction.GetIntensityThreshold(volume.info));
            ApplyTransferFunction(volume);
        }

        //public Texture2D colorsOpacityTexture;

        private void ApplyTransferFunction(Volume volume)
        {
            TransferFunction vp = volume.transferFunction;
            shader.SetTexture(kernel, "ColorOpacityTF", volume.colorOpacityTexture);

            // Phong
            shader.SetFloat("AmbientFactor", vp.ambientReflection);
            shader.SetFloat("DiffuseFactor", vp.diffuseReflection);
            shader.SetFloat("SpecularFactor", vp.specularReflection);
            shader.SetFloat("SpecularExponent", vp.specularReflectionPower);
        }

        public void Perform(Volume volume, RenderTexture positionDepth, RenderTexture directionSteps)
        {
            TextureHelper.ClearOutRenderTexture(volume.volumeImage);

            ApplyVolume(volume);

            shader.SetTexture(kernel, "VolumeImage", volume.volumeImage);

            //if (volume == null)
            //{
            //    return;
            //}

            //if (volume.realTimeLao)
            //{
            VolumeCao vc = volume.GetComponent<VolumeCao>();
            if (vc != null && vc.laoOutput != null)
            {
                shader.SetTexture(kernel, "Lao", vc.laoOutput);
            }

            if (volume.shadingMode == VolumeShadingMode.Phong
                || volume.shadingMode == VolumeShadingMode.PhongAndCAO)
            {
                shader.SetTexture(kernel, "Normals", vc.normals);
            }

            //}
            //else
            //{
            //    shader.SetTexture(kernel, "Lao", volume.lao);
            //}

            shader.SetFloat("SampleStepSize", volume.sampleStepSize);

            shader.SetTexture(kernel, "PositionDepth", positionDepth);
            shader.SetTexture(kernel, "DirectionSteps", directionSteps);
            //shader.SetTexture(kernel, "CarvingUsed", carvingUsed);

            // Set matrices
            Matrix4x4 matrixP = volumeCamera.boundariesFrontCamera.projectionMatrix;
            Matrix4x4 matrixV = volumeCamera.boundariesFrontCamera.worldToCameraMatrix;
            Matrix4x4 matrixM = volume.raycastedVolume.transform.GetComponent<Renderer>().localToWorldMatrix;
            shader.SetMatrix("MatrixM", matrixM);
            shader.SetMatrix("MatrixMV", matrixV * matrixM);

            shader.SetFloat("CameraFarClip", Camera.main.farClipPlane);
            shader.SetVector("ViewerPositionW", Camera.main.transform.position);
            shader.SetVector("LightPositionW", Camera.main.transform.position);

            var rect = volume.CalculateClosestDepthAndBoundingBox();


            //int workAreaWidth = rect.width; // volumeCamera.boundariesFrontCamera.targetTexture.width;
            //int workAreaHeight = rect.height; // volumeCamera.boundariesFrontCamera.targetTexture.height;
            shader.SetInt("Xstart", rect.xMin);
            shader.SetInt("Ystart", rect.yMin);

            //int screenWidth = volumeCamera.boundariesFrontCamera.targetTexture.width;
            //int screenHeight = volumeCamera.boundariesFrontCamera.targetTexture.height;
            //shader.SetInt("Xstart", 0);
            //shader.SetInt("Ystart", 0);

            // Shading Mode
            //shader.SetInt("ShadingMode", (int)volume.shadingMode);
            EnableSingleKeyword(shadingModesKeywords.Values, shadingModesKeywords[volume.shadingMode]);

            Dilation dilationKey = new Dilation(volume.rayStepCountLAO, volume.info.spacing.magnitude);
            //RenderTexture[] carvingDepths = volume.carving.Select(c => c.carvingConfigurations[dilationKey].carvingDepth).ToArray();

            CarvingCamera[] carvingCameras = volume.GetActiveCarvingObjects();

            // Set matrix for clipping
            Matrix4x4[] matrixCarvingMVP = new Matrix4x4[carvingCameras.Length];
            for (int i = 0; i < matrixCarvingMVP.Length; i++)
            {
                Matrix4x4 matrixCarvingP = carvingCameras[i].GetProjectionMatrix();
                Matrix4x4 matrixCarvingV = carvingCameras[i].camera.worldToCameraMatrix;
                matrixCarvingMVP[i] = matrixCarvingP * matrixCarvingV * matrixM;
            }
            shader.SetMatrixArray("CarvingMatrixMVP", matrixCarvingMVP);

            // Set carving depth buffers
            string[] keywords = new string[VoxelClipping.MAX_CARVING_OBJECTS];
            for (int i = 0; i < keywords.Length; i++)
            {
                keywords[i] = VoxelClipping.CARVING_KEYWORD + i;
            }
            for (int i = 0; i < carvingCameras.Length; i++)
            {
                shader.SetTexture(kernel, "CarvingDepth" + i, carvingCameras[i].carvingConfigurations[dilationKey].carvingDepth);
            }
            EnableSingleKeyword(keywords, VoxelClipping.CARVING_KEYWORD + carvingCameras.Length);

            ExecuteShader(rect.width, rect.height, 1);
        }

        private void OnResolutionChanged(Vector2Int newRes)
        {
            //volumeShading.SetTexture(kernelInit, "PositionDepth", shadingTexture);
            if (volumeImage != null)
            {
                Destroy(volumeImage);
            }

            volumeImage = new RenderTexture(newRes.x, newRes.y, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            volumeImage.filterMode = FilterMode.Point;
            volumeImage.enableRandomWrite = true;
            volumeImage.Create();


            //shader.SetTexture(kernel, "CarvingNormals", volumeCamera.carvingCamera.targetTexture);
            //shader.SetTexture(kernel, "BoundariesFront", volumeCamera.boundariesFrontCamera.targetTexture);

            // Visualization of created image
            foreach (Volume v in volumes)
            {
                if (v.volumeImage)
                {
                    Destroy(v.volumeImage);
                }

                v.volumeImage = Instantiate(volumeImage);
                v.volumeImage.enableRandomWrite = true;
                v.volumeImage.Create();

                //vs.volumeImage = new RenderTexture(newRes.x, newRes.y, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                //vs.volumeImage.filterMode = FilterMode.Point;
                //vs.volumeImage.enableRandomWrite = true;
                //vs.volumeImage.Create();

                v.raycastedVolume.GetComponent<MeshRenderer>().material.SetTexture("_RaycastedImage", v.volumeImage);
            }
        }
        private void OnDestroy()
        {
            if (volumeImage)
            {
                Destroy(volumeImage);
            }

        }
    }
}