using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Editor helper: builds the Demo scene reliably (a DemoFlow object with the Drone_Player prefab
/// assigned + an EventSystem), saves it to Assets/Scenes/Demo.unity, and adds it to Build Settings.
/// Run via the menu: Udaan ▸ Create or Update Demo Scene.
/// </summary>
public static class DemoSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Demo.unity";
    private const string DronePath = "Assets/Prefabs/Drone_Player.prefab";

    [MenuItem("Udaan/Create or Update Demo Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var flowGo = new GameObject("DemoFlow");
        var flow = flowGo.AddComponent<DemoFlow>();
        var drone = AssetDatabase.LoadAssetAtPath<GameObject>(DronePath);
        if (drone == null)
            Debug.LogWarning($"DemoSceneBuilder: Drone_Player prefab not found at {DronePath} — assign DemoFlow.dronePrefab by hand.");
        flow.dronePrefab = drone;

        // Per-faction models (run build-models.bat first so these FBX exist).
        flow.enemyModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/enemy_binbot.fbx");
        flow.allyModel  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/drone_player_t0.fbx");
        flow.bossModel  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/boss_octa.fbx");

        // Park environment models (null = greybox fallback in ParkMapGenerator).
        flow.treeModel    = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/park_tree.fbx");
        flow.slideModel   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/park_slide.fbx");
        flow.swingModel   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/park_swing.fbx");
        flow.gymModel     = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/park_gym.fbx");
        flow.sandboxModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/park_sandbox.fbx");
        flow.seesawModel  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/park_seesaw.fbx");
        flow.playsetModel   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/park_playset.fbx");
        flow.merryModel     = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/park_merry.fbx");
        flow.domeModel      = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/park_dome.fbx");
        flow.tyreSwingModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/park_tyreswing.fbx");
        flow.rockWallModel    = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/park_rockwall.fbx");
        flow.tyreWallModel    = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/park_tyrewall.fbx");
        flow.trampolineModel  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/park_trampoline.fbx");
        flow.benchModel       = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/park_bench.fbx");
        flow.animalMerryModel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/park_animalmerry.fbx");

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!list.Exists(s => s.path == ScenePath))
        {
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
        Debug.Log($"Demo scene created/updated at {ScenePath} (added to Build Settings, index 0). Open it and press Play.");
    }
}
