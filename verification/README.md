# Verification - 2026-10-14

Unity **6000.3.2f1** successfully imported and compiled the project and FishNet RPC/SyncType code generation. The actual local/network scenes and paddle/ball prefab assets are saved. A macOS Development build at `/private/tmp/PongQA.app` built successfully. Unity MCP v10.0.0 connected to the Editor and was used for Console, Play Mode, menu calls and screenshots.

`results.json` records passing checks from actual local multiplayer runs. An Editor host and two separate headless Development-build clients verified LEFT/RIGHT/SPECTATOR, one ball on each client, a server-approved serve, replicated motion and score, goal ObserversRpc to all, YOU SERVE TargetRpc to RIGHT only, and reconnect receiving 1:0 plus six feed entries. `final-right`, `final-spectator` and `final-rejoin` logs contain those checks. Raket movement was driven through a QA menu to z=3, then observed in the remote client; this tests NetworkTransform transport rather than physical keyboard focus.

A subsequent late-join test exposed that Place() overwrote the z position from spawn data. Place now preserves z. `checked-right.jsonl` confirms every sample from a new client joining an already moved paddle retains z=3. This final client has no runtime exceptions. Its serve/goal/TargetRpc paths also passed.

Statistics settings were found disabled in the generated scene and repaired. The saved scene now has Development mode, Update Client and Update Server enabled. `Pong/Captures/pong-traffic-verified.png` was rendered and visually inspected: a moving white ball, both coloured paddles, owner HUD, 30 Hz and nonzero traffic counters are visible. The traffic sample is only a sanity check, not the required tick-rate comparison experiment. HUD scrolling keeps controls reachable in a short Game view.

The final Console query retained two Metal/URP messages about memoryless depth load/store after screenshot capture. No C# compilation errors or runtime exceptions appeared in the final gameplay checks; the graphics messages remain documented separately. Package Manager also retains invalid-signature statuses for some official packages, consistent with Unity's documented old-Editor certificate-validity issue, fixed from 6000.3.7f1. The homework's required 6000.3.2f1 remains selected and signature verification was not disabled.

Older import/runtime logs preserve failures from before the fixes; they are historical evidence, not the final status. The standalone Roslyn API check (`tools/check_compile.py`) also passes; it excludes the MCP Editor helper, which is covered by the actual Unity import/build. It is secondary to the real Editor/build results.

Additional completed checks: native OS keyboard W/S/arrows moved the correct local starter paddles, with actual Local goal logs. Native W moved only the network owner (LEFT z=2.4167, RIGHT z=0); the separate guest saw LEFT z=2.41. Native Space produced a server-approved rally and a shared goal. Restarting the server yielded score 0:0 with one ball.

Multiplayer Play Mode activated Players 2/3/4; Player 2 has the Server tag. Four actual Unity processes were reached through MCP: Player 2 server-only, Main LEFT client-only, Player 3 RIGHT client-only, Player 4 SPECTATOR client-only. Each had one ball. A later rally produced 0:1 and six feed entries identically on all three clients. `multiplayer-play-mode.json` records the initial role snapshots.

The 10/60/30 Hz experiment used the same two-client arrangement, automatic paddle tracking, a 60 fps cap and 32-second runs with continuous rallies and no goals. `benchmarks/summary.json` records means of 342.8 / 1701.6 / 900.6 B/s after at least ten seconds of rally (10/60/30 Hz). Client-window descriptions in the homework table state timing consequences; no human visual comparison is claimed for the headless experiment.

Section 13 vocabulary and all ten Section 14 answers were completed from the supplied Week03 lecture notes. GitHub repo and real current commits now exist. Still pending: two physical laptops/hotspot, partner connection cards, oral demo and student sending the design link to the instructor. The historical per-step push timing cannot be recreated.
