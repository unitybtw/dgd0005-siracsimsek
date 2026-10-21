# Homework 2 verification

## Current tick-slot revision - 21 October 2026

Start with `cli-audit-tick-slot-2026-10-21.json` and `../../Week04/README.md`. The current PaddleController and probe hashes match the successful actual Unity 6000.3.2f1 CLI build and four-process runtime check. `_inputSubmittedForTick` tracks a completed local tick: C/X sent in Update already fulfill it; OnTick supplies ordinary keyboard input, including zero, to each tick not already fulfilled. No later tick is blindly skipped.

The reference ordinary C case had +19 rejects and normal-speed movement. Server RPC traces verified 20 C requests and exactly 19 C rejects in all four trials. Two later arrival ticks additionally rejected an ordinary request and had a total +20; these extra counts are disclosed and retained. X, T, NaN, spectators, independent movement and finite/clamped server positions passed. The 30 ms validation/counter/log body is unchanged apart from an explicitly opt-in Development/Editor trace hook.

`tick-input-coverage-2026-10-21.jsonl` retains post-tick completion samples: 232 consecutive LEFT ticks including C/X and 193 RIGHT ticks, with no tick lacking an input. `server-c-inputs-2026-10-21.jsonl` contains original RPC arguments and pre-call rejection counts around all four C bursts. The full raw traces, build log and helpers are local in `delivery/homework2-cli-check-2026-10-21/attempt-9-tick-slot/`.

`console-tick-slot-2026-10-21.json` and `console-tick-slot-inputs-2026-10-21.jsonl` document the current +19 real Console capture. Console is cleared before the first C call and captured after all twenty calls, before validating the next ordinary request. The capture helper only observes the opt-in trace; it never pauses input or changes the server counter. Earlier screenshots remain dated historical evidence.

The game probe and RPC recording are inactive unless explicitly enabled with `-pongHomework2Probe`. An isolated malformed-input subtest pauses only a separate client's sender; no such pause is used for the ordinary C/X/T tests or current Console capture.

## Historical evidence

`results.json`, `server-selected.jsonl` and `client-teleport.jsonl` describe the 18 October revision and native-keyboard multi-process tests, including shared scoring. `tools/summarize_homework2.py` extracts those dated trace windows. The original raw archive is `delivery/homework2-raw-evidence.zip` (not committed).

An earlier isolated Console run paused a separate client's ordinary sender and yielded 19 rejects. It is historical evidence, not the current Console screenshot. The same-day `cli-audit-2026-10-21.json` and its +20 Console evidence describe the earlier unconditional ordinary-input revision; they are retained as history. The 20 October report `cli-audit-2026-10-20.json` and archived image `../../Week04/Captures/server-console-c-2026-10-20.png` describe the later revision that still omitted an ordinary input after C/X. Those dated reports do not establish current source hashes; use the current tick-slot report above.

Synchronous Editor tooling and trace recording can cause packet catch-up and background rejections. Cumulative rejects are not cheat-only counts. The current optional probe buffers writes and captures extra client frames around T to preserve short corrections.

All runtime checks are local to one Mac. Physical partner/hotspot testing, human vocabulary/line explanation, deadline compliance and cloud deployment have not been established by these artifacts. The current tick-slot implementation has its own fresh Unity build and runtime traces; older repository consistency reviews describe their dated revisions.
