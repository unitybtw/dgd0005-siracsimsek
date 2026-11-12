# Week 5 - Pong on a host

In-class assignment: the Homework 1/2 Pong project now runs as a **listen host**. One process starts the server and joins it as client 0. A second process is the guest; it connects to an address and a port exactly as it did in Week 3.

- Unity **6000.3.2f1**, FishNet **4.7.3R**, Tugboat UDP **7770**, tick rate **30**.
- Code: `Pong/Assets/Scripts/Tools/ConnectionHud.cs`, `Pong/Assets/Scripts/Network/ConnectionLogger.cs`, `Pong/Assets/Scripts/Network/PaddleController.cs`.
- Evidence: `verification/week05/summary.json` and `verification/week05/*-smoke.jsonl` (raw `.log`/probe traces are machine logs and stay out of Git).

## What changed

1. **ConnectionHud:** a `Start Host` button next to `Start Server`, visible only when both the server and the client are stopped. It calls `ServerManager.StartConnection()` and then `ClientManager.StartConnection("127.0.0.1", port)`. When both are started, one `Stop Host` button stops the client first, then the server.
2. **ConnectionHud:** the HUD prints `Paddles in All: <n>` so the Step 3 duplicate-list bug is visible.
3. **ConnectionLogger:** `OnClientState` still logs every client state. When the client reaches `Started` and `ServerManager.Started` is also true, it logs `[Host] This process is the server AND a client. RTT is 0 ms.`
4. **PaddleController:** both `OnStartServer` and `OnStartClient` call `Register()`, which contains `if (!All.Contains(this)) All.Add(this);`. A host therefore adds its own paddle once, not twice.

The setup check is satisfied in the scene: `_startOnHeadless: 0`, `NetworkManager ▸ Run In Background` on, Multiplayer Play Mode 2.0.2 installed, FishNet 4.7.3R, no Mirror or Netcode.

## Step 0 - compile

The project compiles in Unity 6000.3.2f1; the batch import/compile exited successfully and the macOS Development build `/private/tmp/PongQA.app` was produced. The local Pong scene (`Pong-Start.unity`) is the single-player baseline; the network scene is `Pong.unity`.

## Step 1 - a host without new code

Starting a server and then a client in one process produces a host. In the recorded run the host Console contained:

```text
[Server] Started
[Client] Started
[Host] This process is the server AND a client. RTT is 0 ms.
[Server] client 0 assigned LEFT
[Client] I am client 0, side LEFT
```

The host owns client 0 and the LEFT paddle (`Paddles in All: 1` before a guest joins). The local client HUD showed `Client: ONLINE`, `my id = 0` and RTT 0.

## Step 2 - Start Host and the [Host] line

The button body is the two calls named in the handout. The `[Host]` line is printed from the **client** state handler, after checking `ServerManager.Started`, so it cannot print before the server is up.

The same host path was exercised in a Development build with `-pongServer -pongClient`. The `[Host]` line was captured in the host Console; `summary.json` stores the exact string in `host_line`. `Stop Host` stops the client first and then the server; `Start Server` alone leaves a server with no local paddle, no keyboard and no client HUD, which is the Edgegap-style dedicated server from Week 4.

## Step 3 - both callbacks, one paddle

In a host the same NetworkObject has two halves. The host Console clearly shows the server half doing its work first:

```text
[Server] client 0 assigned LEFT          <- OnStartServer
[Server] Client 0 joined as LEFT
[Client] I am client 0, side LEFT        <- OnStartClient
```

**Write-down:** `OnStartServer` ran first, on the **server half**. `OnStartClient` ran second, on the **client half**, for the same paddle object. Without the `All.Contains` guard, both calls would append the same object, and `All.Count` would be 2 with only one paddle.

Measured with the new HUD panel:

| Situation | `Paddles in All` |
|---|---|
| Host only, before the guest joins | **1** |
| Host + one guest | **2** (host LEFT + guest RIGHT) |

The first value is the important one: it is 1, not 2. Evidence: the first samples of `rally30-host-smoke.jsonl`.

## Step 4 - a guest joins

The checked guest was a separate macOS Development process on the same Mac. It is the same code path as a Multiplayer Play Mode instance (client connects to an IP and port); Multiplayer Play Mode itself was not used for this recording.

Observed:

