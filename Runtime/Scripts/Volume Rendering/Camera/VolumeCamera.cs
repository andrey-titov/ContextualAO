using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;

namespace ContextualAmbientOcclusion.Runtime
{
    public class VolumeCamera : MonoBehaviour
    {
        //public CarvingCamera carvingCamera;
        public List<Camera> cameras { get; private set; } = new List<Camera>();
        public Camera boundariesFrontCamera { get; private set; }
        public Camera boundariesBackCamera { get; private set; }
        public Camera occludersCamera { get; private set; }
        //public Camera carvingCamera { get; private set; }
        //public Camera dilationCamera { get; private set; }
        public VolumeRendering volumeRendering { get; set; }

        //public List<Volume> volumes { get; private set; } = new List<Volume>();
        //public List<RenderTexture> frontTextures { get; private set; } = new List<RenderTexture>();
        //public List<RenderTexture> backTextures { get; private set; } = new List<RenderTexture>();

        // Visualization Piepeline
        private RayCasting rayCasting;
        private ClassificationCompositing classifComposit;

        //Carving - LAO Pipeline
        private VoxelClipping voxelClipping;
        private SphericalRaycast rayCastLAO;
        private SobelNormals sobelNormals;

        //public delegate void BeforeRenderAction();
        //public static event BeforeRenderAction OnBeforeRender;

        private void Awake()
        {
            List<GameObject> cameraObjects = new List<GameObject>
        {
            new GameObject("Boundaries Front Camera"),
            new GameObject("Boundaries Back Camera"),
            new GameObject("Occluders Camera"),
            //cameraObjects.Add(new GameObject("Carving Camera"));
            //new GameObject("Dilation Camera")
        };

            foreach (GameObject cam in cameraObjects)
            {
                cam.transform.parent = transform;
                cam.transform.localPosition = Vector3.zero;
                cam.transform.localRotation = Quaternion.identity;
                cam.transform.localScale = Vector3.one;

                Camera c = cam.AddComponent<Camera>();
                c.enabled = false;
                c.depth = Camera.main.depth - 1f;
                c.clearFlags = CameraClearFlags.Color;
                c.backgroundColor = new Color(0, 0, 0, 0);
                c.allowMSAA = false;
                c.allowHDR = false;
                cameras.Add(c);
            }

            boundariesFrontCamera = cameras[0];
            boundariesBackCamera = cameras[1];
            occludersCamera = cameras[2];
            //carvingCamera = cameras[3];
            //dilationCamera = cameras[3];


            boundariesFrontCamera.cullingMask = LayerMask.GetMask("Volume Boundaries");
            boundariesBackCamera.cullingMask = LayerMask.GetMask("Volume Boundaries");
            occludersCamera.cullingMask = Camera.main.cullingMask;
            //carvingCamera.cullingMask = LayerMask.GetMask("Volume Carving");
            //dilationCamera.cullingMask = LayerMask.GetMask("Volume Dilation");

            ResolutionObservable.OnResolutionChanged += OnResolutionChanged;
            VolumeRendering.OnVolumeRenderingReady += OnVolumeRenderingReady;

            Volume.OnVolumeLoaded += OnVolumeLoaded;
            Volume.OnVolumeDestroyed += OnVolumeDestroyed;
        }

        private void OnVolumeRenderingReady(VolumeRendering volumeRendering)
        {
            this.volumeRendering = volumeRendering;

            // Visualization Piepeline
            rayCasting = GetComponent<RayCasting>();
            classifComposit = GetComponent<ClassificationCompositing>();

            //Carving - LAO Pipeline
            voxelClipping = GetComponent<VoxelClipping>();
            rayCastLAO = GetComponent<SphericalRaycast>();
            sobelNormals = GetComponent<SobelNormals>();

            foreach (Volume volume in volumeRendering.volumes)
            {
                OnVolumeLoaded(volume);
            }
        }

