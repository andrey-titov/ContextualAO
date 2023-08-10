using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Unity.VisualScripting;
using ContextualAmbientOcclusion.Runtime;

namespace ContextualAmbientOcclusion.Runtime
{
    public class CarvingCamera : MonoBehaviour
    {
        public Material frontMaterial;
        public Material backMaterial;
        //public MeshRenderer mesh;

        new public Camera camera { get; private set; }

        public RenderTexture depthFront { get; private set; }
        public RenderTexture depthBack { get; private set; }
        //public RenderTexture depthFrontSAMPLE;

        public CarvingDilation carvingDilation { get; private set; }

        public const int FBO_RESOLUTION = 1024;

        private readonly Vector3 RENDERING_OFFSET = new Vector3(100, 100, 100);

        public List<Volume> volumes { get; set; } = new List<Volume>();
        public Dictionary<Dilation, CarvingConfiguration> carvingConfigurations { get; set; } = new();

        public delegate void CarvingDestroyedAction(CarvingCamera carvingCamera);
        public static event CarvingDestroyedAction OnCarvingDestroyed;

        private VolumeRendering volumeRendering;

        //public RenderTexture carvingDepthSAMPLE; //{ get; private set; }
        //public RenderTexture carvingDepthDilationSAMPLE; //{ get; private set; }

        private void Awake()
        {
            carvingDilation = GetComponent<CarvingDilation>();

            // Depth Render Textures
            depthFront = new RenderTexture(FBO_RESOLUTION, FBO_RESOLUTION, 16, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
            depthBack = new RenderTexture(FBO_RESOLUTION, FBO_RESOLUTION, 16, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
            //depthFrontSAMPLE = new RenderTexture(FBO_RESOLUTION, FBO_RESOLUTION, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);

            depthFront.filterMode = FilterMode.Bilinear;
            depthBack.filterMode = FilterMode.Bilinear;

            // Properly setting camera
            camera = GetComponent<Camera>();
            camera.targetTexture = depthFront;
            camera.enabled = false;

            //GameObject initialMesh = null;
            //foreach (Transform child in transform.parent)
            //{
            //    if (child.parent == transform.parent && child.name == "Mesh")
            //    {
            //        initialMesh = child.gameObject;
            //        break;
            //    }
            //}
            LoadMeshes();

            Volume.OnVolumeLoaded += OnVolumeLoaded;
            Volume.OnVolumeDestroyed += OnVolumeDestroyed;
        }

        private void Start()
        {
            volumeRendering = FindObjectOfType<VolumeRendering>();

            foreach (Volume volume in volumeRendering.volumes)
            {
                GenerateDilationForVolume(volume);
            }
        }

        private void OnDestroy()
        {
            if (OnCarvingDestroyed != null)
            {
                OnCarvingDestroyed.Invoke(this);
            }

            Destroy(depthFront);
            Destroy(depthBack);
        }

        public void LoadMeshes()
        {
            RenderToDepthBuffers();

            foreach (var kv in carvingConfigurations)
            {
                Dilation dilationKey = new Dilation(kv.Key.rayStepCountLAO, kv.Key.spacingMagnitude);
                carvingDilation.DilateAndUnite(dilationKey, depthFront, depthBack, kv.Value);                
            }
        }

        private void OnVolumeLoaded(Volume volume)
        {
            if (!volumes.Contains(volume))
            {
                volumes.Add(volume);
            }

            GenerateDilationForVolume(volume);
        }

        private void OnVolumeDestroyed(Volume volume)
        {
            Dilation dilationKey = new Dilation(volume.rayStepCountLAO, volume.info.spacing.magnitude);

            int volumesSameKey = volumeRendering.volumes
                .Where(v => v != volume)
                .Where(v => v.rayStepCountLAO == dilationKey.rayStepCountLAO && v.info.spacing.magnitude == dilationKey.spacingMagnitude)
                .Count();

            // Destroy configuration if no other volume uses it
            if (volumesSameKey == 0)
            {
                CarvingConfiguration config = carvingConfigurations[dilationKey];
                Destroy(config.carvingDepth);
                Destroy(config.carvingDepthDilation);
                carvingConfigurations.Remove(dilationKey);
            }
        }

        private void GenerateDilationForVolume(Volume volume)
        {
            // Configuration for current LAO step count
            Dilation dilationKey = new Dilation(volume.rayStepCountLAO, volume.info.spacing.magnitude);

            if (!carvingConfigurations.ContainsKey(dilationKey))
            {
                carvingConfigurations[dilationKey] = carvingDilation.DilateAndUnite(dilationKey, depthFront, depthBack);
            }
        }


        private void RenderToDepthBuffers()
        {
            MeshRenderer[] subMeshes = GetComponentsInChildren<MeshRenderer>(false).Where(m => LayerMask.LayerToName(m.gameObject.layer) == VolumeRendering.LAYER_VOLUME_CARVING).ToArray();

            transform.position += RENDERING_OFFSET;
            foreach (MeshRenderer mesh in subMeshes)
            {
                mesh.material = frontMaterial;
            }
            camera.targetTexture = depthFront;
            camera.Render();

            GL.invertCulling = true;
            foreach (MeshRenderer mesh in subMeshes)
            {
                mesh.material = backMaterial;
            }
            camera.targetTexture = depthBack;
            camera.Render();
            GL.invertCulling = false;

            transform.position -= RENDERING_OFFSET;
            foreach (MeshRenderer mesh in subMeshes)
            {
                mesh.material = frontMaterial;
            }
            camera.targetTexture = depthFront;

            //Graphics.Blit(depthFront, depthFrontSAMPLE);
        }

        public Matrix4x4 GetProjectionMatrix()
        {
            return GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
        }
    }

    public class CarvingConfiguration
    {
        public RenderTexture carvingDepth { get; set; }
        public RenderTexture carvingDepthDilation { get; set; }
    }

}
