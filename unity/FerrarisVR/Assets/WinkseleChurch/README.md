# Maria-Hemelvaartkerk — detailed exterior game asset

The original illustrative model is preserved unchanged in
`../versions/v1-illustrative-2026-09-12.zip`. This version is a separate asset.

## Use in Unity

Import **WinkseleChurch.unitypackage** into the existing FerrarisVR project.
Drag `Assets/WinkseleChurch/MariaHemelvaartkerk.prefab` into a scene. The package
contains the ready-built prefab, three LOD meshes, material assets, textures,
eight box colliders and a shader for Unity's **Built-in Render Pipeline**.
The current FerrarisVR world/scene has not been modified.

The `Unity/Assets/WinkseleChurch` folder is also supplied for direct copying.
Use **Ferraris → Create detailed Winksele church prefab** to regenerate the prefab.
The importer is idempotent and updates only that asset folder.

Desktop stone maps import at 4096px, slate and wood at 2048px. The Android override
uses 2048/1024px and ASTC 6×6. The LODGroup transitions are 0.55, 0.20 and 0.025
screen-relative height. Adjust them for the actual camera and scene.
Quest frame rate, memory usage and LOD transition appearance still require testing
on the headset; no hardware performance target is claimed.

## Other tools and editable source

- `church-master.blend`: editable architectural components with packed source maps.
- `church-lod0.glb`, `church-lod1.glb`, `church-lod2.glb`: self-contained PBR assets.
- Corresponding `.fbx` files: game-engine imports; accompanying image files must
  remain available. The Unity package handles material setup automatically.
- `church-collision.fbx`: eight simple collision volumes, separate from visuals.
- `church-hero.png`, `church-detail.png`: Cycles renders of the **exported LOD0 GLB
  reimported into Blender**, not generated concept art.

| Mesh | Triangles | Objects | Material draws per pass |
| --- | ---: | ---: | ---: |
| LOD0 | 77,252 | 2 | 13 |
| LOD1 | 30,709 | 2 | 13 |
| LOD2 | 7,056 | 2 | 13 |

There are eight unique materials. The body and sacristy are separate objects;
shared materials are reused. Shadow and additional-light passes may add draw calls.

GLB/FBX exports use Y up; the Blender master uses Z up. +X follows the long axis
towards the choir. The origin is at ground level in the nave. Model extents are
approximately **43.72 × 19.79 × 37.33 m (length × width × height)**. These are chosen
model dimensions, not verified dimensions of the real building.

## Detail and limits

The exterior has through-wall window recesses, separate glazing and leadwork,
individual arch stones, stepped buttresses, corner quoins, beveled stone edges,
clock numerals and hands, portal columns, gutters, downpipes and a modeled spire.
Materials use scanned base-color, tangent-space normal and roughness maps.
UV0 is for repeating materials; UV1 is packed separately for engine lightmaps.

This remains a **photo-based interpretation**, not photogrammetry, a laser scan or
a measured architectural reconstruction. Window tracery, clocks, weathering,
material scale and some small features are interpreted. There is no playable
interior, opening-door animation, destruction setup or baked interior lighting.
The collision volumes intentionally block entry and are approximate at the apse.

The building depicts the current exterior. The sacristy is a documented 1786
addition and is separated for editing. Removing it alone does not establish an
accurate 1775 reconstruction; other later fittings and historical finishes require
checking against period sources.

## Validation

- `geometry-validation.json`: all three GLBs checked for finite positions, normals,
  tangents and both UV sets; valid indices; unit-length normals; PBR texture links;
  decreasing triangle counts.
- `unity-asset-validation.json`: imported successfully in an isolated Unity
  **6000.6.0f1** project. Verified scale/orientation, shader compilation, material
  assignments, three LODs and eight colliders. The prefab was exported from that test.
- Both delivered images were visually inspected after the wall-seam corrections.

Rebuild with Blender 4.5 LTS, then validate with Python 3:

```sh
blender --background --factory-startup -t 8 --python build_asset.py
python3 check_assets.py
```

The texture files are included. `texture-sources.json` records original download
URLs, sizes and checksums. `SHA256SUMS.txt` covers delivered outputs.

## Sources and material rights

Architectural references:

- [Kerk in Herent historical brochure, including the 1881 Mortier plan](https://www.kerkinherent.be/wp-content/uploads/2015/11/Onze-Lieve-Vrouwekerk-Historiek.pdf).
- [Open Churches exterior photographs and description](https://openchurches.eu/en-be/churches/maria-hemelvaart-herent).
- [Onroerend Erfgoed, object 41893](https://inventaris.onroerenderfgoed.be/erfgoedobjecten/41893).

Scanned material maps from Poly Haven, supplied under [CC0](https://polyhaven.com/license):

- [Sandstone Blocks 05](https://polyhaven.com/a/sandstone_blocks_05): weathered wall masonry.
- [Sandstone Blocks 08](https://polyhaven.com/a/sandstone_blocks_08): dressed stone.
- [Grey Roof Tiles](https://polyhaven.com/a/grey_roof_tiles): weathered slate.
- [Wood Planks Grey](https://polyhaven.com/a/wood_planks_grey): doors and louvres.

The church geometry and assembly scripts were created for this project.
Reference photographs have not been redistributed or applied as building textures.
