using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using System;

namespace ContextualAmbientOcclusion.Runtime
{
    public class VoxelClipping : ComputeRoutine
    {
        //private CarvingCamera carvingCamera;

        public List<Volume> volumes { get; set; } = new List<Volume>();

        public const string CARVING_KEYWORD = "CARVING_";
        public const int MAX_CARVING_OBJECTS = 8;

        // Start is called before the first frame update
        private void Awake()
        {
            VolumeRendering.OnVolumeRenderingReady += OnVolumeRenderingReady;
        }

        private void OnVolumeRenderingReady(VolumeRendering volumeRendering)
        {
            InitializeShader(CARVING_KEYWORD + 0);
        }

        private void Start()
        {

        }

        public void OnVolumeLoaded(Volume volume)
        {
            VolumeCao vc = null;
            if (!volumes.Contains(volume))
            {
                vc = volume.gameObject.AddComponent<VolumeCao>();
                volumes.Add(volume);
            }
            else
            {
                vc = volume.GetComponent<VolumeCao>();

                Destroy(vc.normals);
                Destroy(vc.opacityOutput);
                Destroy(vc.laoMask);
                Destroy(vc.laoOutput);
                Destroy(vc.laoPrecalculated);
            }

            vc.normals = TextureHelper.CreateRenderTexture3D(volume.info.dimensions, SobelNormals.FORMAT_NORMALS, FilterMode.Bilinear);
            //vc.normalsPrecalculated = TextureHelper.CreateRenderTexture3D(volume.info.dimensions, SobelNormals.FORMAT_NORMALS, FilterMode.Bilinear);
            vc.opacityOutput = TextureHelper.CreateRenderTexture3D(volume.info.dimensions + new Vector3Int(2, 2, 2), RenderTextureFormat.R8, FilterMode.Bilinear);
            vc.laoMask = TextureHelper.CreateRenderTexture3D(volume.info.dimensions, RenderTextureFormat.R8, FilterMode.Point);
            vc.laoOutput = TextureHelper.CreateRenderTexture3D(volume.info.dimensions, RenderTextureFormat.R8);
            vc.laoPrecalculated = TextureHelper.CreateRenderTexture3D(volume.info.dimensions, RenderTextureFormat.R8);
        }

        //private void ApplyTransferFunction(Volume volume)
        //{
        //    shader.SetTexture(kernel, "OpacityTF", volume.opacityTexture);
        //}

        //public bool RequirePrecalculation(Volume volume)
        //{
        //    VolumeCao vc = volume.GetComponent<VolumeCao>();

        //    return !vc.precalculatedRayPattern.HasValue
        //        || vc.precalculatedRayPattern != volume.rayPatternLAO
        //        || !vc.precalculatedShadingMode.HasValue
        //        || vc.precalculatedShadingMode != volume.shadingMode;
        //}

        public void PrecalculateOpacity(Volume volume)
        {
            shader.SetTexture(kernel, "OpacityTF", volume.opacityTexture);

            VolumeCao vc = volume.GetComponent<VolumeCao>();

            // Set Opacity parameters
            shader.SetTexture(kernel, "Intensities", volume.intensities);
            shader.SetVector("Dimensions", (Vector3)volume.info.dimensions);

            // Output        
            shader.SetTexture(kernel, "ResultOpacities", vc.opacityOutput);
            shader.SetTexture(kernel, "ResultMask", vc.laoMask);

            // Precalculate output when no clipping is applied
            shader.EnableKeyword("PRECALCULATION_PASS");
            ExecuteShader(volume.info.dimensions);
            shader.DisableKeyword("PRECALCULATION_PASS");

            
        }

        public void Perform(Volume volume)
        {
            VolumeCao vc = volume.GetComponent<VolumeCao>();

            if (CalculatedThisFrame(vc))
            {
                return;
            }

            //if (!vl.precalculatedRayPattern.HasValue || vl.precalculatedRayPattern != vl.volume.rayPatternLAO)
            //{
            //    PrecalculateOpacity(volume);
            //    vl.precalculatedRayPattern = volume.rayPatternLAO;
            //}

            shader.SetTexture(kernel, "OpacityTF", volume.opacityTexture);

            // Set Opacity parameters
            shader.SetTexture(kernel, "Intensities", volume.intensities);
            shader.SetVector("Dimensions", (Vector3)volume.info.dimensions);

            // Output        
            shader.SetTexture(kernel, "ResultOpacities", vc.opacityOutput);
            shader.SetTexture(kernel, "ResultMask", vc.laoMask);

            //if (volume.shadingMode == VolumeShadingMode.LAOFull1pass)
            //{
            //    return;
            //}
            if (volume.shadingMode == VolumeShadingMode.LAO)
            {
                shader.EnableKeyword("FULL_RECALCULATION");
            }
            else
            {
                shader.DisableKeyword("FULL_RECALCULATION");
            }

            Dilation dilationKey = new Dilation(volume.rayStepCountLAO, volume.info.spacing.magnitude);

            //shader.SetTexture(kernel, "CarvingDepthDilation", volume.carving[0].carvingConfigurations[dilationKey].carvingDepthDilation);

            // Set matrices
            //Matrix4x4 matrixP = volume.carving[0].GetProjectionMatrix();
            //Matrix4x4 matrixV = volume.carving[0].camera.worldToCameraMatrix;
            Matrix4x4 matrixM = volume.raycastedVolume.transform.GetComponent<Renderer>().localToWorldMatrix;
            //Matrix4x4 matrixMVP = matrixP * matrixV * matrixM;
            //Vector4 v = matrixMVP * new Vector4(0, 0, 0, 1);
            //v /= v.w;
            //Debug.Log("pos: " + v);

            CarvingCamera[] carvingCameras = volume.GetActiveCarvingObjects();
            Matrix4x4[] matrixCarvingMVP = new Matrix4x4[carvingCameras.Length];
            for (int i = 0; i < matrixCarvingMVP.Length; i++)
            {
                Matrix4x4 matrixCarvingP = carvingCameras[i].GetProjectionMatrix();
                Matrix4x4 matrixCarvingV = carvingCameras[i].camera.worldToCameraMatrix;
                matrixCarvingMVP[i] = matrixCarvingP * matrixCarvingV * matrixM;
            }
            shader.SetMatrixArray("CarvingMatrixMVP", matrixCarvingMVP);

            // Set carving depth buffers
            string[] keywords = new string[MAX_CARVING_OBJECTS];
            for (int i = 0; i < keywords.Length; i++)
            {
                keywords[i] = CARVING_KEYWORD + i;
            }
            for (int i = 0; i < carvingCameras.Length; i++)
            {
                shader.SetTexture(kernel, "CarvingDepthDilation" + i, carvingCameras[i].carvingConfigurations[dilationKey].carvingDepthDilation);
            }
            EnableSingleKeyword(keywords, CARVING_KEYWORD + carvingCameras.Length);


            // Execute compute shaders
            TimeMeasuring.Start("VoxelClipping");
            ExecuteShader(volume.info.dimensions);
            TimeMeasuring.Pause("VoxelClipping");
        }

        private bool CalculatedThisFrame(VolumeCao vc)
        {
            if (vc.lastFrameVoxelClipping != Time.frameCount)
            {
                vc.lastFrameVoxelClipping = Time.frameCount;
                return false;
            }
            return true;
        }
    }
}