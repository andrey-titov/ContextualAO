using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace ContextualAmbientOcclusion.Runtime
{
    public abstract class ComputeRoutine : MonoBehaviour
    {
        public ComputeShader shader;

        protected int kernel { get; private set; }
        protected uint[] groupSize { get; private set; }

        protected void InitializeShader(params string[] enabledKeywords)
        {
            DisableAllKeywords();
            foreach (string k in enabledKeywords)
            {
                shader.EnableKeyword(k);
            }

            kernel = shader.FindKernel("CSMain");
            groupSize = new uint[3];
            shader.GetKernelThreadGroupSizes(kernel, out groupSize[0], out groupSize[1], out groupSize[2]);
        }

        protected void ExecuteShader(int threadSizeX, int threadSizeY, int threadSizeZ)
        {
            int threadGroupsX = threadSizeX / (int)groupSize[0];
            int threadGroupsY = threadSizeY / (int)groupSize[1];
            int threadGroupsZ = threadSizeZ / (int)groupSize[2];

            threadGroupsX += threadSizeX % (int)groupSize[0] == 0 ? 0 : 1;
            threadGroupsY += threadSizeY % (int)groupSize[1] == 0 ? 0 : 1;
            threadGroupsZ += threadSizeZ % (int)groupSize[2] == 0 ? 0 : 1;

            if (threadGroupsX > 0 && threadGroupsY > 0 && threadGroupsZ > 0)
            {
                shader.Dispatch(kernel, threadGroupsX, threadGroupsY, threadGroupsZ);
            }
        }

        protected void ExecuteShader(Vector3Int threadSizes)
        {
            ExecuteShader(threadSizes.x, threadSizes.y, threadSizes.z);
        }

        protected void ExecuteShaderInParts(int threadSizeX, int threadSizeY, int threadSizeZ, int parts)
        {
            int threadGroupsX = threadSizeX / (int)groupSize[0];
            int threadGroupsY = threadSizeY / (int)groupSize[1];
            int threadGroupsZ = threadSizeZ / (int)groupSize[2];

            threadGroupsX += threadSizeX % (int)groupSize[0] == 0 ? 0 : 1;
            threadGroupsY += threadSizeY % (int)groupSize[1] == 0 ? 0 : 1;
            threadGroupsZ += threadSizeZ % (int)groupSize[2] == 0 ? 0 : 1;

            //if (parts > threadGroupsZ)
            //{
            //    parts = threadGroupsZ;
            //}

            if (threadGroupsX == 0 || threadGroupsY == 0 || threadGroupsZ == 0)
            {
                return;
            }

            int partialGroupSizeZ = threadGroupsZ / parts;
            partialGroupSizeZ += threadGroupsZ % parts == 0 ? 0 : 1;

            // Driver timout management
            Array timeoutArray = Array.CreateInstance(typeof(int), 1);
            Array timeoutArrayRead = Array.CreateInstance(typeof(int), 1);
            ComputeBuffer timeoutBuffer = new ComputeBuffer(1, 4);
            timeoutBuffer.SetData(timeoutArray);

            shader.SetBuffer(kernel, "timeoutBuffer", timeoutBuffer);

            for (int i = 0; i < parts; i++)
            {
                shader.SetInt("startZ", i * (int)groupSize[2] * partialGroupSizeZ);

                shader.Dispatch(kernel, threadGroupsX, threadGroupsY, partialGroupSizeZ);
                //MainCamera.ReadTexture(vssTextureCs, tex1, true);

                timeoutBuffer.GetData(timeoutArrayRead, 0, 0, 1);
            }

            timeoutBuffer.Release();
        }

        protected void ExecuteShaderInParts(Vector3Int threadSizes, int parts)
        {
            ExecuteShaderInParts(threadSizes.x, threadSizes.y, threadSizes.z, parts);
        }

        protected void DisableAllKeywords()
        {
            foreach (string k in shader.shaderKeywords)
            {
                shader.DisableKeyword(k);
            }
        }

        protected void DisableKeywords(IEnumerable<string> listKeywords)
        {
            foreach (string k in listKeywords)
            {
                shader.DisableKeyword(k);
            }
        }

        protected void EnableSingleKeyword(IEnumerable<string> listKeywords, string keywordToEnable)
        {
            foreach (string k in listKeywords)
            {
                shader.DisableKeyword(k);
            }

            shader.EnableKeyword(keywordToEnable);
        }
    }
}