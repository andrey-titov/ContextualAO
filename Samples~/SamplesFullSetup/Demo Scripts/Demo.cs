using ContextualAmbientOcclusion.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Demo : MonoBehaviour
{
    public Volume[] volumes;

    public const float ROTATION_SPEED = 20;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        foreach (Volume volume in volumes)
        {
            volume.transform.Rotate(new Vector3(0, 1, 0), -ROTATION_SPEED * Time.deltaTime);
        }
    }
}
