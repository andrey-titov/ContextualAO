using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ContextualAmbientOcclusion.Runtime
{
    public class SobelNormals : ComputeRoutine
    {
        public const RenderTextureFormat FORMAT_NORMALS = RenderTextureFormat.ARGB32;

        private void Awake()
        {
            VolumeRendering.OnVolumeRenderingReady += OnVolumeRenderingReady;
        }

        private void OnVolumeRenderingReady(VolumeRendering volumeRendering)
        {
            InitializeShader();
        }

        private void Start()
        {

        }

        public void OnVolumeLoaded(Volume volume)
        {

        }

        //public bool RequirePrecalculation(Volume volume)
        //{
        //    VolumeCao vc = volume.GetComponent<VolumeCao>();

        //    return !vc.normalsArePrecalculated;
        //}

        //public void PrecalculateNormals(Volume volume)
        //{
        //    VolumeCao vc = volume.GetComponent<VolumeCao>();

        //    shader.EnableKeyword("PRECALCULATION_PASS");

        //    shader.SetTexture(kernel, "Intensities", volume.intensities);
        //    shader.SetTexture(kernel, "OpacityTF", volume.opacityTexture);

        //    // Set Opacity parameters
        //    //shader.SetTexture(kernel, "Opacities", vc.opacityOutput);
        //    shader.SetTexture(kernel, "Result", vc.normalsPrecalculated);
        //    //shader.SetVector("Dimensions", (Vector3)volume.info.dimensions);

        //    ExecuteShader(volume.info.dimensions);
        //    shader.DisableKeyword("PRECALCULATION_PASS");

        //    vc.normalsArePrecalculated = true;
        //}

        public void Perform(Volume volume)
        {
            VolumeCao vc = volume.GetComponent<VolumeCao>();

            if (CalculatedThisFrame(vc))
            {
                return;
            }

            // Set Opacity parameters
            shader.SetTexture(kernel, "Opacities", vc.opacityOutput);
            shader.SetTexture(kernel, "Result", vc.normals);
            shader.SetVector("Dimensions", (Vector3)volume.info.dimensions);
            //shader.SetTexture(kernel, "NormalsPrecalculated", vc.normalsPrecalculated);
            //shader.SetTexture(kernel, "Mask", vc.laoMask);

            // Execute compute shaders
            TimeMeasuring.Start("SobelNormals");
            ExecuteShader(volume.info.dimensions);
            TimeMeasuring.Pause("SobelNormals");
        }

        private bool CalculatedThisFrame(VolumeCao vc)
        {
            if (vc.lastFrameNormals != Time.frameCount)
            {
                vc.lastFrameNormals = Time.frameCount;
                return false;
            }
            return true;
        }
    }
}