using UnityEditor;

/// <summary>
/// Standardizes every model imported under Assets/Art/Models so Blender FBX exports come in **upright at
/// a consistent scale** with no per-model fiddling. Runs automatically on import (and on Reimport).
///
/// The fix pair: build_models.py exports with bake_space_transform (Z-up → Y-up baked into the mesh), and
/// this sets Unity's importer to bake the axis conversion + use the file's own scale (1:1). Result: drag a
/// model in, it's oriented right and sized right. If a model's *nose* still faces the wrong way, that's a
/// per-model yaw you can set on the skin (FactionVisuals eulers / the prefab's Skin child), not a scale hack.
/// </summary>
public class UdaanModelPostprocessor : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        string p = assetPath.Replace('\\', '/');
        if (p.IndexOf("/Art/Models/", System.StringComparison.OrdinalIgnoreCase) < 0) return;

        var mi = (ModelImporter)assetImporter;
        // Orientation is already baked by build_models.py's export (bake_space_transform), so DON'T bake again
        // here (double-conversion re-rotates). If a model still comes in lying down / rotated, flip this to true.
        mi.bakeAxisConversion = false;
        mi.useFileScale = true;         // trust the FBX's real units (1 Blender m = 1 Unity m)
        mi.globalScale = 1f;
        mi.importCameras = false;
        mi.importLights = false;
        mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        mi.importNormals = ModelImporterNormals.Import;
    }
}
