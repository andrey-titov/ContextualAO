using itk.simple;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace ContextualAmbientOcclusion.Runtime
{
    public class VolumeReader : MonoBehaviour
    {
        public enum FileLocation
        {
            //Resources,
            StreamingAssets,
            StreamingAssetsSpecific,
            Path,
        }

        public enum RenderFormat
        {
            UINT8 = TextureFormat.R8,
            UINT16 = TextureFormat.R16,
            FLOAT16 = TextureFormat.RHalf,
            FLOAT32 = TextureFormat.RFloat,
        }

        const string VOLUME_FOLDER = "Volumes";
        const string TF_FOLDER = "Transfer Functions";

        //public ComputeShader precalculateNormals;
        public FileLocation fileLocation = FileLocation.StreamingAssets;
        public RenderFormat renderFormat = RenderFormat.UINT16;
        public int transferFunction1DLength = 512;

        public string volumeFile;
        public string transferFunctionFile;

        //public bool reverseX = false;
        //public bool reverseY = false;
        //public bool reverseZ = false;

        //private const TextureFormat FORMAT_INTENSITIES = TextureFormat.R8;

        //private const TextureFormat FORMAT_LAO = TextureFormat.R8;

        private Volume volume;
        //private int kernelNormals;

        //public Texture3D intensities;
        //public RenderTexture normals;


        private void Awake()
        {
            volume = GetComponent<Volume>();
        }

        private void Start()
        {
            // Read current file if it exists
            if (!string.IsNullOrWhiteSpace(volumeFile)
                && !string.IsNullOrWhiteSpace(transferFunctionFile))
            {
                //TimeMeasuring.Start("Read File");
                ReadFile();
                //TimeMeasuring.End("Read File");
            }
        }

        public void ReadFile()
        {
            // Read MINC file
            string mincFilePath = GetFileFolder(true) + volumeFile;
            ImageFileReader reader = new ImageFileReader();
            //reader.SetImageIO("NiftiImageIO");
            reader.SetFileName(mincFilePath);
            Image file = reader.Execute();

            // Read property file
            string propertyFilePath = GetFileFolder(false) + transferFunctionFile;
            TransferFunction transferFunction = new TransferFunction(propertyFilePath, transferFunction1DLength);

            // Read data file
            VolumeInfo info = ReadVolumeInfo(file);
            Color[] voxels = ReadVoxelData(file, info);
            Texture3D intensities = CreateDataTexture(voxels, info);
            //RenderTexture normals = CalculateNormalsCS(info, intensities, transferFunction);
            file.Dispose();

            volume.LoadNewVolume(info, intensities, transferFunction);
        }

        private string GetFileFolder(bool isVolume)
        {
            switch (fileLocation)
            {
                //case FileLocation.Resources:
                //    return Application.dataPath + "/Resources";
                case FileLocation.StreamingAssets:
                    return Application.streamingAssetsPath + "/";
                case FileLocation.StreamingAssetsSpecific:
                    return Application.streamingAssetsPath + "/" + (isVolume ? VOLUME_FOLDER : TF_FOLDER) + "/";
                case FileLocation.Path:
                    return "";
                default:
                    return "";
            }
        }

        private VolumeInfo ReadVolumeInfo(Image file)
        {
            VolumeInfo info = new VolumeInfo();

            // Size
            var imageDimensions = file.GetSize();
            info.dimensions.x = (int)imageDimensions[0];
            info.dimensions.y = (int)imageDimensions[1];
            info.dimensions.z = (int)imageDimensions[2];

            // Spacing
            var imageSpacing = file.GetSpacing();
            info.spacing.x = (float)imageSpacing[0];
            info.spacing.y = (float)imageSpacing[1];
            info.spacing.z = (float)imageSpacing[2];

            // Origin
            var imageOrigin = file.GetOrigin();
            info.origin.x = (float)imageOrigin[0];
            info.origin.y = (float)imageOrigin[0];
            info.origin.z = (float)imageOrigin[0];

            // Pixel Count
            info.voxelCount = (int)file.GetNumberOfPixels();

            // Min-Max values
            MinimumMaximumImageFilter minMaxFilter = new MinimumMaximumImageFilter();
            minMaxFilter.Execute(file);
            info.min = (float)minMaxFilter.GetMinimum();
            info.max = (float)minMaxFilter.GetMaximum();
            minMaxFilter.Dispose();

            if (file.GetNumberOfComponentsPerPixel() > 1)
            {
                throw new Exception("Number of components in medical image bigger than 1 is not supported.");
            }

            return info;
        }

        private Color[] ReadVoxelData(Image file, VolumeInfo info)
        {
            if (file.GetPixelID() == PixelIDValueEnum.sitkFloat32) // Float
            {
                return ReadImageData<float>(info, file.GetConstBufferAsFloat(), (src, temp) => Marshal.Copy(src, temp, 0, temp.Length));
            }
            else if (file.GetPixelID() == PixelIDValueEnum.sitkFloat64)
            {
                return ReadImageData<double>(info, file.GetConstBufferAsDouble(), (src, temp) => Marshal.Copy(src, temp, 0, temp.Length));
            }
            //else if (file.GetPixelID() == PixelIDValueEnum.sitkInt8) // Int
            //{
            //    return ReadImageData<sbyte>(info, file.GetConstBufferAsInt8(), (src, temp) => Marshal.Copy(src, temp, 0, temp.Length));
            //}
            else if (file.GetPixelID() == PixelIDValueEnum.sitkInt16)
            {
                return ReadImageData<short>(info, file.GetConstBufferAsInt16(), (src, temp) => Marshal.Copy(src, temp, 0, temp.Length));
            }
            else if (file.GetPixelID() == PixelIDValueEnum.sitkInt32)
            {
                return ReadImageData<int>(info, file.GetConstBufferAsInt32(), (src, temp) => Marshal.Copy(src, temp, 0, temp.Length));
            }
            else if (file.GetPixelID() == PixelIDValueEnum.sitkUInt8) // Uint
            {
                return ReadImageData<byte>(info, file.GetConstBufferAsUInt8(), (src, temp) => Marshal.Copy(src, temp, 0, temp.Length));
            }
            else if (file.GetPixelID() == PixelIDValueEnum.sitkUInt16)
            {
                return ReadImageData<ushort>(info, file.GetConstBufferAsUInt16(), (src, temp) =>
                {
                    short[] signed = new short[temp.Length];
                    Marshal.Copy(src, signed, 0, temp.Length);

                    for (int i = 0; i < temp.Length; i++)
                    {
                        temp[i] = unchecked((ushort)signed[i]);
                    }

                    //temp = Array.ConvertAll(signed, b => unchecked((ushort)b));

                    //int a = 0;
                    //for (int i = 0; i < temp.Length; i++)
                    //{
                    //    if (temp[i] != 0)
                    //    {
                    //        a++;
                    //    }
                    //}

                    //Debug.Log("a: " + a);
                });
            }
            //else if (file.GetPixelID() == PixelIDValueEnum.sitkUInt32)
            //{
            //    return ReadImageData<uint>(info, file.GetConstBufferAsUInt32(), (src, temp) => Marshal.Copy(src, temp, 0, temp.Length));
            //}
            else
            {
                throw new Exception($"Reading of format {file.GetPixelID()} is not implemented.");
            }
        }

        //private Color[] ReadImageData2(VolumeInfo info, IntPtr src, Action<IntPtr, short[]> action)
        //{
        //    short[] imageData = new short[info.voxelCount];
        //    action.Invoke(src, imageData);
        //    Color[] voxels = new Color[info.voxelCount];

        //    TimeMeasuring.Start("Image data read");

        //    NativeArray<short> na = new NativeArray<short>(imageData, Allocator.Temp);

        //    //imageData.Iterate3D(info.dimensions, (x, y, z, i) =>
        //    //{
        //    //    //int xAdj = reverseX ? (info.dimensions.x - x) - 1 : x;
        //    //    //int yAdj = reverseY ? (info.dimensions.y - y) - 1 : y;
        //    //    //int zAdj = reverseZ ? (info.dimensions.z - z) - 1 : z;

        //    //    //int iAdj = (xAdj) + (yAdj * info.dimensions.x) + (zAdj * info.dimensions.x * info.dimensions.y);

        //    //    na[i] = imageData[i];
        //    //});

        //    //ReadVolumeJob<T> job = new ReadVolumeJob<T>()
        //    //{
        //    //    min = info.min,
        //    //    max = info.max,
        //    //    rawData = new NativeArray<T>(imageData, Allocator.Persistent),
        //    //    voxels = new NativeArray<float>(imageData.Length, Allocator.Persistent),
        //    //};

        //    //JobHandle handle = job.Schedule(info.voxelCount, 64);
        //    //handle.Complete();

        //    imageData.Iterate3D(info.dimensions, (x, y, z, i) =>
        //    {
        //        float rawIntensity = (float)na[i];
        //        float normalizedIntensity = (rawIntensity - info.min) / (info.max - info.min);

        //        //int xAdj = reverseX ? (info.dimensions.x - x) - 1 : x;
        //        //int yAdj = reverseY ? (info.dimensions.y - y) - 1 : y;
        //        //int zAdj = reverseZ ? (info.dimensions.z - z) - 1 : z;

        //        //int iAdj = (xAdj) + (yAdj * info.dimensions.x) + (zAdj * info.dimensions.x * info.dimensions.y);

        //        voxels[i].r = normalizedIntensity;
        //    });

        //    TimeMeasuring.End("Image data read");

        //    //for (int i = 0; i < voxels.Length; i++)
        //    //{
        //    //    voxels[i].r = job.voxels[i];
        //    //}



        //    return voxels;
        //}

        private Color[] ReadImageData<T>(VolumeInfo info, IntPtr src, Action<IntPtr, T[]> action) where T : IConvertible
        {
            //TimeMeasuring.Start("Image data read");

            T[] imageData = new T[info.voxelCount];
            action.Invoke(src, imageData);
            //NativeArray<float> a = new NativeArray<float>(info.voxelCount * 4, Allocator.Persistent);
            Color[] voxels = new Color[info.voxelCount];



            //ReadVolumeJob<T> job = new ReadVolumeJob<T>()
            //{
            //    min = info.min,
            //    max = info.max,
            //    rawData = new NativeArray<T>(imageData, Allocator.Persistent),
            //    voxels = new NativeArray<float>(imageData.Length, Allocator.Persistent),
            //};

            //JobHandle handle = job.Schedule(info.voxelCount, 64);
            //handle.Complete();

            imageData.Iterate3D(info.dimensions, (x, y, z, i) =>
            {
                float rawIntensity = imageData[i].ToSingle(null);
                float normalizedIntensity = (rawIntensity - info.min) / (info.max - info.min);

                //int xAdj = reverseX ? (info.dimensions.x - x) - 1 : x;
                //int yAdj = reverseY ? (info.dimensions.y - y) - 1 : y;
                //int zAdj = reverseZ ? (info.dimensions.z - z) - 1 : z;

                //int iAdj = (xAdj) + (yAdj * info.dimensions.x) + (zAdj * info.dimensions.x * info.dimensions.y);

                voxels[i].r = normalizedIntensity;
            });

            //TimeMeasuring.End("Image data read");       

            return voxels;
        }

        private Texture3D CreateDataTexture(Color[] voxels, VolumeInfo info)
        {
            Texture3D dataTexture = new Texture3D(info.dimensions.x, info.dimensions.y, info.dimensions.z, (TextureFormat)renderFormat, false);
            dataTexture.wrapMode = TextureWrapMode.Clamp;
            dataTexture.filterMode = FilterMode.Bilinear;
            dataTexture.anisoLevel = 0;
            dataTexture.SetPixels(voxels);
            dataTexture.Apply();

            return dataTexture;
        }

        //private RenderTexture CalculateNormalsCS(VolumeInfo info, Texture3D dataTexture, TransferFunction transferFunction)
        //{
        //    RenderTexture normals = TextureHelper.CreateRenderTexture3D(info.dimensions, FORMAT_NORMALS, FilterMode.Bilinear);

        //    // Perform computation in a compute shader
        //    uint[] groupSize = new uint[3];
        //    precalculateNormals.GetKernelThreadGroupSizes(kernelNormals, out groupSize[0], out groupSize[1], out groupSize[2]);
        //    precalculateNormals.SetTexture(kernelNormals, "Intensities", dataTexture);
        //    precalculateNormals.SetTexture(kernelNormals, "Result", normals);

        //    Texture2D opacityTF = transferFunction.GetOpacityTF(info);
        //    precalculateNormals.SetTexture(kernelNormals, "OpacityTF", opacityTF);
        //    Destroy(opacityTF);

        //    //precalculateNormals.SetTexture(kernel, "Result2", normalsRT);

        //    precalculateNormals.Dispatch(kernelNormals, (info.dimensions.x / (int)groupSize[0]) + 1, (info.dimensions.y / (int)groupSize[1]) + 1, (info.dimensions.z / (int)groupSize[2]) + 1);

        //    return normals;
        //}
    }
}