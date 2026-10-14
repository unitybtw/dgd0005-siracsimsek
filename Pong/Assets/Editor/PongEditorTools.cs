using System;
using System.IO;
using MCPForUnity.Editor.Services;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PongEditorTools
{
    [InitializeOnLoadMethod]
    private static void ConnectOnLoad()
    {
        if (Application.isBatchMode) return;
        EditorApplication.delayCall += Connect;
    }
    [MenuItem("Pong/Connect local Unity MCP")]
    public static async void Connect()
    {
        var config = EditorConfigurationCache.Instance;
        config.SetUseHttpTransport(true);
        config.SetHttpBaseUrl("http://127.0.0.1:8080");
        bool connected = await MCPServiceLocator.Bridge.StartAsync();
        Debug.Log($"[Pong] Unity MCP connected: {connected}");
    }
    [MenuItem("Pong/Open game scene")]
    public static void OpenGame() => EditorSceneManager.OpenScene("Assets/Scenes/Pong.unity");
    [MenuItem("Pong/QA/Start host")]
    public static void StartHost()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        var manager = FishNet.InstanceFinder.NetworkManager;
        manager.ServerManager.StartConnection();
        manager.ClientManager.StartConnection();
    }
    [MenuItem("Pong/QA/Serve as owner")]
    public static void Serve() => PaddleController.Local.RequestServe();
    [MenuItem("Pong/QA/Move owner to z 3")]
    public static void MoveOwner()
    {
        var p = PaddleController.Local.transform.position;
        p.z = 3; PaddleController.Local.transform.position = p;
    }
    [MenuItem("Pong/QA/Force right goal boundary")]
    public static void GoalBoundary()
    {
        if (!FishNet.InstanceFinder.IsServerStarted || !NetworkBall.Instance.InPlay.Value)
            throw new InvalidOperationException("A server rally must be active.");
        NetworkBall.Instance.transform.position = new Vector3(9, Court.BallY, 0);
    }
    [MenuItem("Pong/QA/Write runtime snapshot")]
    public static void Snapshot()
    {
        var state = GameState.Instance;
        var ball = NetworkBall.Instance;
        var manager = FishNet.InstanceFinder.NetworkManager;
        var data = new SnapshotData
        {
            server = manager != null && manager.IsServerStarted,
            client = manager != null && manager.IsClientStarted,
            paddles = PaddleController.All.Count,
            balls = UnityEngine.Object.FindObjectsByType<NetworkBall>(FindObjectsSortMode.None).Length,
            left = state == null ? -1 : state.LeftScore.Value,
            right = state == null ? -1 : state.RightScore.Value,
            serving = state == null ? -1 : state.ServingSide.Value,
            inPlay = ball != null && ball.InPlay.Value,
            ballPosition = ball == null ? Vector3.zero : ball.transform.position,
            localSide = PaddleController.Local == null ? -1 : PaddleController.Local.Side.Value
        };
        Directory.CreateDirectory("../verification");
        File.WriteAllText("../verification/runtime-snapshot.json", JsonUtility.ToJson(data, true));
        Debug.Log("[Pong] " + JsonUtility.ToJson(data));
    }
    [Serializable]
    private class SnapshotData
    {
        public bool server, client, inPlay;
        public int paddles, balls, left, right, serving, localSide;
        public Vector3 ballPosition;
    }
    [MenuItem("Pong/QA/Build standalone")]
    public static void BuildQA()
    {
        PongProjectSetup.RepairNetworkObjects();
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Pong.unity" },
            locationPathName = "/private/tmp/PongQA.app",
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.Development
        });
        if (result.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("Pong QA build failed: " + result.summary.result);
        Debug.Log("[Pong] QA build succeeded.");
    }
    [MenuItem("Pong/Export unitypackage")]
    public static void ExportPackage()
    {
        string deliveryDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../delivery"));
        Directory.CreateDirectory(deliveryDir);
        string packagePath = Path.Combine(deliveryDir, "dgd0005-siracsimsek.unitypackage");
        AssetDatabase.ExportPackage("Assets", packagePath, ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);
        Debug.Log("[Pong] Exported package to: " + packagePath);
    }
}

