using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor helper: swaps a drone prefab's greybox visual for an exported FBX "skin". It parents the model
/// under the prefab as a child named "Skin", disables the old primitive MeshRenderers, and saves the
/// prefab. The FBX→Unity axis/scale differ, so after running you'll tune the Skin child's
/// scale/rotation in the prefab to fit — but the mechanical swap is done for you. Re-runnable.
/// Menu: Udaan ▸ Skin Player Drone (T0).
/// </summary>
public static class DroneSkinner
{
    private const string PlayerPrefab = "Assets/Prefabs/Drone_Player.prefab";

    [MenuItem("Udaan/Skin Player Drone (T0)")]
    public static void SkinPlayerT0() => Skin(PlayerPrefab, "Assets/Art/Models/drone_player_t0.fbx", 0.35f);

    /// <summary>Make the spinning-prop "blur" disks translucent (URP transparent, alpha ~0.4).
    /// If materials are embedded in the FBX, extract them first (model Import Settings ▸ Materials ▸ Extract).</summary>
    [MenuItem("Udaan/Make Prop Disks Translucent")]
    public static void TranslucentProps()
    {
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material"))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (m == null || !m.name.Contains("PropBlur")) continue;
            m.SetFloat("_Surface", 1f);            // 0 opaque, 1 transparent (URP)
            m.SetFloat("_Blend", 0f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            foreach (var p in new[] { "_BaseColor", "_Color" })
                if (m.HasProperty(p)) { var c = m.GetColor(p); c.a = 0.4f; m.SetColor(p, c); }
            EditorUtility.SetDirty(m); n++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[DroneSkinner] Set {n} 'PropBlur' material(s) translucent." + (n == 0 ? " (none found — extract FBX materials first.)" : ""));
    }

    /// <summary>Attach a model FBX as the visual "Skin" of a prefab (best-effort scale; tune afterward).</summary>
    public static void Skin(string prefabPath, string modelPath, float scaleGuess)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (model == null) { Debug.LogError($"[DroneSkinner] Model not found: {modelPath} — run build-models.bat first."); return; }

        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null) { Debug.LogError($"[DroneSkinner] Prefab not found: {prefabPath}"); return; }

        try
        {
            var existing = root.transform.Find("Skin");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var skin = (GameObject)PrefabUtility.InstantiatePrefab(model);
            skin.name = "Skin";
            skin.transform.SetParent(root.transform, false);
            skin.transform.localPosition = Vector3.zero;
            skin.transform.localRotation = Quaternion.identity;
            skin.transform.localScale = Vector3.one * scaleGuess;

            // Hide the greybox: disable MeshRenderers that aren't part of the new skin.
            int hidden = 0;
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr.transform.IsChildOf(skin.transform)) continue;
                if (mr.enabled) { mr.enabled = false; hidden++; }
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"[DroneSkinner] Skinned {prefabPath} with {modelPath}. Added child 'Skin' (scale {scaleGuess}); disabled {hidden} greybox renderer(s). " +
                      "Open the prefab and tune Skin position/rotation/scale to fit; re-enable a greybox renderer if you want the old look back.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
