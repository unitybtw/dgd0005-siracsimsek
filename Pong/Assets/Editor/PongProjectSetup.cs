using System.IO;
using System.Reflection;
using FishNet.Component.Spawning;
using FishNet.Component.Transforming;
using FishNet.Editing;
using FishNet.Managing;
using FishNet.Managing.Client;
using FishNet.Managing.Server;
using FishNet.Managing.Statistic;
using FishNet.Managing.Timing;
using FishNet.Managing.Transporting;
using FishNet.Object;
using FishNet.Transporting.Tugboat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PongProjectSetup
{
    private static void ConfigureStatistics(StatisticsManager statistics)
    {
        var serialized = new SerializedObject(statistics);
        serialized.FindProperty("_networkTraffic._enableMode").enumValueIndex = 1;
        serialized.FindProperty("_networkTraffic._updateClient").boolValue = true;
        serialized.FindProperty("_networkTraffic._updateServer").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(statistics);
    }
    public static void SerializeNetworkObject(NetworkObject obj, bool sceneObject)
    {
        // Match FishNet's own Reserialize NetworkObjects utility before saving generated assets.
        var method = typeof(NetworkObject).GetMethod("ReserializeEditorSetValues", BindingFlags.Instance | BindingFlags.NonPublic);
        method.Invoke(obj, new object[] { true, sceneObject });
        EditorUtility.SetDirty(obj);
    }
    [MenuItem("Pong/Repair network object serialization")]
    public static void RepairNetworkObjects()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
        if (EditorSceneManager.GetActiveScene().path != "Assets/Scenes/Pong.unity")
            EditorSceneManager.OpenScene("Assets/Scenes/Pong.unity");
        var createIds = typeof(NetworkObject).GetMethod("CreateSceneId", BindingFlags.Static | BindingFlags.NonPublic);
        createIds.Invoke(null, new object[] { EditorSceneManager.GetActiveScene(), true, 0 });
        foreach (string path in new[] { "Assets/Prefabs/Paddle.prefab", "Assets/Prefabs/Ball.prefab" })
        {
            var obj = AssetDatabase.LoadAssetAtPath<NetworkObject>(path);
            if (obj != null) SerializeNetworkObject(obj, false);
        }
        foreach (var obj in Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.None)) SerializeNetworkObject(obj, true);
        ConfigureStatistics(Object.FindFirstObjectByType<StatisticsManager>());
        RefreshDefaultPrefabsMenu.RebuildDefaultPrefabs();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[Pong] NetworkObject component references and scene IDs repaired.");
    }
    static PongProjectSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (!Application.isBatchMode && !File.Exists("Assets/Scenes/Pong.unity")
                && !EditorApplication.isPlayingOrWillChangePlaymode) CreateProject();
        };
    }
    private static void Set(Object target, string property, bool value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(property).boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void ConfigureTransform(GameObject go, bool clientAuthority)
    {
        var nt = go.AddComponent<NetworkTransform>();
        Set(nt, "_clientAuthoritative", clientAuthority);
        Set(nt, "_synchronizeRotation", false);
        Set(nt, "_synchronizeScale", false);
        var so = new SerializedObject(nt);
        so.FindProperty("_interpolation").intValue = 2;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    [MenuItem("Pong/Create missing scenes and prefabs")]
    public static void CreateProject()
    {
        if (File.Exists("Assets/Scenes/Pong.unity")) { Debug.Log("Pong scene already exists; keeping edits."); return; }
        Directory.CreateDirectory("Assets/Prefabs");
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Materials");
        // A referenced material also keeps the runtime-created court's shader in builds.
        var unlit = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PongUnlit.mat");
        if (unlit == null)
        {
            unlit = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            AssetDatabase.CreateAsset(unlit, "Assets/Materials/PongUnlit.mat");
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.transform.SetPositionAndRotation(new Vector3(-3, 20, 0), Quaternion.Euler(90, 0, 0));
        camera.orthographic = true; camera.orthographicSize = 8;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.045f, 0.065f);
        camera.gameObject.AddComponent<AudioListener>();
        var court = new GameObject("Court").AddComponent<Court>();
        var left = GameObject.CreatePrimitive(PrimitiveType.Cube);
        left.name = "Left Paddle"; left.transform.localScale = new Vector3(0.5f, 1, 2);
        left.GetComponent<Renderer>().sharedMaterial = unlit;
        Object.DestroyImmediate(left.GetComponent<Collider>());
        left.AddComponent<PaddleMovement>();
        var right = Object.Instantiate(left); right.name = "Right Paddle";
        var rs = new SerializedObject(right.GetComponent<PaddleMovement>());
        rs.FindProperty("side").intValue = 1; rs.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/Pong-Start.unity");
        Object.DestroyImmediate(right);
        Object.DestroyImmediate(left.GetComponent<PaddleMovement>());
        left.name = "Paddle";
        left.AddComponent<NetworkObject>();
        left.AddComponent<PaddleController>();
        ConfigureTransform(left, true);
        SerializeNetworkObject(left.GetComponent<NetworkObject>(), false);
        var paddle = PrefabUtility.SaveAsPrefabAsset(left, "Assets/Prefabs/Paddle.prefab").GetComponent<NetworkObject>();
        Object.DestroyImmediate(left);
        var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere); ball.name = "Ball";
        ball.GetComponent<Renderer>().sharedMaterial = unlit;
        ball.transform.position = new Vector3(0, Court.BallY, 0); ball.transform.localScale = Vector3.one * 0.5f;
        Object.DestroyImmediate(ball.GetComponent<Collider>());
        ball.AddComponent<NetworkObject>(); ball.AddComponent<NetworkBall>(); ConfigureTransform(ball, false);
        SerializeNetworkObject(ball.GetComponent<NetworkObject>(), false);
        var ballPrefab = PrefabUtility.SaveAsPrefabAsset(ball, "Assets/Prefabs/Ball.prefab").GetComponent<NetworkObject>();
        Object.DestroyImmediate(ball);
        Set(court, "localBall", false);
        court.gameObject.AddComponent<NetworkObject>();
        court.gameObject.AddComponent<BallSpawner>().BallPrefab = ballPrefab;
        var state = new GameObject("GameState"); state.AddComponent<NetworkObject>(); state.AddComponent<GameState>();
        SerializeNetworkObject(court.GetComponent<NetworkObject>(), true);
        SerializeNetworkObject(state.GetComponent<NetworkObject>(), true);
        var managerObject = new GameObject("NetworkManager");
        var transport = managerObject.AddComponent<Tugboat>(); transport.SetPort(7770); transport.SetClientAddress("localhost");
        managerObject.AddComponent<TransportManager>();
        var server = managerObject.AddComponent<ServerManager>(); server.SetStartOnHeadless(false);
        managerObject.AddComponent<ClientManager>();
        var time = managerObject.AddComponent<TimeManager>(); time.SetTickRate(30);
        var statistics = managerObject.AddComponent<StatisticsManager>();
        ConfigureStatistics(statistics);
        var manager = managerObject.AddComponent<NetworkManager>();
        managerObject.AddComponent<PlayerSpawner>().SetPlayerPrefab(paddle);
        managerObject.AddComponent<ConnectionLogger>(); managerObject.AddComponent<ServerBootstrap>();
        new GameObject("Hud").AddComponent<ConnectionHud>();
        AssetDatabase.SaveAssets();
        RefreshDefaultPrefabsMenu.RebuildDefaultPrefabs();
        var collections = AssetDatabase.FindAssets("t:DefaultPrefabObjects");
        if (collections.Length == 1)
            manager.SpawnablePrefabs = AssetDatabase.LoadAssetAtPath<FishNet.Managing.Object.DefaultPrefabObjects>(AssetDatabase.GUIDToAssetPath(collections[0]));
        if (manager.SpawnablePrefabs == null) throw new System.InvalidOperationException("DefaultPrefabObjects collection was not generated.");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/Pong.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Pong.unity", true) };
        PlayerSettings.productName = "Pong";
        PlayerSettings.runInBackground = true;
        AssetDatabase.SaveAssets();
        Debug.Log("[Pong] Scenes, prefabs, server HUD and 30 Hz networking created.");
    }
}
