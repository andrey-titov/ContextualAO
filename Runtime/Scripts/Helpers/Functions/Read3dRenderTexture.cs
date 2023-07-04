using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ContextualAmbientOcclusion.Runtime
{
    public class Read3dRenderTexture : ComputeRoutine
    {
        private void Awake()
        {
            InitializeShader();
        }

        public float[] Read(RenderTexture renderTexture3d)
        {
            Vector3Int dimensions = new Vector3Int(renderTexture3d.height, renderTexture3d.width, renderTexture3d.volumeDepth);
            int arraySize = dimensions.x * dimensions.y * dimensions.z;

            //Array dataArray = Array.CreateInstance(typeof(float), arraySize);
            ComputeBuffer dataBuffer = new ComputeBuffer(arraySize, 4);
            //dataBuffer.SetData(dataArray);

            shader.SetVector("Dimensions", (Vector4)(Vector3)dimensions);
            shader.SetTexture(kernel, "Texture", renderTexture3d);
            shader.SetBuffer(kernel, "Result", dataBuffer);

            ExecuteShader(dimensions);

            Array dataArrayRead = Array.CreateInstance(typeof(float), arraySize);
            dataBuffer.GetData(dataArrayRead, 0, 0, arraySize);

            dataBuffer.Release();

            return (float[])dataArrayRead;
        }
    }
}