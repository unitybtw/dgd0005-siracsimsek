using System;
using System.Threading.Tasks;
using FishNet.Managing;
using FishNet.Transporting.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Multiplayer.PlayMode;
#endif

public static class RelayLink
{
    public static async Task EnsureSignedInAsync()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            var options = new InitializationOptions();
            var args = Environment.GetCommandLineArgs();
            int profileIdx = Array.IndexOf(args, "-pongProfile");
            if (profileIdx >= 0 && profileIdx + 1 < args.Length)
                options.SetProfile(args[profileIdx + 1]);
#if UNITY_EDITOR
            else if (!CurrentPlayer.IsMainEditor)
                options.SetProfile("guest");
#endif
            await UnityServices.InitializeAsync(options);
        }
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public static async Task<string> HostAsync(NetworkManager manager, int maxConnections = 3)
    {
        if (manager == null) throw new InvalidOperationException("NetworkManager missing.");
        var utp = manager.TransportManager.GetTransport<UnityTransport>();
        if (utp == null) throw new InvalidOperationException("UnityTransport is not on the NetworkManager object.");

        await EnsureSignedInAsync();
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        utp.SetRelayServerData(allocation.ToRelayServerData("dtls"));
        Debug.Log($"[Relay] Join code: {joinCode}");

        manager.ServerManager.StartConnection();
        manager.ClientManager.StartConnection();
        return joinCode;
    }

    public static async Task JoinAsync(NetworkManager manager, string joinCode)
    {
        if (manager == null) throw new InvalidOperationException("NetworkManager missing.");
        string cleaned = (joinCode ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(cleaned)) throw new ArgumentException("Join code is empty.");
        var utp = manager.TransportManager.GetTransport<UnityTransport>();
        if (utp == null) throw new InvalidOperationException("UnityTransport is not on the NetworkManager object.");

        await EnsureSignedInAsync();
        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(cleaned);

        utp.SetRelayServerData(allocation.ToRelayServerData("dtls"));
        Debug.Log($"[Relay] Joining allocation with code: {cleaned}");
        manager.ClientManager.StartConnection();
    }
}
