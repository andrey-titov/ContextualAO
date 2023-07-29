# ContextualAO
ContextualAO is a Unity package that includes a volume renderer with the Contextual Ambient Occlusion (CAO) algorithm that supports ambient occlusion with real-time clipping.

## Installation
1) Create or open a project in Unity 2022.3 or later.
2) Navigate to *Window => Package Manager*. The *Package Manager* window should open.
3) Click on the small plus in the top left of the *Package Manager* window, and then click on **Add Package from git URL...**

![image](https://github.com/andrey-titov/ContextualAO/assets/22062174/600bceb2-5238-411c-8f51-7f2542ff1c5b)

4) Paste `https://github.com/andrey-titov/ContextualAO.git` and click on **Add**.
5) Wait for the package to be downloaded and installed. When this is done, the *ContextualAO* package should then appear in the list inside the *Package Manager* window.
6) Select the **ContextualAO** package in the list of packages, click on **Samples** an then on **Import** next to *Full Setp*.
7) Navigate to *Edit => Project Settings*. The *Project Settings* Window should open.
8) Navigate to *Tags and Layers* and click one th *2 mini-sliders* icon on the top right.

![image](https://github.com/andrey-titov/ContextualAO/assets/22062174/aeeae63e-4428-4dcc-acc6-9b9f06fc61a1)

9) Click on the **CAO** preset.
10) In the *Project* window, navigate to *Assets => Samples => ContextualAO 1.0.0 => Full Setup*.
11) You can open the *Demo Static* scene to see a sample scene containing volumes that are being clipped and visualizaed using the Contextual Ambient Occlusion algorithm. You can also open the *Demo VR* to have an interactive scene where the right hand can be used to clip the volume. However, that scene requires the package *XR Interaction Toolkit* of version 2.3.2 to be installed.
