using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ContextualAmbientOcclusion.Runtime
{
    public class SphericalRaycast : ComputeRoutine
    {
        //public RenderTexture laoOutput;
        //public RenderTexture laoPrecalculated;

        //RenderTexture opacityOutput;
        //RenderTexture laoMask;

        //const int RAY_STEP_COUNT = 20;

        public int computeShaderParts { get; set; } = 1;

        private Dictionary<RayPatternLAO, string> rayPatternsKeywords;


        private void Awake()
        {
            rayPatternsKeywords = new Dictionary<RayPatternLAO, string> {
            {RayPatternLAO.Neighborhood6, "NEIGHBORS_6" },
            {RayPatternLAO.Neighborhood14, "NEIGHBORS_14" },
            {RayPatternLAO.Neighborhood26, "NEIGHBORS_26" },
            {RayPatternLAO.Rubiks54, "RUBIKS_54" },
            {RayPatternLAO.Sphere512, "SPHERE_512" },
        };

            VolumeRendering.OnVolumeRenderingReady += OnVolumeRenderingReady;
        }

        private void OnVolumeRenderingReady(VolumeRendering volumeRendering)
        {
            InitializeShader(rayPatternsKeywords[RayPatternLAO.Neighborhood6]);
        }

        private void Start()
        {

        }

        public bool RequirePrecalculation(Volume volume)
        {
            VolumeCao vc = volume.GetComponent<VolumeCao>();
            return !vc.rayCastLaoPrecalculated;
        }

        public void OnVolumeLoaded(Volume volume)
        {
            //    this.volume = volume;[
            //    //this.opacityOutput = opacityOutput;
            //    //this.laoMask = laoMask;

            //    if (laoOutput != null)
            //    {
            //        Destroy(laoOutput);
            //        Destroy(laoPrecalculated);
            //    }

            //    laoOutput = TextureHelper.CreateRenderTexture3D(volume.info.dimensions, RenderTextureFormat.R8);
            //    laoPrecalculated = TextureHelper.CreateRenderTexture3D(volume.info.dimensions, RenderTextureFormat.R8);
        }

        public void PrecalculateLAO(Volume volume)
        {
            //// Calculate size of Volume
            //Vector3 physicalSize = new Vector3(opacityOutput.width, opacityOutput.height, opacityOutput.volumeDepth);
            //Vector3 distanceToVoxel = new Vector3(1f / physicalSize.x, 1f / physicalSize.y, 1f / physicalSize.z);
            //physicalSize.Scale(volume.info.spacing);
            //Vector3 stepForMm = new Vector3(1f / physicalSize.x, 1f / physicalSize.y, 1f / physicalSize.z);
            ////stepForMm.Scale(new Vector3(laoOutput.width / (float)opacityOutput.width, laoOutput.height / (float)opacityOutput.height, laoOutput.volumeDepth / (float)opacityOutput.volumeDepth));

            shader.SetInt("RayStepCount", volume.rayStepCountLAO);

            VolumeCao vc = volume.GetComponent<VolumeCao>();

            // Set LAO parameters
            shader.SetTexture(kernel, "Opacities", vc.opacityOutput);
            //shader.SetTexture(kernelLao, "LaoPrecalculated", volume.lao);
            shader.SetTexture(kernel, "Mask", vc.laoMask);
            shader.SetVector("Dimensions", (Vector3)volume.info.dimensions);
            shader.SetVector("PhysicalSize", volume.physicalSizeMmWithBorder);
            shader.SetVector("StepForMm", volume.stepForMmInNcWithBorder);
            shader.SetFloat("DistanceToVoxel", volume.distanceToVoxelNcWithBorder.magnitude);

            EnableSingleKeyword(rayPatternsKeywords.Values, rayPatternsKeywords[volume.rayPatternLAO]);

            // LAO precalculation
            {
                shader.SetTexture(kernel, "Result", vc.laoPrecalculated);
                shader.SetTexture(kernel, "LaoPrecalculated", vc.laoOutput);
                //ExecuteShader(volume.info.dimensions);
                ExecuteShaderInParts(volume.info.dimensions, computeShaderParts);
            }

            vc.rayCastLaoPrecalculated = true;
            vc.precalculatedShadingMode = volume.shadingMode;
        }

        public void Perform(Volume volume)
        {
            VolumeCao vc = volume.GetComponent<VolumeCao>();

            if (CalculatedThisFrame(vc))
            {
                return;
            }

            shader.SetTexture(kernel, "Opacities", vc.opacityOutput);
            //shader.SetTexture(kernelLao, "LaoPrecalculated", volume.lao);
            shader.SetTexture(kernel, "Mask", vc.laoMask);

            shader.SetInt("RayStepCount", volume.rayStepCountLAO);

            shader.SetTexture(kernel, "Result", vc.laoOutput);
            shader.SetTexture(kernel, "LaoPrecalculated", vc.laoPrecalculated);

            //if (volume.shadingMode == VolumeShadingMode.LAOFull1pass)
            //{
            //    shader.EnableKeyword("LAO_ONE_PASS");
            //    shader.SetTexture(kernel, "CarvingDepth", carvingCamera.carvingDilation.carvingDepth);
            //    shader.SetTexture(kernel, "Intensities", volume.intensities);

            //    // Set matrices
            //    Matrix4x4 matrixP = carvingCamera.GetProjectionMatrix();
            //    Matrix4x4 matrixV = carvingCamera.camera.worldToCameraMatrix;
            //    Matrix4x4 matrixM = volume.raycastedVolume.transform.GetComponent<Renderer>().localToWorldMatrix;
            //    Matrix4x4 matrixMVP = matrixP * matrixV * matrixM;
            //    shader.SetMatrix("MatrixMVP", matrixMVP);

            //    // Distances
            //    shader.SetVector("PhysicalSize", volume.physicalSizeMm);
            //    shader.SetVector("StepForMm", volume.stepForMmInNc);
            //    shader.SetFloat("DistanceToVoxel", volume.distanceToVoxelNc.magnitude);
            //}
            //else
            //{
            //    shader.DisableKeyword("LAO_ONE_PASS");

            // Distances (correct)
            shader.SetVector("Dimensions", (Vector3)volume.info.dimensions);
            shader.SetVector("PhysicalSize", volume.physicalSizeMmWithBorder);
            shader.SetVector("StepForMm", volume.stepForMmInNcWithBorder);
            shader.SetFloat("DistanceToVoxel", volume.distanceToVoxelNcWithBorder.magnitude);
            //}

            if (volume.shadingMode == VolumeShadingMode.LAO)
            {
                shader.EnableKeyword("FULL_RECALCULATION");
            }
            else
            {
                shader.DisableKeyword("FULL_RECALCULATION");
            }

            EnableSingleKeyword(rayPatternsKeywords.Values, rayPatternsKeywords[volume.rayPatternLAO]);

            //ExecuteShader(volume.info.dimensions);
            TimeMeasuring.Start("RayCastLAO");
            ExecuteShaderInParts(volume.info.dimensions, computeShaderParts);
            TimeMeasuring.Pause("RayCastLAO");
        }

        private bool CalculatedThisFrame(VolumeCao vc)
        {
            if (vc.lastFrameRayCastLAO != Time.frameCount)
            {
                vc.lastFrameRayCastLAO = Time.frameCount;
                return false;
            }
            return true;
        }
    }
}