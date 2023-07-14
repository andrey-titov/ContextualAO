using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ContextualAmbientOcclusion.Runtime
{
    public class Volume : MonoBehaviour
    {
        //public string selectedVolume;
        //public string volumePropertyFile;

        //public bool realTimeLao;
        [Range(0.1f, 2f)]
        public float sampleStepSize = 1f;
        public bool dithering = false;
        public VolumeShadingMode shadingMode = VolumeShadingMode.SolidColor;
        public RayPatternLAO rayPatternLAO = RayPatternLAO.Neighborhood6;
        public int rayStepCountLAO = 20;
        public CarvingCamera[] carving;

        public VolumeInfo info { get; private set; }
        public Texture3D intensities { get; private set; }
        //public RenderTexture normals { get; private set; }
        public VolumeReader reader { get; private set; }
        public TransferFunction transferFunction { get; private set; }
        public Texture2D colorOpacityTexture { get; private set; }
        public Texture2D colorTexture { get; private set; }
        public Texture2D opacityTexture { get; private set; }
        public RenderTexture volumeImage { get; set; }

        // Calculated Distance
        public float raySamplingStep { get; private set; }
        public Vector3 physicalSizeMm { get; private set; }
        public Vector3 stepForMmInNc { get; private set; }
        public Vector3 distanceToVoxelNc { get; private set; }
        public Vector3 physicalSizeMmWithBorder { get; private set; }
        public Vector3 stepForMmInNcWithBorder { get; private set; }
        public Vector3 distanceToVoxelNcWithBorder { get; private set; }

        public GameObject colorBoundaries { get; private set; }
        public GameObject raycastedVolume { get; private set; }

        private Vector3[] vertices;
        public VolumeCamera volumeCamera { get; set; }

        public delegate void VolumeLoadedAction(Volume volume);
        public static event VolumeLoadedAction OnVolumeLoaded;

        public delegate void VolumeDestroyedAction(Volume volume);
        public static event VolumeDestroyedAction OnVolumeDestroyed;

        private void Awake()
        {
            colorBoundaries = transform.Find("Color Boundaries").gameObject;
            raycastedVolume = transform.Find("Raycasted Volume").gameObject;

            //if (selectedVolume != null)
            //{
            //    LoadVolume(selectedVolume);
            //}
            //if (volumePropertyFile != null)
            //{
            //    volumeProperty = new VolumeProperty(volumePropertyFile);
            //}

            vertices = colorBoundaries.GetComponent<MeshFilter>().mesh.vertices.Distinct().ToArray();

            VolumeRendering.OnVolumeRenderingReady += OnVolumeRenderingReady;
            CarvingCamera.OnCarvingDestroyed += OnCarvingDestroyed;
        }

        private void OnCarvingDestroyed(CarvingCamera carvingCamera)
        {
            if (carving.Contains(carvingCamera))
            {
                carving = carving.Where(c => c != carvingCamera).ToArray();
            }
        }

        private void OnVolumeRenderingReady(VolumeRendering volumeRendering)
        {
            volumeCamera = Camera.main.GetComponent<VolumeCamera>();
        }

        public void LoadNewVolume(VolumeInfo info, Texture3D intensities, TransferFunction transferFunction)
        {
            //info = Resources.Load<VolumeInfo>($"{folder}/info");
            //intensities = Resources.Load<Texture3D>($"{folder}/intensities");
            //normals = Resources.Load<Texture3D>($"{folder}/normals");
            //lao = Resources.Load<Texture3D>($"MRHead/lao");

            if (this.info != null)
            {
                Destroy(this.intensities);
                //Destroy(this.normals);
            }

            this.info = info;
            this.intensities = intensities;
            //this.normals = normals;
            LoadTransferFunction(transferFunction);

            UpdateScale();
            UpdateStepInfo();
            UpdatePhysicalSizeInfo();

            VolumeCao cao = GetComponent<VolumeCao>();
            if (cao != null) 
            {
                cao.Clear();
            }

            if (OnVolumeLoaded != null)
            {
                OnVolumeLoaded.Invoke(this);
            }
        }

        public void LoadTransferFunction(TransferFunction transferFunction)
        {
            this.transferFunction = transferFunction;

            if (colorOpacityTexture)
            {
                Destroy(colorOpacityTexture);
                Destroy(colorTexture);
                Destroy(opacityTexture);
            }

            colorOpacityTexture = transferFunction.GetColorOpacityTF(info);
            colorTexture = transferFunction.GetColorTF(info);
            opacityTexture = transferFunction.GetOpacityTF(info);
        }

        // Start is called before the first frame update
        void Start()
        {
            reader = GetComponent<VolumeReader>();

        }

        // Update is called once per frame
        void Update()
        {
            //GL.invertCulling = !GL.invertCulling;
        }

        private void OnDestroy()
        {
            if (OnVolumeDestroyed != null)
            {
                OnVolumeDestroyed.Invoke(this);
            }

            if (colorOpacityTexture)
            {
                Destroy(colorOpacityTexture);
                Destroy(colorTexture);
                Destroy(opacityTexture);
            }

            if (intensities)
            {
                Destroy(intensities);
            }

            if (volumeImage)
            {
                Destroy(volumeImage);
            }
        }

        private void UpdateScale()
        {
            Vector3 appliedScale = Vector3.one;
            appliedScale.Scale(info.dimensions);
            appliedScale.Scale(info.spacing);
            appliedScale *= 0.001f;
            colorBoundaries.transform.localScale = appliedScale;
            raycastedVolume.transform.localScale = appliedScale;

            //// New origin
            //volumeParent.transform.localPosition = origin + appliedScale * 0.5f;
        }

        private void UpdateStepInfo()
        {
            // PRISM-like calculations
            int dimWithSmallestStep;

            Vector3Int dimensions = info.dimensions;
            Vector3 spacing = info.spacing;

            if (spacing.x <= spacing.y && spacing.x <= spacing.z)
            {
                dimWithSmallestStep = dimensions.x;
            }
            else if (spacing.y <= spacing.x && spacing.y <= spacing.z)
            {
                dimWithSmallestStep = dimensions.y;
            }
            else
            {
                dimWithSmallestStep = dimensions.z;
            }

            //float minScale = Mathf.Min(scale.x, Mathf.Min(scale.y, scale.z));
            //float sampleDistance = 1f / minScale;

            float sampleDistance = 1f;
            float textureSpaceSamplingDenominator = 1f / dimWithSmallestStep;

            raySamplingStep = sampleDistance * textureSpaceSamplingDenominator;
            //volumeImage.material.SetFloat("step_size", realSamplingDistance);
            //volumeImage.material.SetFloat("step_size_adjustment", sampleDistance);
        }

        private void UpdatePhysicalSizeInfo()
        {
            // Dimensions of volume
            {
                Vector3 physicalSize = info.dimensions;
                distanceToVoxelNc = new Vector3(1f / physicalSize.x, 1f / physicalSize.y, 1f / physicalSize.z);
                physicalSize.Scale(info.spacing);
                physicalSizeMm = physicalSize;
                stepForMmInNc = new Vector3(1f / physicalSize.x, 1f / physicalSize.y, 1f / physicalSize.z);
            }

            // Dimensions of volume with border
            {
                Vector3 physicalSize = info.dimensions + new Vector3Int(2, 2, 2);
                distanceToVoxelNcWithBorder = new Vector3(1f / physicalSize.x, 1f / physicalSize.y, 1f / physicalSize.z);
                physicalSize.Scale(info.spacing);
                physicalSizeMmWithBorder = physicalSize;
                stepForMmInNcWithBorder = new Vector3(1f / physicalSize.x, 1f / physicalSize.y, 1f / physicalSize.z);
            }
        }

        public IEnumerable<Vector3> GetNormalizedMvpVertices()
        {
            Matrix4x4 mvpMatrix = volumeCamera.GetProjectionMatrix() * Camera.main.worldToCameraMatrix * raycastedVolume.transform.localToWorldMatrix;

            foreach (Vector3 pos in vertices)
            {
                Vector4 posTransformed = mvpMatrix * new Vector4(pos.x, pos.y, pos.z, 1);
                Vector3 posNormalizedCoordinates = new Vector3(posTransformed.x, posTransformed.y, posTransformed.z) / posTransformed.w;

                yield return posNormalizedCoordinates;
            }
        }

        public RectInt CalculateClosestDepthAndBoundingBox()
        {
            float xMin = float.MaxValue;
            float xMax = float.MinValue;
            float yMin = float.MaxValue;
            float yMax = float.MinValue;

            foreach (Vector3 vertex in GetNormalizedMvpVertices())
            {
                if (vertex.x < xMin)
                {
                    xMin = vertex.x;
                }
                if (vertex.x > xMax)
                {
                    xMax = vertex.x;
                }

                if (vertex.y < yMin)
                {
                    yMin = vertex.y;
                }
                if (vertex.y > yMax)
                {
                    yMax = vertex.y;
                }
            }

            Vector2Int resolution = volumeCamera.GetResolution();

            int xMinRes = (int)(resolution.x * Mathf.Clamp((xMin + 1f) / 2f, 0f, 1f));
            int xMaxRes = (int)(resolution.x * Mathf.Clamp((xMax + 1f) / 2f, 0f, 1f));
            int yMinRes = (int)(resolution.y * Mathf.Clamp((yMin + 1f) / 2f, 0f, 1f));
            int yMaxRes = (int)(resolution.y * Mathf.Clamp((yMax + 1f) / 2f, 0f, 1f));

            RectInt rect = new RectInt(xMinRes, resolution.y - yMaxRes, xMaxRes - xMinRes, yMaxRes - yMinRes);

            return rect;
        }

        public CarvingCamera[] GetActiveCarvingObjects()
        {
            return carving
                .Where(c => c.isActiveAndEnabled)
                .ToArray();
        }
    }
}