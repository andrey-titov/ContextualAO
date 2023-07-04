using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace ContextualAmbientOcclusion.Runtime
{
    public class ResolutionObservable : MonoBehaviour
    {
        public delegate void ResolutionChangedAction(Vector2Int newRes);
        public static event ResolutionChangedAction OnResolutionChanged;

        private Vector2Int resolution = new Vector2Int(0, 0);

        //private bool headsetIsUsed = false;

        // Start is called before the first frame update
        void Awake()
        {
            //resolution = new Vector2Int(Screen.width, Screen.height);
        }

        private void Start()
        {
            //if (OnResolutionChanged != null)
            //{
            //    OnResolutionChanged(resolution);
            //}
        }

        // Update is called once per frame
        void Update()
        {
            //if (XRSettings.isDeviceActive)
            //{
            //    headsetIsUsed = true;
            //}

            if (XRSettings.isDeviceActive)
            {
                if (XRSettings.eyeTextureWidth != 0 && XRSettings.eyeTextureHeight != 0
                && (resolution.x != XRSettings.eyeTextureWidth || resolution.y != XRSettings.eyeTextureHeight))
                {
                    resolution = new Vector2Int(XRSettings.eyeTextureWidth, XRSettings.eyeTextureHeight);
                    Debug.Log("XR resolution: " + resolution);

                    if (OnResolutionChanged != null)
                    {
                        OnResolutionChanged(resolution);
                    }
                }
            }
            else
            {
                if (resolution.x != Screen.width || resolution.y != Screen.height)
                {
                    resolution = new Vector2Int(Screen.width, Screen.height);
                    //Debug.Log("Monitor resolution: " + resolution);

                    if (OnResolutionChanged != null)
                    {
                        OnResolutionChanged(resolution);
                    }
                }
            }


        }
    }
}