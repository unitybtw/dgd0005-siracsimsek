# Week 4 - Homework 2: The server decides

This continues the working Week 3 FishNet Pong project in `Pong/` using Unity 6000.3.2f1 and 30 Hz. The target is server-authoritative paddle movement, validated input, an independent 30 ms input limit per owned paddle, and retained C/X/T cheat demonstrations.

## Implementation steps

- Baseline: Homework 1 scenes, ball, score and serve are present. Previous verification is retained in `verification/`.
- Step 1: turn off client authority on the paddle prefab; retain server authority on the ball.
- Step 2: send an axis every network tick, including zero; move and clamp only on the server.
- Step 3: reject NaN and spectator input; clamp the axis to [-1, 1].
- Step 4: reject additional input within 30 ms using a per-paddle timestamp; count and log rejections.
- Step 5: retain C (20 requests), X (axis 50), T (local teleport); record real multiplayer results and a server Console screenshot.

Each implementation step is committed and pushed separately during this work. Results and answers will be recorded below as they are obtained. A physical partner test and a deadline/on-time claim are not inferred from local testing.

## Step 1 result

Both prefab NetworkTransforms have Client Authoritative off. In three-process Multiplayer Play Mode (server + two clients), queued W moved only the old client-local copy to z=4; the server retained LEFT z=0 and RIGHT z=0. This is the expected loss of client movement authority before adding input RPCs. FishNet suppresses unchanged transform sends, so an idle server did not immediately correct that local visual change. Evidence: `verification/week04/step1.json`.
