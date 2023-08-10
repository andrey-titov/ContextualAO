# ContextualAO
ContextualAO is a Unity package that includes a volume renderer with the Contextual Ambient Occlusion (CAO) algorithm that supports ambient occlusion with real-time clipping.

[![Watch the video](https://img.youtube.com/vi/I1nA5n5v1-E/maxresdefault.jpg)](https://www.youtube.com/watch?v=I1nA5n5v1-E)

## Installation
1) Create or open a project in Unity 2022.3 or later.
2) Navigate to *Window => Package Manager*. The *Package Manager* window should open.
3) Click on the small plus in the top left of the *Package Manager* window, and then click on **Add Package from git URL...**

![image](https://github.com/andrey-titov/ContextualAO/assets/22062174/600bceb2-5238-411c-8f51-7f2542ff1c5b)

4) Paste `https://github.com/andrey-titov/ContextualAO.git` and click on **Add**.
5) Wait for the package to be downloaded and installed. When this is done, the *ContextualAO* package should then appear in the list inside the *Package Manager* window.
6) Select the **ContextualAO** package in the list of packages, click on **Samples** an then on **Import** next to *Full Setp*.
7) *(Optionally)* Click on **Import** next to *VR Demo* in order to import the VR demo scene. This scene requires the package *XR Interaction Toolkit*, and it was tested to work with the version 2.4.3.
8) Navigate to *Edit => Project Settings*. The *Project Settings* Window should open.
9) Navigate to *Tags and Layers* and click one th *2 mini-sliders* icon on the top right.

![image](https://github.com/andrey-titov/ContextualAO/assets/22062174/aeeae63e-4428-4dcc-acc6-9b9f06fc61a1)

10) Click on the **CAO** preset.
11) In the *Project* window, navigate to *Assets => Samples => ContextualAO 1.0.0 => Full Setup*.
12) You can open the *Demo* scene to see a sample scene containing volumes that are being clipped and visualizaed using the Contextual Ambient Occlusion algorithm. You can also open the *Demo VR* to have an interactive scene where the right hand can be used to clip the volume. 

## Demo Scenes

### Demo

![2023-08-10 6=18=18 PM](https://github.com/andrey-titov/ContextualAO/assets/22062174/64866f37-7955-4bd1-ad30-eb21ab00d846)

The Demo scene features two volumes whose properties and types of rendering can be changed in real time. The right volume will slowly rotate in real time.

### Demo VR

![2023-08-10 6=27=10 PM](https://github.com/andrey-titov/ContextualAO/assets/22062174/632d07e8-ff10-482e-ae9c-ce83f3820e6f)

The Demo VR scene features the same two volumes that can be interactively clipped with a cubic clipping mesh in the left hand, and a spherical clipping mesh in the right hand. The visualization can be toggled between CAO and solid color rendering using the primary button on either of the controllers (buttons *A* and *X* on the Oculus Touch controllers). Additionally, it can be toggled between CAO and Blinn-Phong shading using the primary button on either of the controllers (buttons *B* and *Y* on the Oculus Touch controllers)