        private void OnVolumeLoaded(Volume volume)
        {
            //if (!volumes.Contains(volume))
            //{
            //    volumes.Add(volume);
            //}

            //if (frontTextures.Count < volumes.Count 
            //    && boundariesFrontCamera.targetTexture)
            //{
            //    frontTextures.Add(Instantiate(boundariesFrontCamera.targetTexture));
            //    backTextures.Add(Instantiate(boundariesBackCamera.targetTexture));
            //}

            rayCasting.OnVolumeLoaded(volume);
            classifComposit.OnVolumeLoaded(volume);

            voxelClipping.OnVolumeLoaded(volume);
            rayCastLAO.OnVolumeLoaded(volume);
            sobelNormals.OnVolumeLoaded(volume);

            //voxelClipping.PrecalculateOpacity(volume);
            //rayCastLAO.PrecalculateLAO();
        }

        private void OnVolumeDestroyed(Volume volume)
        {
            //volumes.Remove(volume);

            //RenderTexture front = frontTextures[frontTextures.Count - 1];
            //RenderTexture back = backTextures[backTextures.Count - 1];

            //frontTextures.Remove(front);
            //backTextures.Remove(back);

            //Destroy(front);
            //Destroy(back);
        }

        private void LateUpdate()
        {
            //if (previousRayPattern != volume.rayPatternLAO)
            //{
            //    voxelClipping.PrecalculateOpacity();
            //    rayCastLAO.PrecalculateLAO();
            //    previousRayPattern = volume.rayPatternLAO;
            //}
        }

