Low Poly Terrain
----------------

Thanks for purchasing the Low Poly Terrain Asset. Low Poly Terrain allows you to create large, faceted, terrains for your low-poly games.
- Import height and color maps from your favorite generation/painting tool.
- Alternatively, import height and color information from a Unity Terrain.
- Full collision support.
- Achieve a properly faceted look that the default terrain system can't.
- Generate and manager LOD levels dynamically, keeping a low poly count.
- Low CPU and GPU Usage, and taking advantage of dynamic batching.
- Place trees or other objects procedurally on your terrains.
- Ideal for high performance environments such as VR.
- Full source and shader code included.
- Works with PBR or legacy lighting models.

Version History
---------------

Version 1.3
- Unity 5.6 Upgrade
- Support for importing height and color information from a Unity Terrain directly.
- Run-time querying of the terrain height is now supported.
- Small tweak to make sure placed objects are tagged as statics so that batching can happen!

Version 1.2.1
- Unity 5.5 Upgrade
Version 1.2.0
- Prevent non power-of-two+1 collider resolutions
- Warning if LOD distances are incorrect
- Unity 5.3 update

Version 1.1.1
- Fix issue with multiple Object Generators on the same terrain, re-generating one would clear the objects from the other...

Version 1.1.0
- You can now import RAW heightmap files from World Machine or your favorite terrain generation tool. Both Raw/R16 (16-bit integers) and R32 (32-bit floating points) are supported. There is now a drop-down to let you pick your favorite method.
- You can now set LOD transition distances by hand, instead of using the default multiply-by-2 values.
- Additional Materials.
- Added the ability to turn shadow casting on, although the shadows flicker during transitions. There are still issues with this though
- Improved tessellation of quads so that the seam follows the source terrain as best as it can
- Reduced the length of the chunk squirts, they really only needed to be as big as the potential difference between one LOD level and the next.
- Fixed Collision mesh not matching render geometry properly. Added option to force consistent triangulation.
- Fixed transitions popping through fog and water planes

Version 1.0.0
- Initial release

Documentation
-------------

More information and tutorial videos can be found here:
http://jeansimonet.com/low-poly-terrain-documentation/

Here are the basic settings you need to specify when creating a new terrain.

0. Add a Low Poly Terrain object to the scene
Right click in the Hierarchy view and select Low Poly Terrain. Alternatively, you can use the GameObject Menu, or simply add a Low Poly Terrain component to an new GameObject.

1a. Height and Color Maps
The most important pieces of information you can set on your terrain are the height and color maps. The height map is a greyscale map that encodes the height of the terrain, and the color map its color. LowPolyTerrain uses vertex colors instead of UV coordinates by default to color the terrain. You can skip the vertex colors, and instead set uv coordinates on your terrain meshes by unchecking the box above 'color map'.

1b. Using a Unity Terrain
Alternatively, you can make the Low Poly Terrain import height and color information from a Unity Terrain.

2. Set the size of your terrain
Use the Terrain Size and Terrain Height fields to scale your terrain. The size of the terrain is the length of the terrain square. Powers of 2 work best with having multiple LOD levels (as LOD Levels double in size with every step, and so you terrain size must be divisible by 2 LODLevels times). The terrain must also be a multiple of the chunk size. The heights can be any number.

3. Set Chunk size, Resolution and LOD Levels
Chunk are the individual blocks of the terrain, they will switch LOD level based on distance to the camera.
•The Chunk Size is the length of one side of a chunk square. It must be a divisor of the Terrain Size. It must also be a multiple of the Resolution.
•The Base Resolution define how big the smallest triangle of your terrain can be. That is, at the lowest lod level (highest detail).
•LOD Levels define the number of lower resolution meshes each chunk will generate.

4. Set Vertex Offsets
The Y Offset moves terrain vertices up and down randomly at generation time. This is generally to add a little bit of visual interest to the terrain. Otherwise, you can end up with lots of coplanar triangles, which isn't very interesting with the low poly 3D style.
The XZ Offset moves the vertices on the plane randomly to even further add visual interest to the terrain. Note that the collider the terrain generates does NOT have its vertices moved on the plane (only on the Y axis) so your collisions will be slightly off if you use this field.

5. Set Materials
The terrain uses two materials, an opaque one, for the majority of the time, and a transparent one for when chunks switch LOD levels. The package comes with two default materials (and shaders) that use Unity 5's PBR shading, while using vertex colors instead of texture maps.
If you want to use you own materials, make sure you check or uncheck the Generate  Vert Colors checkbox appropriately. If your shader doesn't use vertex colors, but relies on uv coordinates, you must uncheck the box (and regenerate).

6. LOD Settings
These settings define when the terrain chunks switch LOD Levels. The LOD Distance is the distance at which a chunk switches from LOD0 to LOD1. The switching distance is then doubled for the next LOD level. The Flip Flop percent is there to prevent chunks switching back and forth if you happen to be right AT the transition distance. The LOD Transition time defines how long it takes for the switch from one level to happen. Adjust according to your taste!
