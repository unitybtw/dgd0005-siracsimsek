using System;
using System.Collections;
using System.Linq;
using FishNet;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Multiplayer.PlayMode;
#endif

public class ServerBootstrap : MonoBehaviour
{
    private IEnumerator Start()
    {
        // ConnectionLogger.Start must subscribe before the server starts.
        yield return null;
        var args = Environment.GetCommandLineArgs();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        int tickFlag = Array.IndexOf(args, "-pongTickRate");
        if (tickFlag >= 0 && tickFlag + 1 < args.Length && ushort.TryParse(args[tickFlag + 1], out ushort tickRate)
            && (tickRate == 10 || tickRate == 30 || tickRate == 60))
            InstanceFinder.TimeManager.SetTickRate(tickRate);
#endif
        bool server = Array.IndexOf(Environment.GetCommandLineArgs(), "-pongServer") >= 0;
#if UNITY_EDITOR
        server |= CurrentPlayer.Tags.Contains("Server");
#endif
        if (server && InstanceFinder.NetworkManager != null)
            InstanceFinder.ServerManager.StartConnection();
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pongClient") >= 0)
            InstanceFinder.ClientManager.StartConnection();
    }
}
