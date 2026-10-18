using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Opt-in local QA instrumentation; never created during normal gameplay.
public class PongSmokeProbe : MonoBehaviour
{
    private string output;
    private float nextSample;
    private bool benchmark;
    private float nextServe;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        var args = Environment.GetCommandLineArgs();
        int flag = Array.IndexOf(args, "-pongProbe");
        if (flag < 0 || flag + 1 >= args.Length) return;
        var probe = new GameObject("Opt-in QA probe").AddComponent<PongSmokeProbe>();
        probe.output = args[flag + 1];
        probe.benchmark = Array.IndexOf(args, "-pongBenchmark") >= 0;
        if (probe.benchmark) { Application.targetFrameRate = 60; QualitySettings.vSyncCount = 0; }
#endif
    }
    private void Update()
    {
        var local = PaddleController.Local;
        var state = GameState.Instance;
        var ball = NetworkBall.Instance;
        if (benchmark && local != null && local.Side.Value < 2 && ball != null)
        {
            // Exercise the same input path as a player; never move the client transform.
            if (Keyboard.current != null)
            {
                float difference = Mathf.Clamp(ball.transform.position.z, -4f, 4f) - local.transform.position.z;
                var keys = Mathf.Abs(difference) < 0.25f ? new KeyboardState()
                    : new KeyboardState(difference > 0 ? Key.W : Key.S);
                InputSystem.QueueStateEvent(Keyboard.current, keys);
            }
            if (Time.unscaledTime >= nextServe && state != null && state.ServingSide.Value == local.Side.Value && !ball.InPlay.Value)
            {
                nextServe = Time.unscaledTime + 1;
                local.RequestServe();
            }
        }
        if (Time.unscaledTime < nextSample) return;
        nextSample = Time.unscaledTime + 0.5f;
        if (state == null || !state.GetComponent<FishNet.Object.NetworkObject>().IsClientInitialized) return;
        var p = new Sample
        {
            time = Time.unscaledTime,
            localSide = local == null ? -1 : local.Side.Value,
            left = state.LeftScore.Value, right = state.RightScore.Value,
            serving = state.ServingSide.Value,
            feedCount = state.Feed.Count,
            paddles = PaddleController.All.Count,
            balls = FindObjectsByType<NetworkBall>(FindObjectsSortMode.None).Length,
            inPlay = ball != null && ball.InPlay.Value,
            leftZ = PaddleController.All.Find(paddle => paddle.Side.Value == 0)?.transform.position.z ?? 0,
            rightZ = PaddleController.All.Find(paddle => paddle.Side.Value == 1)?.transform.position.z ?? 0,
            serverOut = FindFirstObjectByType<ConnectionHud>()?.ServerOutBytesPerSecond ?? 0,
            ball = ball == null ? Vector3.zero : ball.transform.position
        };
        File.AppendAllText(output, JsonUtility.ToJson(p) + "\n");
    }
    [Serializable]
    private class Sample
    {
        public float time, leftZ, rightZ;
        public double serverOut;
        public int localSide, left, right, serving, feedCount, paddles, balls;
        public bool inPlay;
        public Vector3 ball;
    }
}
