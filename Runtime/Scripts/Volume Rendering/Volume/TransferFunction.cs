using ContextualAmbientOcclusion.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ContextualAmbientOcclusion.Runtime
{
    public class TransferFunction
    {
        public int textureLength { get; set; }
        public int interpolationType { get; set; }
        public bool shadingEnabled { get; set; }
        public float diffuseReflection { get; set; }
        public float ambientReflection { get; set; }
        public float specularReflection { get; set; }
        public float specularReflectionPower { get; set; }
        public OpacityControlPoint[] opacityTF { get; set; }
        public GradientControlPoint[] gradientTF { get; set; }
        public ColorControlPoint[] colorTF { get; set; }

        private const int TEXTURE_WIDTH_MIN = 256;
        private const int TEXTURE_WIDTH_MAX = 8192;

        public TransferFunction(string fileName, int textureLength)
        {
            this.textureLength = textureLength;

            using (TextReader reader = File.OpenText(fileName))
            {
                interpolationType = int.Parse(reader.ReadLine());
                shadingEnabled = int.Parse(reader.ReadLine()) == 1;

                // Phong
                diffuseReflection = float.Parse(reader.ReadLine());
                ambientReflection = float.Parse(reader.ReadLine());
                specularReflection = float.Parse(reader.ReadLine());
                specularReflectionPower = float.Parse(reader.ReadLine());

                // Opacity TF
                string[] opacityLine = reader.ReadLine().Split(' ');
                opacityTF = new OpacityControlPoint[int.Parse(opacityLine[0]) / 2];
                for (int i = 0; i < opacityTF.Length; i++)
                {
                    opacityTF[i].intensity = float.Parse(opacityLine[i * 2 + 1]);
                    opacityTF[i].opacity = float.Parse(opacityLine[i * 2 + 2]);
                }

                // Gradient TF
                string[] gradientLine = reader.ReadLine().Split(' ');
                gradientTF = new GradientControlPoint[int.Parse(gradientLine[0]) / 2];
                for (int i = 0; i < gradientTF.Length; i++)
                {
                    gradientTF[i].intensity = float.Parse(gradientLine[i * 2 + 1]);
                    gradientTF[i].opacity = float.Parse(gradientLine[i * 2 + 2]);
                }

                // Gradient TF
                string[] colorLine = reader.ReadLine().Split(' ');
                colorTF = new ColorControlPoint[int.Parse(colorLine[0]) / 4];
                for (int i = 0; i < colorTF.Length; i++)
                {
                    colorTF[i].intensity = float.Parse(colorLine[i * 4 + 1]);
                    float r = float.Parse(colorLine[i * 4 + 2]);
                    float g = float.Parse(colorLine[i * 4 + 3]);
                    float b = float.Parse(colorLine[i * 4 + 4]);
                    colorTF[i].color = new Vector3(r, g, b);
                }
            }
        }

        private Texture2D GenerateTexture(TextureFormat format, float[] opacities, Vector3[] colors)
        {
            // Create texture
            int length = (opacities == null) ? colors.Length : opacities.Length;
            Texture2D newTexture = new Texture2D(length, 1, format, false);
            newTexture.wrapMode = TextureWrapMode.Clamp;
            newTexture.filterMode = FilterMode.Bilinear;
            newTexture.anisoLevel = 0;

            // Copy opacity and color values
            Color[] colorsTexture = new Color[length];

            if (opacities != null && colors != null) // Both color and opacity
            {
                for (int i = 0; i < colorsTexture.Length; i++)
                {
                    colorsTexture[i] = (Vector4)colors[i];
                    colorsTexture[i].a = opacities[i];
                }
            }
            else if (colors != null) // Color only
            {
                for (int i = 0; i < colorsTexture.Length; i++)
                {
                    colorsTexture[i] = (Vector4)colors[i];
                    colorsTexture[i].a = 1f;
                }
            }
            else // Opacity only
            {
                for (int i = 0; i < colorsTexture.Length; i++)
                {
                    colorsTexture[i].r = opacities[i];
                }
            }

            newTexture.SetPixels(colorsTexture);
            newTexture.Apply();

            return newTexture;
        }

        private int GetVolumeSpecificTextureLength(VolumeInfo info)
        {
            Func<float, float> adjustIntensity = (float intensity) =>
            {
                return (intensity - info.min) / (info.max - info.min);
            };

            int opacityTextureLength;
            int colorTextureIndex;

            // Opacity
            {
                float firstIadj = adjustIntensity(opacityTF[0].intensity);
                float lastIadj = adjustIntensity(opacityTF[opacityTF.Length - 1].intensity);
                float differenceFirstLast = lastIadj - firstIadj;
                opacityTextureLength = (int)Mathf.Ceil(textureLength / differenceFirstLast);
            }

            // Color
            {
                float firstIadj = adjustIntensity(colorTF[0].intensity);
                float lastIadj = adjustIntensity(colorTF[colorTF.Length - 1].intensity);
                float differenceFirstLast = lastIadj - firstIadj;
                colorTextureIndex = (int)Mathf.Ceil(textureLength / differenceFirstLast);
            }

            int biggestLength = Math.Max(opacityTextureLength, colorTextureIndex);

            return Math.Clamp(biggestLength, TEXTURE_WIDTH_MIN, TEXTURE_WIDTH_MAX);
        }

        private float[] GetOpacityArray(VolumeInfo info, int volumeSpecificLength)
        {
            float[] opacities = new float[volumeSpecificLength];

            Func<float, float> adjustIntensity = (float intensity) =>
            {
                return (intensity - info.min) / (info.max - info.min);
            };

            Func<float, int> getIndex = (float intensityAdj) =>
            {
                return (int)(intensityAdj * volumeSpecificLength);
            };

            // Opacity TF
            for (int i = -1; i < opacityTF.Length; i++)
            {
                OpacityControlPoint start;
                OpacityControlPoint end;

                if (i == -1)
                {
                    start = opacityTF[i + 1];
                    end = opacityTF[i + 1];
                    start.intensity = info.min;
                }
                else if (i + 1 < opacityTF.Length)
                {
                    start = opacityTF[i];
                    end = opacityTF[i + 1];
                }
                else
                {
                    start = opacityTF[i];
                    end = opacityTF[i];
                    end.intensity = info.max;
                }

                float startIadj = adjustIntensity(start.intensity);
                float endIadj = adjustIntensity(end.intensity);

                int startIndex = getIndex(startIadj);
                int endIndex = getIndex(endIadj);

                int trueStartIndex = Math.Max(0, startIndex);
                int trueEndIndex = Math.Min(volumeSpecificLength, endIndex);

                for (int j = trueStartIndex; j < trueEndIndex; j++)
                {
                    float interpolationFactor = (j - startIndex) / (float)(endIndex - startIndex);
                    opacities[j] = Mathf.Lerp(start.opacity, end.opacity, interpolationFactor);
                }
            }

            return opacities;
        }

        private Vector3[] GetColorArray(VolumeInfo info, int volumeSpecificLength)
        {
            Vector3[] colors = new Vector3[volumeSpecificLength];

            Func<float, float> adjustIntensity = (float intensity) =>
            {
                return (intensity - info.min) / (info.max - info.min);
            };

            Func<float, int> getIndex = (float intensityAdj) =>
            {
                return (int)(intensityAdj * volumeSpecificLength);
            };

            // Color TF
            for (int i = -1; i < colorTF.Length; i++)
            {
                ColorControlPoint start;
                ColorControlPoint end;

                if (i == -1)
                {
                    start = colorTF[i + 1];
                    end = colorTF[i + 1];
                    start.intensity = info.min;
                }
                else if (i + 1 < colorTF.Length)
                {
                    start = colorTF[i];
                    end = colorTF[i + 1];
                }
                else
                {
                    start = colorTF[i];
                    end = colorTF[i];
                    end.intensity = info.max;
                }

                float startIadj = adjustIntensity(start.intensity);
                float endIadj = adjustIntensity(end.intensity);

                int startIndex = getIndex(startIadj);
                int endIndex = getIndex(endIadj);

                int trueStartIndex = Math.Max(0, startIndex);
                int trueEndIndex = Math.Min(volumeSpecificLength, endIndex);

                for (int j = trueStartIndex; j < trueEndIndex; j++)
                {
                    float interpolationFactor = (j - startIndex) / (float)(endIndex - startIndex);
                    Vector3 color = Vector3.zero;
                    color.x = Mathf.Lerp(start.color.x, end.color.x, interpolationFactor);
                    color.y = Mathf.Lerp(start.color.y, end.color.y, interpolationFactor);
                    color.z = Mathf.Lerp(start.color.z, end.color.z, interpolationFactor);
                    colors[j] = color;
                }
            }

            return colors;
        }

        public Texture2D GetColorOpacityTF(VolumeInfo info, TextureFormat format = TextureFormat.ARGB32)
        {
            int volumeSpecificLength = GetVolumeSpecificTextureLength(info);
            float[] opacities = GetOpacityArray(info, volumeSpecificLength);
            Vector3[] colors = GetColorArray(info, volumeSpecificLength);
            return GenerateTexture(format, opacities, colors);
        }

        public Texture2D GetColorTF(VolumeInfo info, TextureFormat format = TextureFormat.RGB24)
        {
            int volumeSpecificLength = GetVolumeSpecificTextureLength(info);
            Vector3[] colors = GetColorArray(info, volumeSpecificLength);
            return GenerateTexture(format, null, colors);
        }

        public Texture2D GetOpacityTF(VolumeInfo info, TextureFormat format = TextureFormat.R8)
        {
            int volumeSpecificLength = GetVolumeSpecificTextureLength(info);
            float[] opacities = GetOpacityArray(info, volumeSpecificLength);
            return GenerateTexture(format, opacities, null);
        }

        public float GetIntensityThreshold(VolumeInfo info)
        {
            Func<float, float> adjustIntensity = (float intensity) =>
            {
                return (intensity - info.min) / (info.max - info.min);
            };

            for (int i = 0; i < opacityTF.Length; i++)
            {
                if (opacityTF[i].opacity > 0f)
                {
                    int previousIndex = Math.Max(0, i - 1);
                    float previousIntensity = opacityTF[previousIndex].intensity;
                    return adjustIntensity(previousIntensity);
                }
            }

            return 0f;
        }
    }

    public struct OpacityControlPoint
    {
        public float intensity;
        public float opacity;
    }

    public struct GradientControlPoint
    {
        public float intensity;
        public float opacity;
    }

    public struct ColorControlPoint
    {
        public float intensity;
        public Vector3 color;
    }
}
