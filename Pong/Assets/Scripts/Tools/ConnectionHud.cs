using System;
using System.Collections.Generic;
using System.Reflection;
using FishNet;
using FishNet.Editing.NetworkProfiler;
using FishNet.Managing;
using FishNet.Managing.Statistic;
using UnityEngine;

public class ConnectionHud : MonoBehaviour
{
    public static readonly List<Func<string>> Panels = new List<Func<string>>();
    public static bool EditingAddress { get; private set; }
    private static readonly List<string> flashes = new List<string>();
    private static float flashUntil;
    private NetworkManager manager;
    private NetworkTrafficStatistics statistics;
    private string address = "127.0.0.1";
    private double serverIn, serverOut, clientIn, clientOut;
    public double ServerOutBytesPerSecond => serverOut;
    private double si, so, ci, co;
    private float sampleAt;
    private GUIStyle title, body;
    private Vector2 scroll;
    // FishNet 4.7.3 exposes the statistics event but keeps its traffic totals internal.
    private static readonly FieldInfo inbound = typeof(BidirectionalNetworkTraffic).GetField("InboundTraffic", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo outbound = typeof(BidirectionalNetworkTraffic).GetField("OutboundTraffic", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo bytes = inbound.FieldType.GetField("Bytes");
    private void Start()
    {
        manager = InstanceFinder.NetworkManager;
        sampleAt = Time.unscaledTime;
        if (manager != null && manager.StatisticsManager.TryGetNetworkTrafficStatistics(out statistics))
            statistics.OnNetworkTraffic += Traffic;
    }
    private static double Total(BidirectionalNetworkTraffic traffic, FieldInfo direction)
    {
        if (traffic == null) return 0;
        object data = direction.GetValue(traffic);
        return data == null ? 0 : Convert.ToDouble(bytes.GetValue(data));
    }
    private void Traffic(uint tick, BidirectionalNetworkTraffic server, BidirectionalNetworkTraffic client)
    {
        si += Total(server, inbound); so += Total(server, outbound);
        ci += Total(client, inbound); co += Total(client, outbound);
    }
    private void Update()
    {
        float elapsed = Time.unscaledTime - sampleAt;
        if (elapsed < 1f) return;
        serverIn = si / elapsed; serverOut = so / elapsed;
        clientIn = ci / elapsed; clientOut = co / elapsed;
        si = so = ci = co = 0; sampleAt = Time.unscaledTime;
    }
    public static void Flash(string message)
    {
        Debug.Log($"[Client] Flash: {message}");
        if (Time.unscaledTime >= flashUntil) flashes.Clear();
        flashes.Add(message);
        while (flashes.Count > 3) flashes.RemoveAt(0);
        flashUntil = Time.unscaledTime + 3f;
    }
    private void OnGUI()
    {
        if (manager == null) return;
        if (title == null)
        {
            title = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
            body = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
        }
        float scale = Mathf.Clamp(Screen.width / 1280f, 0.7f, 1.5f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        GUILayout.BeginArea(new Rect(16, 16, 300, Screen.height / scale - 32), GUI.skin.box);
        scroll = GUILayout.BeginScrollView(scroll);
        GUILayout.Label("PONG / NETWORK LAB", title);
        GUILayout.Label($"Server: {(manager.IsServerStarted ? "ONLINE" : "OFFLINE")}   Client: {(manager.IsClientStarted ? "ONLINE" : "OFFLINE")}", body);
        GUILayout.Label("Host IP / localhost", body);
        GUI.SetNextControlName("HostAddress");
        GUI.enabled = !manager.IsClientStarted;
        address = GUILayout.TextField(address, 100);
        GUI.enabled = true;
        EditingAddress = GUI.GetNameOfFocusedControl() == "HostAddress";
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(manager.IsServerStarted ? "Stop Server" : "Start Server", GUILayout.Height(32)))
        {
            GUI.FocusControl(null);
            if (manager.IsServerStarted) manager.ServerManager.StopConnection(true);
            else manager.ServerManager.StartConnection();
        }
        if (GUILayout.Button(manager.IsClientStarted ? "Stop Client" : "Start Client", GUILayout.Height(32)))
        {
            GUI.FocusControl(null);
            if (manager.IsClientStarted) manager.ClientManager.StopConnection();
            else { manager.TransportManager.Transport.SetClientAddress(address.Trim()); manager.ClientManager.StartConnection(); }
        }
        GUILayout.EndHorizontal();
        var local = PaddleController.Local;
        GUILayout.Label(local == null ? "You: not connected" : $"You: client {local.Owner.ClientId} / {PaddleController.SideName(local.Side.Value)}", body);
        GUILayout.Label($"RTT {manager.TimeManager.RoundTripTime} ms   Tick {manager.TimeManager.LocalTick}\nRate {manager.TimeManager.TickRate} Hz / UDP 7770", body);
        GUILayout.Label($"Server IN {serverIn:F0} / OUT {serverOut:F0} B/s\nClient IN {clientIn:F0} / OUT {clientOut:F0} B/s", body);
        GUILayout.Space(12);
        foreach (var panel in Panels) GUILayout.Label(panel(), body);
        GUILayout.Space(10);
        GUILayout.Label("MOVE  W/S or arrows\nSERVE  Space (your turn)\nTwo players required. Extra clients spectate.", body);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
        if (Time.unscaledTime < flashUntil)
            GUI.Box(new Rect(Screen.width / scale / 2 - 240, 24, 480, 80), string.Join("\n", flashes), title);
        GUI.matrix = Matrix4x4.identity;
    }
    private void OnDestroy()
    {
        if (statistics != null) statistics.OnNetworkTraffic -= Traffic;
        EditingAddress = false;
        flashes.Clear();
    }
}
