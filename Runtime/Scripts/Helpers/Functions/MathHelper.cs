using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ContextualAmbientOcclusion.Runtime
{
    public static class MathHelper
    {
        public static float Sinc(float value)
        {
            if (value == 0f)
            {
                return 1f;
            }

            return Mathf.Sin(value * Mathf.PI) / (value * Mathf.PI);
        }
    }
}