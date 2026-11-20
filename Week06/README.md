# Week 6 - Homework 3: The Host Cheats, and the Join Code

- Unity **6000.3.2f1**, FishNet **4.7.3R**, Listen Host (`Start Host` / `Stop Host`), Homework 2 server-authoritative paddles (`Client Authoritative` off, 30 ms rate limit, keys `C`, `X`, `T`).
- Added key `G` in `PaddleController.Update` (`GameState.Instance.LeftScore.Value = 9;`, direct SyncVar write with no RPC).

## Step 1 - The guest cheats

Tested with a Development-build listen host (`Start Host`) and a separate Development-build guest connecting on `127.0.0.1:7770`. Pressing `C`, `X`, `T`, `G` on the guest produced:

| Key, on the guest | Host Console | What both windows show |
|---|---|---|
| `C` | 19 `Rejected input from client 1: too fast.` lines from the burst (`rejected` 4 → 23 on tick 52) | Paddle moves at normal speed (`+0.267` m in one 30 Hz tick, speed 8) |
| `X` | Nothing extra from the cheat (`Clamp` applied silently) | Normal speed (`+0.267` m, never faster) |
| `T` | Nothing (`Server x` stayed `7.0` on every server sample) | Jumps toward ball on the guest for one snapshot, then snaps back to `x = 7.0` |
| `G` | Nothing (`Host` score stayed `0 : 0`) | Host and guest scores remain `0 : 0`; guest logs FishNet's `LogServerNotActiveWarning()` |

**Why did the host never see `G`'s SyncVar write?** FishNet `SyncVar<T>` uses server-authoritative write permissions by default: writing `LeftScore.Value = 9` on a client where `IsServerInitialized` is false is rejected locally by `SyncVar.SetValue` (logging `LogServerNotActiveWarning`), sends no packet to the server, and leaves the authoritative score unchanged.

## Step 2 - Serve rate limit

In `PaddleController.RequestServe`, added per-paddle rate limiting (`MinServeGap = 1.0`, instance field `_lastServeTime = -1`) after the null check and before the `ServingSide` check, using `[TargetRpc] private void TargetRefused(NetworkConnection target, string why)` to flash `TOO FAST` only to the requesting owner.

- Guest double-tapping Space within `0.15 s` flashed `[Client] Flash: TOO FAST` on the guest only; the host HUD did not flash.
- The host could immediately serve (`InPlay = true`) because `_lastServeTime` is an instance field (one timer per paddle, not `static`), and after waiting more than `1.0 s` the guest's next Space press was no longer refused with `TOO FAST`.
