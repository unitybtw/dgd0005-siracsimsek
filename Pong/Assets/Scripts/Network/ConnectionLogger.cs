using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;

public class ConnectionLogger : MonoBehaviour
{
    private NetworkManager manager;
    private void Start()
    {
        manager = InstanceFinder.NetworkManager;
        if (manager == null) { Debug.LogError("[Server] NetworkManager missing."); return; }
        manager.ServerManager.OnServerConnectionState += ServerState;
        manager.ServerManager.OnRemoteConnectionState += RemoteState;
        manager.ClientManager.OnClientConnectionState += ClientState;
    }
    private void ServerState(ServerConnectionStateArgs args) => Debug.Log($"[Server] {args.ConnectionState}");
    private void ClientState(ClientConnectionStateArgs args)
    {
        Debug.Log($"[Client] {args.ConnectionState}");
        // In a host, the client joined a server in the same process, so its RTT is 0.
        if (args.ConnectionState == LocalConnectionState.Started && manager.ServerManager.Started)
            Debug.Log("[Host] This process is the server AND a client. RTT is 0 ms.");
    }
    private void RemoteState(NetworkConnection conn, RemoteConnectionStateArgs args) =>
        Debug.Log($"[Server] client {conn.ClientId} {args.ConnectionState}; clients {manager.ServerManager.Clients.Count}");
    private void OnDestroy()
    {
        if (manager == null) return;
        manager.ServerManager.OnServerConnectionState -= ServerState;
        manager.ServerManager.OnRemoteConnectionState -= RemoteState;
        manager.ClientManager.OnClientConnectionState -= ClientState;
    }
}
