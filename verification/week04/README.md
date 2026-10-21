# Homework 2 verification

## Current revision - 21 October 2026

Start with `cli-audit-2026-10-21.json` and `../../Week04/README.md`. The current PaddleController and optional probe hashes match that successful Unity 6000.3.2f1 CLI build and four-process local runtime check. Code has not changed since that build. Ordinary owner input is sent on every network tick, including zero and ticks around C/X; no skip flag remains. The server owns movement, clamps input/position, and enforces an independent 30 ms limit per paddle.

The ordinary C test produced +20 rejections in its arrival tick with normal-speed movement, because a regular input packet can share the burst's tick. The isolated 20-request subtest produced +19; it is a separate validation case, not the normal-play demonstration. X moved at normal speed; T corrected to the actual server snapshot; NaN, spectators, independent movement, finite positions and court bounds passed. This measured C count differs from the handout's idealized +19 and is disclosed rather than hidden.

`console-every-tick-2026-10-21.json` describes the current real Console image in `../../Week04/Captures/server-console-c.png`. It shows 20 collapsed rejects after one C press with two separate clients connected and both ordinary input senders active. `console-every-tick-selected-2026-10-21.jsonl` retains the original samples around the event. No tick sender was paused or skipped for this screenshot.

The full current runtime traces, build log and temporary audit helpers are local in `delivery/homework2-cli-check-2026-10-21/attempt-6/` (not committed). The game probe requires explicit `-pongHomework2Probe` opt-in and is inactive in ordinary gameplay.

## Historical evidence

`results.json`, `server-selected.jsonl` and `client-teleport.jsonl` describe the 18 October revision and native-keyboard multi-process tests, including shared scoring. `tools/summarize_homework2.py` extracts those dated trace windows. The original raw archive is `delivery/homework2-raw-evidence.zip` (not committed).

An earlier isolated Console run paused a separate client's ordinary sender and yielded 19 rejects. It is historical evidence, not the current Console screenshot. The 20 October report `cli-audit-2026-10-20.json` and archived image `../../Week04/Captures/server-console-c-2026-10-20.png` describe the later revision that still omitted an ordinary input after C/X. Those dated reports do not establish current source hashes; use the 21 October report above.

Synchronous Editor tooling and trace recording can cause packet catch-up and background rejections. Cumulative rejects are not cheat-only counts. The current optional probe buffers writes and captures extra client frames around T to preserve short corrections.

All runtime checks are local to one Mac. Physical partner/hotspot testing, human vocabulary/line explanation, deadline compliance and cloud deployment have not been established by these artifacts. The final review checks repository and artifact consistency; it does not claim a new Unity runtime run when gameplay code is unchanged.
