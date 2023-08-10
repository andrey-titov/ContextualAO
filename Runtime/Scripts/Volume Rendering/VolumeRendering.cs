using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace ContextualAmbientOcclusion.Runtime
{
    public class VolumeRendering : MonoBehaviour
    {        
        public ComputeShader voxelClipping;
        public ComputeShader rayCastLAO;
        public ComputeShader sobelNormals;
        public ComputeShader rayCasting;
        public ComputeShader classificationCompositing;

        public List<Volume> volumes { get; set; } = new List<Volume>();

        private VolumeCamera volumeCamera;

        public const string LAYER_VOLUME_BOUNDARIES = "Volume Boundaries";
        public const string LAYER_VOLUME_CARVING = "Volume Carving";

        public delegate void VolumeRenderingReadyAction(VolumeRendering volumeRendering);
        public static event VolumeRenderingReadyAction OnVolumeRenderingReady;

        void Awake()
        {
            Volume.OnVolumeLoaded += OnVolumeLoaded;
            Volume.OnVolumeDestroyed += OnVolumeDestroyed;
        }

        void Start()
        {
            volumeCamera = Camera.main.gameObject.AddComponent<VolumeCamera>();

            volumeCamera.gameObject.AddComponent<VoxelClipping>().shader = voxelClipping;
            volumeCamera.gameObject.AddComponent<SphericalRaycast>().shader = rayCastLAO;
            volumeCamera.gameObject.AddComponent<SobelNormals>().shader = sobelNormals;
            volumeCamera.gameObject.AddComponent<RayCasting>().shader = rayCasting;
            volumeCamera.gameObject.AddComponent<ClassificationCompositing>().shader = classificationCompositing;            

            if (OnVolumeRenderingReady != null)
            {
                OnVolumeRenderingReady.Invoke(this);
            }
        }

        private void OnVolumeLoaded(Volume volume)
        {
            if (!volumes.Contains(volume))
            {
                volumes.Add(volume);
            }

            if (volumeCamera != null)
            {
                volume.volumeCamera = volumeCamera;
            }
        }

        private void OnVolumeDestroyed(Volume volume)
        {
            volumes.Remove(volume);
        }
    }
}