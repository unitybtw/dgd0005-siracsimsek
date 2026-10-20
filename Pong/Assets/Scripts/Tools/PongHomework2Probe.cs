#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Reflection;
using FishNet;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Explicitly enabled verification only. No probe or file access during normal play.
public class PongHomework2Probe : MonoBehaviour
{
    private string output, commands;
    private int lastCommand, releaseFrame;
    private StreamWriter writer;
    private double nextFrameSample, nextFlush;
    private double preciseFrameSamplingUntil;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var args = Environment.GetCommandLineArgs();
        int flag = Array.IndexOf(args, "-pongHomework2Probe");
        if (flag < 0 || flag + 1 >= args.Length) return;
        new GameObject("Opt-in Homework 2 probe").AddComponent<PongHomework2Probe>().Configure(args[flag + 1]);
    }

    public void Configure(string path)
    {
        output = path;
        commands = path + ".command.json";
        writer = new StreamWriter(output, true);
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        InstanceFinder.TimeManager.OnPostTick += Sample;
    }

    private void Update()
    {
        if (releaseFrame > 0 && Time.frameCount >= releaseFrame && Keyboard.current != null)
        {
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            releaseFrame = 0;
        }
        if (commands == null || !File.Exists(commands) || PaddleController.Local == null) return;
        var c = JsonUtility.FromJson<Command>(File.ReadAllText(commands));
        if (c == null || c.id == lastCommand) return;
        lastCommand = c.id;
        if (!string.IsNullOrEmpty(c.key) && Keyboard.current != null)
        {
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState((Key)Enum.Parse(typeof(Key), c.key)));
            releaseFrame = Time.frameCount + Math.Max(2, c.frames);
        }
        else if (!string.IsNullOrEmpty(c.request))
        {
            if (c.request == "pauseTickInput")
            {
                // Isolate an exact 20-RPC Console capture from unrelated normal input packets.
                // The separate server's simulation tick remains subscribed and unchanged.
                var method = typeof(PaddleController).GetMethod("OnTick", BindingFlags.Instance | BindingFlags.NonPublic);
                var tick = (Action)Delegate.CreateDelegate(typeof(Action), PaddleController.Local, method);
                InstanceFinder.TimeManager.OnTick -= tick;
                return;
            }
            float axis = c.request == "nan" ? float.NaN : c.request == "positiveInfinity" ? float.PositiveInfinity
                : c.request == "negativeInfinity" ? float.NegativeInfinity : c.axis;
            typeof(PaddleController).GetMethod("ServerSetAxis", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(PaddleController.Local, new object[] { axis });
        }
    }

    private void LateUpdate()
    {
        // Preserve the very brief T jump/correction even when it falls between 120 Hz samples.
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            preciseFrameSamplingUntil = Time.realtimeSinceStartupAsDouble + 0.2;
        if (InstanceFinder.IsClientStarted && !InstanceFinder.IsServerStarted
            && (Time.realtimeSinceStartupAsDouble >= nextFrameSample
                || Time.realtimeSinceStartupAsDouble < preciseFrameSamplingUntil))
        {
            nextFrameSample = Time.realtimeSinceStartupAsDouble + 1.0 / 120;
            Sample();
        }
    }

    private void Sample()
    {
        if (output == null) return;
        var m = InstanceFinder.NetworkManager;
        var state = GameState.Instance;
        var ball = NetworkBall.Instance;
        var lines = new System.Collections.Generic.List<PaddleSample>();
        foreach (var p in PaddleController.All)
            if (p != null) lines.Add(new PaddleSample { side = p.Side.Value, owner = p.OwnerId,
                rejected = p.RejectedInputs, position = p.transform.position });
        var sample = new SampleData { time = Time.realtimeSinceStartupAsDouble, frame = Time.frameCount,
            tick = m.TimeManager.Tick, tickDelta = m.TimeManager.TickDelta,
            server = m.IsServerStarted, client = m.IsClientStarted, rtt = m.TimeManager.RoundTripTime,
            localSide = PaddleController.Local == null ? -1 : PaddleController.Local.Side.Value,
            left = state == null ? -1 : state.LeftScore.Value, right = state == null ? -1 : state.RightScore.Value,
            inPlay = ball != null && ball.InPlay.Value, ball = ball == null ? Vector3.zero : ball.transform.position,
            paddles = lines.ToArray() };
        writer.WriteLine(JsonUtility.ToJson(sample));
        if (Time.realtimeSinceStartupAsDouble >= nextFlush)
        {
            nextFlush = Time.realtimeSinceStartupAsDouble + 0.25;
            writer.Flush();
        }
    }

    private void OnDestroy()
    {
        if (InstanceFinder.TimeManager != null) InstanceFinder.TimeManager.OnPostTick -= Sample;
        if (writer != null) writer.Dispose();
    }

    [Serializable] private class Command { public int id, frames; public string key, request; public float axis; }
    [Serializable] private class PaddleSample { public int side, owner, rejected; public Vector3 position; }
    [Serializable] private class SampleData
    {
        public double time, tickDelta;
        public uint tick;
        public long rtt;
        public int frame, localSide, left, right;
        public bool server, client, inPlay;
        public Vector3 ball;
        public PaddleSample[] paddles;
    }
}
#endif
