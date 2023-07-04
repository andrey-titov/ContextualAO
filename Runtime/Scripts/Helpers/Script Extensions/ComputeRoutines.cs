using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ContextualAmbientOcclusion.Runtime
{
    public class ComputeRoutines : MonoBehaviour
    {
        public ComputeShader[] shader;

        protected int[] kernel { get; private set; }
        protected uint[][] groupSize { get; private set; }

        protected void InitializeShaders(params string[][] kernelEnabledKeywords)
        {
            for (int i = 0; i < kernelEnabledKeywords.Length; i++)
            {
                DisableAllKeywords(i);
                string[] enabledKeywords = kernelEnabledKeywords[i];

                foreach (string k in enabledKeywords)
                {
                    shader[i].EnableKeyword(k);
                }
            }

            kernel = new int[shader.Length];
            groupSize = new uint[shader.Length][];

            for (int i = 0; i < shader.Length; i++)
            {
                kernel[i] = shader[i].FindKernel("CSMain");
                groupSize[i] = new uint[3];
                shader[i].GetKernelThreadGroupSizes(kernel[i], out groupSize[i][0], out groupSize[i][1], out groupSize[i][2]);
            }
        }

        protected void ExecuteShader(int kernelIndex, int threadSizeX, int threadSizeY, int threadSizeZ)
        {
            int threadGroupsX = threadSizeX / (int)groupSize[kernelIndex][0];
            int threadGroupsY = threadSizeY / (int)groupSize[kernelIndex][1];
            int threadGroupsZ = threadSizeZ / (int)groupSize[kernelIndex][2];

            threadGroupsX += threadSizeX % (int)groupSize[kernelIndex][0] == 0 ? 0 : 1;
            threadGroupsY += threadSizeY % (int)groupSize[kernelIndex][1] == 0 ? 0 : 1;
            threadGroupsZ += threadSizeZ % (int)groupSize[kernelIndex][2] == 0 ? 0 : 1;

            shader[kernelIndex].Dispatch(kernel[kernelIndex], threadGroupsX, threadGroupsY, threadGroupsZ);
        }

        protected void ExecuteShader(int kernelIndex, Vector3Int threadSizes)
        {
            ExecuteShader(kernelIndex, threadSizes.x, threadSizes.y, threadSizes.z);
        }

        protected void DisableAllKeywords(int kernelIndex)
        {
            foreach (string k in shader[kernelIndex].shaderKeywords)
            {
                shader[kernelIndex].DisableKeyword(k);
            }
        }

        protected void EnableSingleKeyword(int kernelIndex, IEnumerable<string> listKeywords, string keywordToEnable)
        {
            foreach (string k in listKeywords)
            {
                shader[kernelIndex].DisableKeyword(k);
            }

            shader[kernelIndex].EnableKeyword(keywordToEnable);
        }
    }
}