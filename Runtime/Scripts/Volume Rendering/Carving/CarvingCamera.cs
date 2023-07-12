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

        public const string CARVING_MESH_LAYER = "Volume Carving";

        new public Camera camera { get; private set; }

        RenderTexture depthFront; //{ get; private set; }
        RenderTexture depthBack; //{ get; private set; }

        public CarvingDilation carvingDilation { get; private set; }

        public const int FBO_RESOLUTION = 1024;

        private readonly Vector3 RENDERING_OFFSET = new Vector3(100, 100, 100);

        //private MeshRenderer[] meshes;
        public List<Volume> volumes { get; set; } = new List<Volume>();
        public Dictionary<Dilation, CarvingConfiguration> carvingConfigurations = new();

        public delegate void CarvingDestroyedAction(CarvingCamera carvingCamera);
        public static event CarvingDestroyedAction OnCarvingDestroyed;

        //public GameObject meshObject { get; private set; }

        private void Awake()
        {
            carvingDilation = GetComponent<CarvingDilation>();

            // Depth Render Textures
            depthFront = new RenderTexture(FBO_RESOLUTION, FBO_RESOLUTION, 16, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
            depthBack = new RenderTexture(FBO_RESOLUTION, FBO_RESOLUTION, 16, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);

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

            //if (volume != null)
            //{
            //    carvingDilation.DilateAndUnite(volume, depthFront, depthBack);
            //}        
        }

        public void LoadNewMeshes()
        {
            RenderToDepthBuffers();

            foreach (Volume volume in volumes)
            {
                Dilation dilationKey = new Dilation(volume.rayStepCountLAO, volume.info.spacing.magnitude);
                carvingDilation.DilateAndUnite(dilationKey, depthFront, depthBack);
            }
        }

        private void OnVolumeLoaded(Volume volume)
        {
            if (!volumes.Contains(volume))
            {
                volumes.Add(volume);
            }

            // Configuration for current LAO step count
            {
                Dilation dilationKey = new Dilation(volume.rayStepCountLAO, volume.info.spacing.magnitude);

                if (!carvingConfigurations.ContainsKey(dilationKey))
                {
                    carvingConfigurations[dilationKey] = carvingDilation.DilateAndUnite(dilationKey, depthFront, depthBack);
                }
            }
        }


        private void RenderToDepthBuffers()
        {
            MeshRenderer[] subMeshes = GetComponentsInChildren<MeshRenderer>(false).Where(m => m.sortingLayerName == CARVING_MESH_LAYER).ToArray();

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
