# Unity GPU Based Occlusion Culling

Author: Przemyslaw Zaworski 
Licence: MIT

It is an early prototype of custom occlusion culling system, for both static and dynamic objects. This asset is intended especially for situations, when 
you can't use benefits from built-in Unity Occlusion Culling (loading custom prefabs from bundles or Streaming Assets), you use forward rendering and high-poly geometry etc.
For every selected game object, it creates bounding box with special transparent material. Pixel shader uses attribute
[earlydepthstencil], to force depth-stencil testing before a shader executes. It allows to get information,
whether current pixel will be finally visible on screen (is inside camera frustum and not occluded by another object).
If will be visible, RWStructuredBuffer gets current screen space vertex coordinates and writes into first UAV register. Otherwise, if invisible, 
RWStructuredBuffer will not write new values (and gets zero vector). On C# side, with ComputeBuffer.GetData, it copies
buffer values into array, then is performed enabling/disabling selected mesh renderer (dependent on current vector value from array).
This solution works best with prefabs made from large amount of various meshes/materials (when single bounding box contains many mesh renderers).
To add new object to be culled, add new entry into "Targets". To visualize bounding boxes, select "Debug".
By default, culling will not work in situations, when for example object is invisible on first camera and visible on second camera. It has to be invisible
on all cameras, but you can extend code from this asset and use OnPreRender() and OnPostRender() to get effect for individual camera.
Tested under Unity 2018.4.22f1 (64-bit, DX11 mode, both deferred and forward rendering). In current state, may not work with LWRP/HDRP. 
Scene "Main" contains four animated high-poly primitives.

To Do:

* Shadow Culling

* Deformable objects support (make efficient algorithm for recalculating bounding box in realtime) 


Effect off:
![alt text](CullingOff.gif)


Effect on:
![alt text](CullingOn.gif)