        private void OnResolutionChanged(Vector2Int newRes)
        {
            foreach (Camera cam in cameras)
            {
                if (GetComponent<Camera>().targetTexture != null)
                {
                    Destroy(cam.targetTexture);
                }

                if (cam.cullingMask == LayerMask.GetMask("Volume Boundaries") || cam.cullingMask == LayerMask.GetMask("Volume Carving"))
                {
                    cam.targetTexture = new RenderTexture(newRes.x, newRes.y, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
                }
                else // Depth-only texture
                {
                    cam.targetTexture = new RenderTexture(newRes.x, newRes.y, 8, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
                }

                cam.targetTexture.wrapMode = TextureWrapMode.Clamp;
                cam.targetTexture.filterMode = FilterMode.Point;
                cam.targetTexture.anisoLevel = 0;
                cam.targetTexture.Create();

                cam.fieldOfView = Camera.main.fieldOfView;
            }

            //for (int i = 0; i < frontTextures.Count; i++)
            //{
            //    Destroy(frontTextures[i]);
            //    Destroy(backTextures[i]);
            //}

            //frontTextures.Clear();
            //backTextures.Clear();

            //for (int i = 0; i < volumes.Count; i++)
            //{
            //    frontTextures.Add(Instantiate(boundariesFrontCamera.targetTexture));
            //    backTextures.Add(Instantiate(boundariesBackCamera.targetTexture));
            //}
        }

        private void OnPreRender()
        {
            if (volumeRendering.volumes.Where(v => enabled).Count() == 0)
            {
                return;
            }

            bool isLeft = Camera.main.stereoActiveEye == Camera.MonoOrStereoscopicEye.Left;
            StereoTargetEyeMask eyeMask = isLeft ? StereoTargetEyeMask.Left : StereoTargetEyeMask.Right;
            Camera.StereoscopicEye stereoEye = isLeft ? Camera.StereoscopicEye.Left : Camera.StereoscopicEye.Right;

            // Setting up occluders camera
            occludersCamera.cullingMask = Camera.main.cullingMask;

            foreach (Camera cam in cameras)
            {
                //cam.enabled = true;

                cam.nearClipPlane = Camera.main.nearClipPlane;
                cam.farClipPlane = Camera.main.farClipPlane;

                if (XRSettings.isDeviceActive)
                {
                    cam.stereoTargetEye = eyeMask;
                    cam.worldToCameraMatrix = Camera.main.GetStereoViewMatrix(stereoEye);
                    cam.projectionMatrix = Camera.main.GetStereoProjectionMatrix(stereoEye);
                }
                else
                {
                    cam.worldToCameraMatrix = Camera.main.worldToCameraMatrix;
                    cam.projectionMatrix = Camera.main.projectionMatrix;
                }

                // Rendering
                if (cam != boundariesBackCamera && cam != boundariesFrontCamera)
                {
                    cam.Render();
                }
            }

            ////Carving - LAO Pipeline
            //if (volumes[0].shadingMode == VolumeShadingMode.CAO
            //    || volumes[0].shadingMode == VolumeShadingMode.PhongAndLAO
            //    || volumes[0].shadingMode == VolumeShadingMode.LAO
            //    //|| volume.shadingMode == VolumeShadingMode.LAOFull1pass
            //    )
            //{
            //    if (precalculatedRayPattern != volumes[0].rayPatternLAO)
            //    {
            //        voxelClipping.PrecalculateOpacity();
            //        rayCastLAO.PrecalculateLAO();
            //        precalculatedRayPattern = volumes[0].rayPatternLAO;
            //    }
            //
            //    voxelClipping.Perform();
            //    rayCastLAO.Perform();
            //}

            if (volumeRendering.volumes.Count == 0)
            {
                return;
            }

            int boundaryLayer = volumeRendering.volumes[0].colorBoundaries.layer;
            //classifComposit.PerformForAllVolumes();

            // Hiding all volumes
            foreach (Volume volume in volumeRendering.volumes)
            {
                volume.colorBoundaries.gameObject.layer = 0;
            }

            foreach (Volume volume in volumeRendering.volumes)
            {
                if (volume.enabled && volume.gameObject.activeSelf)
                {
                    volume.colorBoundaries.gameObject.layer = boundaryLayer;
                                        
                    if (volume.shadingMode == VolumeShadingMode.CAO
                        || volume.shadingMode == VolumeShadingMode.PhongAndCAO
                        || volume.shadingMode == VolumeShadingMode.LAO)
                    {
                        // Both clipping and ray cast needed
                        if (rayCastLAO.RequirePrecalculation(volume))
                        {
                            voxelClipping.PrecalculateOpacity(volume);
                            rayCastLAO.PrecalculateLAO(volume);
                        }

                        voxelClipping.Perform(volume);
                        rayCastLAO.Perform(volume);
                    }
                    else if (volume.shadingMode == VolumeShadingMode.Phong)
                    {
                        // Only voxel clipping needed
                        voxelClipping.Perform(volume);
                    }

                    if (volume.shadingMode == VolumeShadingMode.Phong
                        || volume.shadingMode == VolumeShadingMode.PhongAndCAO)
                    {
                        // Normals calculation needed
                        sobelNormals.Perform(volume);
                    }

                    // Render front and back faces
                    boundariesFrontCamera.Render();
                    GL.invertCulling = true;
                    boundariesBackCamera.Render();
                    GL.invertCulling = false;

                    // Visualization Piepeline
                    rayCasting.Perform(volume);
                    classifComposit.Perform(volume, rayCasting.positionDepth, rayCasting.directionSteps);

                    volume.colorBoundaries.gameObject.layer = 0;
                }
            }

            // Unhiding all volumes
            foreach (Volume volume in volumeRendering.volumes)
            {
                volume.colorBoundaries.gameObject.layer = boundaryLayer;
            }
        }

        private Matrix4x4 GetMonoOrStereoProjectionMatrix()
        {
            switch (Camera.main.stereoActiveEye)
            {
                case Camera.MonoOrStereoscopicEye.Left:
                    return Camera.main.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
                case Camera.MonoOrStereoscopicEye.Right:
                    return Camera.main.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
                case Camera.MonoOrStereoscopicEye.Mono:
                default:
                    return Camera.main.projectionMatrix;
            }
        }

        public Matrix4x4 GetProjectionMatrix()
        {
            return GL.GetGPUProjectionMatrix(GetMonoOrStereoProjectionMatrix(), true);
        }

        //public Matrix4x4 GetProjectionMatrixInverse()
        //{
        //    return GL.GetGPUProjectionMatrix(GetMonoOrStereoProjectionMatrix(), true).inverse;
        //}

        public Vector2Int GetResolution()
        {
            return new Vector2Int(boundariesFrontCamera.targetTexture.width, boundariesFrontCamera.targetTexture.height);
        }
    }
}