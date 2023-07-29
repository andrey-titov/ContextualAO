using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum VolumeShadingMode
{
    SolidColor = 0, // Transfer Function values used directly
    Phong = 1, // Blinn_phong shading
    CAO = 2, // Contextual Ambient Occlusion
    PhongAndCAO = 3, // Contextual Ambient Occlusion combined with Phong (Experimental)
    LAO = 4, // Local Ambient Occlusion
}

public enum RayPatternLAO
{
    Neighborhood6 = 6,
    Neighborhood14 = 14,
    Neighborhood26 = 26,
    Rubiks54 = 54,
    Sphere512 = 512, // Only for benchmarking, should not be used
}