using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Component.Transforming;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

public class PaddleController : NetworkBehaviour
{
    public readonly SyncVar<int> Side = new SyncVar<int>(2);
    public static readonly List<PaddleController> All = new List<PaddleController>();
    public static PaddleController Local { get; private set; }
    [SerializeField] private float speed = 8f;
    private float _serverAxis;
    private const double MinInputGap = 0.030;
    private double _lastInputTime = -1;
    public int RejectedInputs;
    private System.Func<string> rejectedPanel;
    private NetworkTransform networkTransform;
    private uint? _inputSubmittedForTick;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _inputSubmittedForTick = null;
        TimeManager.OnTick += OnTick;
    }
    public override void OnStopNetwork()
    {
        TimeManager.OnTick -= OnTick;
        base.OnStopNetwork();
    }
    private void Awake()
    {
        Side.OnChange += SideChanged;
        networkTransform = GetComponent<NetworkTransform>();
    }
    private void OnDestroy()
    {
        Side.OnChange -= SideChanged;
        RemoveRejectedPanel();
    }
    private void RemoveRejectedPanel()
    {
        if (rejectedPanel != null) ConnectionHud.Panels.Remove(rejectedPanel);
        rejectedPanel = null;
    }
    private void Register() { if (!All.Contains(this)) All.Add(this); }

    public override void OnStartServer()
    {
        base.OnStartServer();
        _serverAxis = 0f;
        _lastInputTime = -1;
        RejectedInputs = 0;
        RemoveRejectedPanel();
        rejectedPanel = () => $"Rejected inputs (paddle {OwnerId}): {RejectedInputs}";
        ConnectionHud.Panels.Add(rejectedPanel);
        bool left = All.Exists(p => p != null && p.IsServerInitialized && p.Side.Value == 0);
        bool right = All.Exists(p => p != null && p.IsServerInitialized && p.Side.Value == 1);
        Side.Value = !left ? 0 : !right ? 1 : 2;
        Register();
        Place();
        Debug.Log($"[Server] client {Owner.ClientId} assigned {SideName(Side.Value)}");
        GameState.Announce($"Client {Owner.ClientId} joined as {SideName(Side.Value)}");
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        networkTransform.OnDataReceived += CorrectVisualDrift;
        Register();
        if (IsOwner)
        {
            Local = this;
            Debug.Log($"[Client] I am client {Owner.ClientId}, side {SideName(Side.Value)}");
        }
        Place();
    }
    public override void OnStopServer()
    {
        RemoveRejectedPanel();
        _serverAxis = 0f;
        GameState.Announce($"Client {Owner.ClientId} left {SideName(Side.Value)}");
        All.Remove(this);
        if (Side.Value < 2 && NetworkBall.Instance != null) NetworkBall.Instance.ResetToCentre();
        base.OnStopServer();
    }
    public override void OnStopClient()
    {
        networkTransform.OnDataReceived -= CorrectVisualDrift;
        All.Remove(this);
        if (Local == this) Local = null;
        base.OnStopClient();
    }
    private void SideChanged(int previous, int next, bool asServer) => Place();
    private void CorrectVisualDrift(NetworkTransform.TransformData previous, NetworkTransform.TransformData next)
    {
        // A local teleport changes presentation only. Snap large errors to an actual server snapshot.
        if (!IsServerInitialized && Vector3.Distance(transform.localPosition, next.Position) > 1f)
            transform.localPosition = next.Position;
    }
    public static string SideName(int side) => side == 0 ? "LEFT" : side == 1 ? "RIGHT" : "SPECTATOR";
    private void Place()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = Side.Value < 2;
        if (Side.Value >= 2) return;
        // Keep the z from spawn/NetworkTransform, including a late joiner's current position.
        if (IsServerInitialized)
            transform.position = new Vector3(Side.Value == 0 ? -Court.PaddleX : Court.PaddleX, 0.5f, transform.position.z);
        Court.Paint(gameObject, Side.Value == 0 ? new Color(0.35f, 0.65f, 1f) : new Color(1f, 0.60f, 0.25f));
    }
    private void Update()
    {
        if (!IsOwner) return;
        var kb = Keyboard.current;
        if (kb == null || ConnectionHud.EditingAddress) return;
        if (kb.cKey.wasPressedThisFrame)
        {
            for (int i = 0; i < 20; i++) ServerSetAxis(1f);
            _inputSubmittedForTick = TimeManager.LocalTick;
        }
        else if (kb.xKey.wasPressedThisFrame)
        {
            ServerSetAxis(50f);
            _inputSubmittedForTick = TimeManager.LocalTick;
        }
        if (kb.tKey.wasPressedThisFrame && NetworkBall.Instance != null && IsClientInitialized && !IsServerInitialized)
            transform.position = NetworkBall.Instance.transform.position;
        if (Side.Value >= 2) return;
        if (kb.spaceKey.wasPressedThisFrame) RequestServe();
    }
    private void OnTick()
    {
        if (IsOwner && Side.Value < 2)
        {
            var kb = Keyboard.current;
            float axis = 0f;
            if (kb != null && !ConnectionHud.EditingAddress)
                axis = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                     - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
            // C/X already supplied this local tick's input in Update. Never omit a later tick.
            // Otherwise send the keyboard axis, including zero so releasing a key stops movement.
            if (_inputSubmittedForTick != TimeManager.LocalTick)
            {
                ServerSetAxis(axis);
                _inputSubmittedForTick = TimeManager.LocalTick;
            }
        }
        if (IsServerInitialized)
        {
            transform.position += Vector3.forward * _serverAxis * speed * (float)TimeManager.TickDelta;
            Court.ClampPaddle(transform);
            // FishNet normally suppresses idle snapshots. Keep correcting even an idle client's T cheat.
            networkTransform.ForceSend();
        }
    }
    [ServerRpc]
    private void ServerSetAxis(float axis)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        PongHomework2Probe.RecordInput(this, axis);
#endif
        if (float.IsNaN(axis)) return;
        axis = Mathf.Clamp(axis, -1f, 1f);
        if (Side.Value >= 2) return;
        double now = TimeManager.TicksToTime(TimeManager.Tick);
        if (_lastInputTime >= 0 && now - _lastInputTime < MinInputGap)
        {
            RejectedInputs++;
            Debug.Log($"[Server] Rejected input from client {OwnerId}: too fast.");
            return;
        }
        _lastInputTime = now;
        _serverAxis = axis;
    }
    [ServerRpc]
    public void RequestServe()
    {
        var state = GameState.Instance;
        var ball = NetworkBall.Instance;
        if (state == null || ball == null || Side.Value >= 2 || Side.Value != state.ServingSide.Value || ball.InPlay.Value) return;
        if (!All.Exists(p => p != null && p.IsServerInitialized && p.Side.Value == 0)
            || !All.Exists(p => p != null && p.IsServerInitialized && p.Side.Value == 1)) return;
        ball.Serve(1 - Side.Value);
        GameState.Announce($"{SideName(Side.Value)} served");
    }
    [ObserversRpc]
    public void AnnounceGoal(int left, int right) => ConnectionHud.Flash($"{SideName(Side.Value)} scored! {left} : {right}");
    [TargetRpc]
    public void YouServe(NetworkConnection target) => ConnectionHud.Flash("YOU SERVE");
}
