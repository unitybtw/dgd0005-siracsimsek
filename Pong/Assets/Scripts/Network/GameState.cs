using System;
using System.Text;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class GameState : NetworkBehaviour
{
    public static GameState Instance { get; private set; }
    public readonly SyncVar<int> LeftScore = new SyncVar<int>();
    public readonly SyncVar<int> RightScore = new SyncVar<int>();
    public readonly SyncVar<int> ServingSide = new SyncVar<int>(0);
    public readonly SyncList<string> Feed = new SyncList<string>();
    private Func<string> panel;
    private void Awake() => Instance = this;
    private void Start() { panel = Status; ConnectionHud.Panels.Add(panel); }
    public override void OnStartServer()
    {
        base.OnStartServer();
        LeftScore.Value = RightScore.Value = 0;
        ServingSide.Value = 0;
        Feed.Clear();
        Announce("Match ready; waiting for two players");
    }
    private void OnDestroy()
    {
        if (panel != null) ConnectionHud.Panels.Remove(panel);
        if (Instance == this) Instance = null;
    }
    public void Goal(int scorerSide)
    {
        if (!IsServerInitialized || scorerSide < 0 || scorerSide > 1) return;
        if (scorerSide == 0) LeftScore.Value++; else RightScore.Value++;
        ServingSide.Value = 1 - scorerSide;
        Announce($"{PaddleController.SideName(scorerSide)} scored! {LeftScore.Value} : {RightScore.Value}");
        foreach (var paddle in PaddleController.All)
        {
            if (paddle == null || !paddle.IsServerInitialized) continue;
            if (paddle.Side.Value == scorerSide) paddle.AnnounceGoal(LeftScore.Value, RightScore.Value);
            if (paddle.Side.Value == ServingSide.Value && paddle.Owner.IsActive) paddle.YouServe(paddle.Owner);
        }
    }
    public static void Announce(string line)
    {
        if (Instance == null || !Instance.IsServerInitialized) return;
        Instance.Feed.Add(line);
        while (Instance.Feed.Count > 6) Instance.Feed.RemoveAt(0);
        Debug.Log($"[Server] {line}");
    }
    private string Status()
    {
        var obj = GetComponent<NetworkObject>();
        if (!obj.IsClientInitialized && !obj.IsServerInitialized) return "Start a server or connect to a host.";
        var text = new StringBuilder($"LEFT  {LeftScore.Value} : {RightScore.Value}  RIGHT\nServe: {PaddleController.SideName(ServingSide.Value)} (SPACE)\n\nRECENT EVENTS\n");
        foreach (string entry in Feed) text.AppendLine(entry);
        return text.ToString();
    }
}
