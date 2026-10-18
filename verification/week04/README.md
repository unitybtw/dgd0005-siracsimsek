# Homework 2 verification

See `results.json` and `../../Week04/README.md` for exact measured values and limitations. Server-selected and teleport JSONL files retain original samples from the multi-process run, rather than reconstructed positions. The full local capture archive is `delivery/homework2-raw-evidence.zip` (not committed).

The main run used Unity Editor server only, separate macOS Development LEFT and RIGHT clients, then a spectator. Native W/C/X/T/Space were pressed in LEFT; opt-in Input System commands controlled RIGHT and spectator and sent NaN through the owner RPC. C/X inputs change server input memory, while the one server tick still caps movement at 8 units/s. `tools/summarize_homework2.py` checked finite positions, court limits, tick speed, actual +19 counter deltas, the other player's concurrent movement and shared scoring.

The isolated Console screenshot deliberately paused the separate client's normal tick sender before queuing one C press, to exclude unrelated zero-axis packet rejections. The server and its limiter were unchanged. The counter became 19, the server HUD showed 19, one ball existed, and the Console error query was empty. This does not substitute for the native C measurement in the ordinary two-player run.

Synchronous Editor tooling and the original high-frequency trace recording caused packet catch-up and background rejections. Do not equate cumulative counts with cheat-only counts. The final optional probe buffers output and caps extra client-frame samples at 120 Hz; this instrumentation change did not alter gameplay validation/movement and the subsequent Development build succeeded.

All checks are local, on one Mac. Physical partner/hotspot testing, human vocabulary/line explanation, deadline compliance and any cloud deployment have not been established by these artifacts.
