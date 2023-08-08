using ContextualAmbientOcclusion.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class DemoVR : MonoBehaviour
{
    public InputActionManager inputActionManager;
    public Volume[] volumes;

    private InputActionAsset inputs;

    private void Awake()
    {
        //Application.targetFrameRate = -1;
        //QualitySettings.vSyncCount = 0;

        inputs = inputActionManager.actionAssets[1];

        InputAction toggleSolidColor = inputs.FindActionMap("Main").FindAction("Solid Color");
        toggleSolidColor.started += OnToggleSolidColor;

        InputAction togglePhong = inputs.FindActionMap("Main").FindAction("Phong");
        togglePhong.started += OnTogglePhong;
    }    

    private void OnToggleSolidColor(InputAction.CallbackContext obj)
    {
        foreach (Volume volume in volumes)
        {
            if (volume.shadingMode != VolumeShadingMode.SolidColor)
            {
                volume.shadingMode = VolumeShadingMode.SolidColor;
            }
            else
            {
                volume.shadingMode = VolumeShadingMode.CAO;
            }
        }
    }

    private void OnTogglePhong(InputAction.CallbackContext obj)
    {
        foreach (Volume volume in volumes)
        {
            if (volume.shadingMode != VolumeShadingMode.Phong)
            {
                volume.shadingMode = VolumeShadingMode.Phong;
            }
            else
            {
                volume.shadingMode = VolumeShadingMode.CAO;
            }
        }
    }
}
