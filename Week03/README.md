# Week 03 - Pong connects and replicates

Written explanations are prepared below. Predictions are example reasoning prepared with the implementation, **not** a record of predictions made before Step 6. Replace them with your own original predictions if you recorded those. Real tests and measurements are explicitly pending.

## Checkpoint D

1. **ObjectId:** FishNet's NetworkObject gives the spawned paddle its network identity; the server assigns its ObjectId when spawning it.
2. **Who chose LEFT?** PaddleController.OnStartServer on the server chooses the first free side. The client learns it through the server-written `SyncVar<int> Side`, including its initial value in the spawn data, and Place applies position/colour.
3. **Owner guard:** `if (!IsOwner) return;` is the first line of PaddleController.Update. Other clients cannot run this paddle's local keyboard movement.

## Predictions and grades

| Question | Prepared reasoning | Experimental grade |
| --- | --- | --- |
| W moves locally, nowhere else: who tells whom? | The owning client sends its paddle position to the server; the server forwards it to observers; they interpolate their remote copy. | Right for the prepared expectation: native W moved only LEFT and the guest received it |
| Who owns the real ball, what reaches clients? | The server spawns and moves the one ball; clients receive its spawned NetworkObject, position updates and InPlay state. | Right for the prepared expectation: one server ball reached each client |
| Trusting client z = 3.2: first cheat? | A modified client can teleport its paddle or exceed the speed limit by sending fabricated positions. | Right as a code-level trust analysis; no malicious-client demo claimed |

## Connection card - Step 11 after Step 6

Run both directions over the same phone hotspot. Read actual ClientId, side and RTT from the HUD. A single-machine test does not satisfy this card.

| Direction | Partner | Network | Server hotspot IP | My ClientId + side | RTT (ms) | First try? / fix |
| --- | --- | --- | --- | --- | --- | --- |
| My laptop hosts; partner joins | Pending | Pending | Pending | Pending | Pending | Not tested |
| Partner hosts; I join | Pending | Pending | Pending | Pending | Pending | Not tested |

Find the hotspot interface IP on macOS with `ipconfig getifaddr en0` (use the actual connected interface if different). Server host: Start Server then Start Client. Guest: enter the server IP, then Start Client. Wrong IP, different networks or a blocked UDP 7770 port can prevent connection.

## Design sheet

| Thing | State or event? | FishNet piece | On which object? | Who writes / calls? |
| --- | --- | --- | --- | --- |
| Ball position | State | NetworkTransform | Ball | Server |
| Ball in play? | State | SyncVar<bool> | Ball | Server |
| Two scores | State | SyncVar<int> | GameState | Server |
| Whose serve | State | SyncVar<int> | GameState | Server |
| Let me serve | Event | ServerRpc | Paddle | Owner client |
| LEFT scored, to all | Event | ObserversRpc | Scorer paddle | Server |
| YOU SERVE, to one | Event | TargetRpc | Conceder paddle | Server |
| Event feed | State | SyncList<string> | GameState | Server |

BallSpawner on Court creates the one ball in **OnStartServer**, at `(0, Court.BallY, 0)`, then calls Spawn. The first line in NetworkBall.Update is **`if (!IsServerInitialized) return;`**, so client copies do not simulate the ball.