- Host: `[Server] client 1 Started; clients 2`, then `[Server] client 1 assigned RIGHT`, `[Server] Client 1 joined as RIGHT`.
- Guest: `[Client] I am client 1, side RIGHT`, HUD `my id = 1`, RTT above 0.
- Both windows: two paddles, one ball, the shared score. The guest moves RIGHT; the host moves LEFT. Both paddles reached `z = -4.0 ... +4.0` in the automated rally.

## Step 5 - the host wins

Played with the server's 30 Hz scene, a host process and a RIGHT guest. The guest's RTT was **7-33 ms, mode 20 ms** over loopback. A second run over the machine's LAN address measured **7-17 ms, mode 7 ms**. The host's counter starts at **0 ms** because its client is in the same process; the live FishNet estimate can later display one tick (33 ms at 30 Hz, 100 ms at 10 Hz), but it is not measuring a remote path.

At 30 Hz the server's authoritative ball moved **0.238 units per tick** (median). At **10 Hz** the same ball moved **0.700 units per 100 ms tick**. On the guest the NetworkTransform interpolates between those snapshots, but every new direction or bounce arrives up to 100 ms late, so:

- the guest's ball is visibly late at the paddle;
- during contact the ball can pass through the guest's on-screen paddle and then snap back when the server's older copy of that paddle is used;
- the host never sees that, because the host's paddle and the server's copy are the same object in the same process;
- a 10 Hz serve also waits for the next 100 ms server tick, while the host's serve starts with no network trip.

Score at 10 Hz still replicated correctly: both windows reached **LEFT 2 : 0 RIGHT** and the feed matched. The scene is back at **30 Hz**.

Why the host wins: the guest sends an input axis; the server moves the guest's paddle on a later tick. The ball collision uses the server's copy, which is one input trip old. The host's input and the server's copy are in one process, so its copy is current. This is host advantage, not a different ball or a different rule.

## Step 6 - the host leaves

Recorded mid-rally by terminating the host process. The guest Console ended with:

```text
[Client] Stopping
[Client] Stopped
```

After that the guest had no local side (`localSide = -1`), no paddles (`paddles = []`), no ball and no score. The host's objects were spawned by that server; when the server went away, FishNet despawned them. Starting a new host produced a new match: client ids 0 and 1 again, score 0 : 0.

The other direction was tested too: while the guest was connected, the guest process was terminated. The host stayed up and logged `Client 1 left RIGHT`; the RIGHT paddle was removed and the ball was reset, while the host's LEFT paddle, score and input kept working. Only the server is special.

## Step 7 - host for a partner, join a partner

**Connection card**

| Field | This run |
|---|---|
| Partner | not available in this session (no second laptop) |
| Host address | `10.151.68.123` for the guest; the host's own client uses `127.0.0.1` |
| My ClientId | **1** |
| My side | **RIGHT (orange)** |
| My RTT | **7-17 ms over the LAN IP path** (mode 7 ms); 13-33 ms over loopback |
| Connected first try? | **Yes**; no fix line needed |
| Scope | Two processes on one Mac, one of them using the machine's LAN address. This is **not** the required two-physical-laptop card. |

The correct fix line if a real partner cannot connect is the first one in Section 4: read the host's IP again (Notes 2.1) and confirm the host Console says `[Server] Started`; then check the host firewall for UDP 7770, Private + Public.

## Step 8 - the same scripts, three ways

1. **Start Server only:** the server-half scripts run: `ConnectionLogger` server events, `GameState.OnStartServer` (score and feed reset), `BallSpawner.OnStartServer` (one ball), the server half of `NetworkBall`, and the HUD. No player is spawned, so there is no `PaddleController.Local`, no owner `Update`, no keyboard, no local RTT and no paddles. The ball waits; the score stays 0 : 0. This process is the dedicated server from Week 4.
2. **Start Host:** the server halves above plus the client half of the same process. `ConnectionLogger` logs both `[Server]` and `[Client]`; `GameState` is one object; `PaddleController.OnStartServer` and `OnStartClient` both run for the host's own paddle, but `All` keeps one entry; `OnTick` moves it and `Update` reads only the owner's keyboard; `NetworkBall.OnStartClient` paints the replicated ball; the HUD shows both server and client traffic. "Runs twice" means both network callbacks run, not that two components exist.
3. **Start Client:** the guest knows the address and port it dialed, its ClientId, its owned paddle (`IsOwner`), its side and the replicated score/feed/ball/paddles, plus its measured RTT. It does **not** know whether the server is also a player, how many processes the host runs, or anything the server does not send. It knows only the state the server replicates.

