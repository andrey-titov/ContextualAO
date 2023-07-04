using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum VolumeShadingMode
{
    SolidColor = 0,
    Phong = 1,
    CAO = 2,
    PhongAndCAO = 3,
    LAO = 4, // Only for benchmarking
    //LAOFull1pass = 5, // Only for benchmarking
}

public enum RayPatternLAO
{
    Neighborhood6 = 6,
    Neighborhood14 = 14,
    Neighborhood26 = 26,
    Rubiks54 = 54,
    Sphere512 = 512,
}