Separate design record: [commit b9f6fc3](https://github.com/unitybtw/dgd0005-siracsimsek/commit/b9f6fc3). It was pushed during final preparation, after the original implementation already existed. It does not satisfy the historical instruction to push before Step 7. The student should send this link to the instructor; no instructor message has been sent.

## Step 8

1. A hit sound for all observing clients uses an **ObserversRpc**. A "not your serve" message to one client uses a **TargetRpc**, with that client's NetworkConnection as the target. The supplied game uses ServerRpc RequestServe, ObserversRpc AnnounceGoal and TargetRpc YouServe.
2. Scores live in GameState's LeftScore and RightScore SyncVars. Only the server changes them, in Goal. A late joiner receives the current state rather than replaying old goal RPCs.
3. Collision uses the server's replicated copy of the paddle. A client sees its own paddle immediately, but its update reaches the server later and other copies may interpolate. That timing difference can make the client picture differ from the server's collision decision.

## Step 9 - event feed

GameState.Feed is a SyncList, written only on the server. Join, leave, serve and goal append entries. The oldest entry is removed whenever there are more than six. A late joiner receives the retained six entries as state. The HUD can show a goal flash and YOU SERVE together without one overwriting the other.

## Step 10 - bytes and tick rate

StatisticsManager uses Development mode with client and server updates enabled. The HUD accumulates actual FishNet socket byte counters over a one-second sampling window and divides by elapsed time; these are application/transport bytes, not total Wi-Fi link overhead.

Stop all instances before changing TimeManager.Tick Rate. Use the same server/client count and a sustained rally; after at least ten seconds, read server OUT. Test 10, 60 and 30 Hz, then leave the scene at **30 Hz**. Do not substitute estimates for observations.

| Tick rate | Server OUT B/s (rally) | Ball in client window | Other paddle |
| --- | --- | --- | --- |
| 10 | 342.8 mean; 223.9 first reading after 10 s | 100 ms network interval; interpolation spans about 200 ms | Same interval for the remote paddle |
| 30 | 900.6 mean; 1168.2 first reading after 10 s | 33.3 ms network interval; interpolation spans about 66.7 ms | More frequent remote updates |
| 60 | 1701.6 mean; 2505.9 first reading after 10 s | 16.7 ms network interval; interpolation spans about 33.3 ms | Most frequent remote updates |

Measured setup: one local Development-build listen host and one guest, automatic owner paddle tracking at speed 8, frame cap 60, 32-second runs, continuous rallies with no goals. The mean uses samples after at least ten seconds of uninterrupted rally; 40/42/42 samples for 10/60/30 Hz. Raw samples are in `verification/benchmarks/`. The client-window descriptions above are timing consequences, not claims of a human visual comparison of these headless runs.

Explanation: 60 Hz sends changed state more often and normally costs more bandwidth; it buys more frequent updates and lower delay between samples. The ball can still look stepped to the paddle owner because that owner controls only their paddle; the ball is simulated on the server and drawn from incoming sampled updates. In these runs 60 Hz cost about 1.89 times the mean server traffic of 30 Hz and 4.96 times that of 10 Hz; it bought more frequent samples. The ratios are observed results for this setup, not universal constants.

## Notes Section 14 - self-review answers

Source: the supplied Week03 lecture notes, Sections 1-7 and 14. These are prepared explanations to study and explain at the demo.

1. **No shared now:** every message takes time to travel and queue, so a remote machine's newest known state is already in the past. The notes give Istanbul-Frankfurt about 1,900 km: a 9.5 ms physical minimum one way, 19 ms round trip, and a typical measured RTT of 35-50 ms. At 60 fps (16.7 ms/frame), that is about 0.57 frames one way at the minimum, 1.14 frames for the minimum RTT, and 2.1-3.0 frames for the typical RTT. These are lecture estimates, not measurements from our laptop.

2. **Topology comparison:**

   | Property | Dedicated server | Listen server | Peer-to-peer |
   | --- | --- | --- | --- |
   | Trust | Neutral server decides | Host player decides | Peers agree by protocol |
   | Cost | Operator funds servers | Host pays upload/CPU | Peers pay their own resources |
   | Latency | All clients reach server | Host local; guests delayed | Direct peer links if NAT permits |

   Block 1 teaches a dedicated authoritative server. In Step 5, the Server-tagged Player 2 process is the server-only process, while the other three are clients. Step 11's hosting laptop runs the server; if Start Client is also pressed in that same process, it is operationally a listen host running both roles. Our recorded local benchmark uses that listen-host arrangement plus one guest; it is not a claim that the prescribed separate Player 2 setup was already tested.

3. **UDP versus TCP:** TCP's ordered stream can delay a fresh position behind a missing older packet; for fast-changing motion, the newest sample is more useful than a retransmitted stale sample. UDP lets the networking library choose delivery guarantees per message. A serve request or goal notification should be reliable; frequent NetworkTransform positions can be unreliable because newer positions supersede old ones.

4. **Identity and script capability:** NetworkObject gives an object network identity and ownership so each machine can refer to the same spawned object. NetworkBehaviour gives its script the networking callbacks, ownership/initialization checks, SyncTypes and RPC support. Hud has neither: ConnectionHud is a local MonoBehaviour that reads already replicated GameState and draws local UI; the UI object itself is not replicated.

5. **One paddle owned by client 0:** on a separate server, IsOwner is false and IsServerInitialized is true, and OnStartServer runs. On client 0, IsOwner is true and IsServerInitialized is false, and OnStartClient runs; only this copy reads the keyboard. On client 1, both are false and OnStartClient runs for the observed copy, so its keyboard movement returns immediately. If client 0 is also the listen host, its single object has both flags true and both start callbacks; the server/client roles share one process in that case.

6. **Replication of movement:** NetworkTransform made the difference. Position travels owning client -> server -> observing client. Client Authoritative on trusts the paddle owner this week; interpolation smooths the received samples. It is off on the Ball prefab because the server simulates the ownerless ball, so a client must not decide its position.

7. **Three local balls versus one network ball:** Court.Awake ran separately in every process with Local Ball enabled, so each process created its own unshared LocalBall. BallSpawner.OnStartServer runs only on the server, which creates and spawns one NetworkBall for clients to observe; Local Ball is disabled in Pong. Calling Spawn as an ordinary client does not authorize a shared spawn: FishNet rejects/warns about the server-only operation rather than accepting that client's object into the server's world.

8. **State/event choices:**

   | Fact or occurrence | Kind | Implementation |
   | --- | --- | --- |
   | Score | State | GameState SyncVars |
   | LEFT scored | Event | ObserversRpc |
   | Whose serve | State | ServingSide SyncVar |
   | You serve | Event | TargetRpc |
   | Hit sound | Event | ObserversRpc |
   | Ball in play | State | InPlay SyncVar |
   | Ball position | State | NetworkTransform; not a manually written position RPC |

9. **Conflicting hits:** the server decides collisions using its own replicated paddle copies and its own ball. An owning client can show an apparently valid touch that the server did not see yet; the later authoritative ball update may look like a miss or correction on that client's screen. Writing InPlay.Value locally would neither authorize a serve nor set the server's velocity; it can produce a wrong local picture. The owner must send RequestServe and let the server validate the turn and write shared state.

10. **Bandwidth arithmetic:** 8 objects x 30 ticks/s x 40 bytes = **9,600 B/s per client** for positions; with 4 clients the server sends **38,400 B/s** in total, before other traffic and transport overhead. Our measured tick-rate results are in Step 10 below: lower tick rate sent less traffic. Your own paddle is moved immediately in its owner's Update, so lowering the network tick rate affects remote copies and the server-driven ball rather than the owner's local movement.

## Notes Section 13 - vocabulary practice

| Term | Explanation |
| --- | --- |
| Topology | The arrangement of network roles and connections, including who is trusted to decide state. |
| Dedicated server | A separate authoritative server process with no local player. |
| Listen server | A player's process that also runs the authoritative server. |
| Peer-to-peer | Players connect to peers and coordinate state without a separate central game server. |
| Client | A process that connects to the server and displays or requests game actions. |
| Host | A process running server and client roles together. |
| Connection | The tracked communication relationship between a client and server. |
| ClientId | The server-assigned identifier of a connection, not an object's identity. |
| Tick | One iteration of network timing, sending, receiving and related simulation work. |
| Tick rate | How many network ticks occur per second. |
| Packet | A unit of data sent over the network. |
| UDP versus TCP | UDP sends datagrams without an ordered reliable stream; TCP provides an ordered reliable byte stream. |
| Reliable / unreliable | Reliable delivery retries/acknowledges important messages; unreliable delivery allows stale samples to be lost. |
| RTT | The time for a message to travel out and a response to return. |
| Ping | A latency probe, often shown as its round-trip time. |
| Bandwidth | The amount of network data transferred per unit of time. |
| NetworkManager | FishNet's central component coordinating server, client, timing, scenes and transport. |
| Transport | The socket/protocol layer carrying FishNet data; this project uses Tugboat over UDP. |
| NetworkObject | The component identifying a networked game object and tracking its ownership. |
| ObjectId | The identifier assigned to a particular spawned NetworkObject. |
| NetworkBehaviour | A script base class enabling FishNet callbacks, RPCs and synchronized state. |
| Scene object | A network object saved into a scene, identified by a SceneId rather than created from a spawn prefab. |
| Prefab | A reusable object asset used to instantiate matching objects. |
| Spawn | Register an instantiated network object on the server and send its existence to observers. |
| PlayerSpawner | Spawns one configured player prefab for each connected client and assigns that client ownership. |
| Owner | The connection assigned control of a NetworkObject. |
| Ownership | The relationship associating that object with its controlling connection. |
| Observer | A connection allowed to receive an object's spawn and replicated updates. |
| Replication | Copying relevant authoritative state to other machines over time. |
| State versus event | State describes what is true now; an event describes an occurrence or request. |
| SyncVar | A synchronized current value normally written by the server, including initial state for late joiners. |
| SyncList | A synchronized collection whose operations and current contents reach clients. |
| OnChange | A callback reacting to a synchronized value change or its received value. |
| NetworkTransform | A component transmitting transform changes and smoothing received motion. |
| Client-authoritative | The owning client is trusted to originate a particular state, such as paddle position this week. |
| Interpolation | Drawing intermediate values between received state samples to smooth motion. |
| ServerRpc | A client-to-server request/call, normally restricted to the object's owner. |
| ObserversRpc | A server-to-observers call for an event everyone observing the object should receive. |
| TargetRpc | A server call delivered to one specified client connection. |
| RequireOwnership | The ServerRpc setting determining whether only the object's owner may call it. |
| Serialization | Encoding values into bytes so another process can decode the same data. |
| Interest management | Selecting which objects and updates each client needs to observe. |

Practise describing two terms aloud and pointing to the actual script line. Prepared written definitions do not claim the oral demo has happened.

## Explain the code - demo study guide

- NetworkObject: identifies an object across machines; NetworkBehaviour: script with networking lifecycle and RPC/SyncType support.
- Ownership: tells which connection controls a spawned object; authority: tells which machine may decide a specific state.
- SyncVar: current replicated value; SyncList: replicated collection; RPC: a remote call representing an event.
- ServerRpc: owner requests serve, server validates turn and player availability; ObserversRpc: server announces a goal to observers; TargetRpc: server sends YOU SERVE to the conceder only.
- NetworkTransform: transports transform samples; interpolation: draws motion between received samples; RTT: round trip time; tick: one network timing step.
- OnStartServer: server-only network startup; OnStartClient: local client startup of the spawned copy; OnStop callbacks remove stale paddle registrations.

These definitions help practise the demo but are not a substitute for the exact Section 13 vocabulary list.

## Real validation checklist

- [x] Editor imports with no compilation errors; setup creates Pong-Start, Pong, Paddle and Ball assets.
- [x] Pong-Start: W/S, arrows, bounces and [Local] goal logs.
- [x] Pong: manager has PlayerSpawner, ServerBootstrap, logger; Start On Headless off; UDP 7770; 30 Hz; traffic stats on.
- [x] Paddle prefab has NetworkObject + client-authoritative NetworkTransform + PaddleController; interpolation 2; rotation/scale off; no collider; no scene copy.
- [x] Ball prefab has NetworkObject + server-authoritative NetworkTransform + NetworkBall; no collider; Court localBall off; BallSpawner reference set.
- [x] GameState has NetworkObject + GameState; both prefabs are in DefaultPrefabObjects.
- [x] Multiplayer Play Mode: Server tag on Player 2; main/Player 3/Player 4 start clients; LEFT/RIGHT/SPECTATOR; owner-only movement; only one ball.
- [x] Wrong-turn serve RPC ignored; right-side Space starts ball on every client.
- [x] Goal updates every score; goal flash everywhere; YOU SERVE only to conceder.
- [x] Rejoin sees existing score and last six feed entries; vacated side reused.
- [x] Server stop/restart starts a fresh match with zero scores and one ball.
- [ ] Both directions of hotspot card completed; second meeting practises rally, goal and reconnect.
- [x] Measured tick-rate table, prepared expectation grades and Section 14 answers complete.
- [x] Current GitHub design/source/report commits pushed with actual links.
- [ ] Original per-step chronology and student sending the design link to the instructor (not fabricated).
- [x] Clean Git archive has no Library, Temp, Logs or Builds; tick rate 30; both READMEs reviewed.

## Verified local run

An Editor host plus separate Development-build RIGHT/spectator clients passed the core replication/RPC checks; see `verification/results.json`. Owner movement and serve were driven with QA menu calls, not physical keyboard input. Reconnect retained 1:0 and six feed entries; a later targeted test also verified a new client keeps the existing paddle z position. Traffic counters are enabled and visibly nonzero. The physical-laptop cards remain pending. The tick-rate table and lecture answers are complete. Multiplayer Play Mode also verified Server-tagged Player 2 plus Main/Player 3/Player 4 roles in four separate processes.

## Visual evidence and runtime captures

The screenshots below provide direct visual proof of local multiplayer execution, active socket traffic monitoring, goal/RPC handling, and reconnect state recovery:

### 1. Active Match Rally & Traffic Measurement
![Active Rally and Traffic Measurement](../Pong/Captures/pong-traffic-verified.png)
*Active rally in progress: white ball moving across the court, LEFT (blue) and RIGHT (orange) paddles positioned, and real-time socket statistics reporting `Server IN 31 / OUT 762 B/s`, `Client IN 369 / OUT 21 B/s` at 30 Hz.*

### 2. Goal Replication, YOU SERVE TargetRpc & Reconnecting Player
![Goal Replication and Reconnection](../Pong/Captures/pong-verified.png)
*Score replication and state recovery: score updated to `LEFT 1 : 0 RIGHT`, conceder notified with `Serve: RIGHT (SPACE)`, and event feed tracking the full connection and scoring lifecycle. When a player disconnected and a new client connected, the match score (1:0) and recent feed entries were accurately preserved.*

### 3. Spectator & 4-Player Role Distribution
![Spectator and Multi-Client Roles](../Pong/Captures/pong-host.png)
*Multiplayer role assignment: client 0 assigned to LEFT, client 1 assigned to RIGHT, and client 2 assigned to SPECTATOR (renderer disabled, spectator HUD).*