## Self-review

1. **Start Host: which two calls? Why is the host's RTT 0?** `ServerManager.StartConnection()` then `ClientManager.StartConnection("127.0.0.1", port)`. The client and server are in one process; there is no network path between them, so the request/response never leaves the machine. The `[Host]` line reports 0 ms. The live counter can show one tick later (33 ms at 30 Hz), but that is local tick scheduling, not a remote round trip.
2. **The guest's paddle on the host, the guest and a spectator:**

   | Where | `IsOwner` | `IsServerInitialized` | Callbacks that ran | Reads the keyboard? |
   |---|---|---|---|---|
   | Host process | false | true | `OnStartServer` then `OnStartClient` (host has both halves) | no |
   | Guest process | true | false | `OnStartClient` only | yes |
   | Spectator process | false | false | `OnStartClient` only | no (`Side == 2` returns before input) |

   The guest's own process is the only place that reads that player's keyboard. The host reads the host player's keyboard; a spectator reads none.

3. **Host Space vs guest Space:** host: `Keyboard.current` → `PaddleController.Update` (owner) → `RequestServe()` → the server half in the same process validates the turn and calls `NetworkBall.Serve` → the server moves the ball → the host client renders the same ball. Guest: `Keyboard.current` → guest `Update` → `RequestServe` ServerRpc over UDP → server receives one one-way trip later → validates and serves → ball state/position comes back one one-way trip later → guest renders. The **guest** waits about one RTT; the host does not wait for a network trip. Both are still server-authoritative.
4. **Step 6: why did the guest's own paddle vanish when the host stopped? Why did the host's game survive when the guest stopped?** The guest's paddle was spawned by the server and existed only as a replicated object. When the host process stopped, the client lost the server and FishNet despawned the server-spawned objects, so the guest had no paddle, ball or score. When the guest stopped, the host was still the server: only that guest's owned paddle was despawned; the host's paddle, score and loop kept running.
5. **State or event:** score = **state** (`SyncVar<int>`); "RIGHT scored!" = **event** (one announcement at the goal); whose serve = **state** (`SyncVar<int> ServingSide`); "YOU SERVE" = **event** (`TargetRpc` to one owner); ball position = **state** (continuous NetworkTransform snapshots); feed = **event history** (a replicated `SyncList<string>` of past events).
6. **A guest writes `GameState.Instance.LeftScore.Value = 9`:** nothing authoritative happens. With default FishNet write permissions the setter refuses because the server is not active in that process; it logs a warning and the value stays the server's value. No packet is sent. The same line on the host works because the host process has the server started, so the write is allowed and the new value replicates to clients.
7. **Feed logs twice on the host; which line stops a goal sound from playing twice?** The host runs both the server callback and the client callback. `GameState.Announce` must only run on the server (`if (!Instance.IsServerInitialized) return;`), while the presentation (sound/flash) must run only on the client half. In a sync callback that means guarding the presentation with `if (!asServer) return;` (or `if (!asServer) PlaySound();`). The score log does not double because it is inside `Announce`, which returns on the client half; only the server writes it once.

## Hand-in

- [x] `ConnectionHud`: `Start Host` / `Stop Host`, visible in the correct states.
- [x] `ConnectionLogger`: `[Host]` line.
- [x] `PaddleController`: both registrations guarded; `All.Count` shown in the HUD.
- [x] `Week05/README.md`: Step 3 callback order, Step 5 RTT/tick-10 observations, connection card, three Step 8 lines, self-review answers.
- [ ] Physical two-laptop partner card: still needs a real partner and a real LAN/hotspot. The values above are a same-machine LAN-IP check, not a substitute.
- [ ] Push: `Week 5: Pong on a host + connection card`.

## Reproduce

1. Open `Pong/` in Unity 6000.3.2f1 and `Assets/Scenes/Pong.unity`.
2. Host: press Play, click **Start Host**. Guest: press Play in another instance, type the host address and click **Start Client**.
3. Expected host Console: `[Host] This process is the server AND a client. RTT is 0 ms.` Expected guest HUD: `client 1 / RIGHT`, RTT above 0.
4. For the automated evidence, `PongEditorTools.BuildQA` builds `/private/tmp/PongQA.app`; the host ran with `-pongServer -pongClient`, the guest with `-pongClient -pongAddress 127.0.0.1` (or the LAN address), both with `-pongBenchmark` and `-pongTickRate 30` or `10`.
