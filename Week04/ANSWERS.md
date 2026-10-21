# Homework 2 - Written answers

Siraç Göktuğ Şimşek | DGD0005 | Week 4

Repository: https://github.com/unitybtw/dgd0005-siracsimsek

## Step 3 - Check the input

If the server receives `axis = 50`, `axis = Mathf.Clamp(axis, -1f, 1f);` changes it to 1. This line runs on the **server**, inside the owner-only `[ServerRpc] ServerSetAxis`. The server's tick then multiplies the accepted axis by its own speed (8 units/s) and tick duration. NaN returns before any state change, and a spectator request returns before writing `_serverAxis`. Positive/negative infinity is also reduced to +1/-1 by the clamp. Client-side presentation never decides server speed.

## Step 4 - Why 30 ms?

At 30 Hz a regular keyboard request arrives every 33.33 ms. A 30 ms minimum accepts that cadence while rejecting multiple requests processed in one server tick. The clock is `TimeManager.TicksToTime(TimeManager.Tick)` on the server; a client cannot supply the timestamp. `_lastInputTime` is an **instance** field on each owned paddle, so one player's spam cannot consume the other player's allowance. `[ServerRpc]` retains FishNet's default ownership check.

- **5 ms:** still rejects requests in the same server tick, because this clock has tick resolution, but allows requests on every distinct tick; it is a weaker minimum if tick rate rises. It does not measure sub-tick 5 ms spacing.
- **500 ms:** accepts only about two inputs per second, rejects normal 30 Hz input, and holds stale directions for too long; movement and stopping feel unresponsive.

Well-formedness/range and the required spectator guard run first; the time check runs before `_serverAxis` changes. In requests with further game rules, validate them after rate accounting, then write state and announce. Here axis is input memory; the server tick performs movement. Each rejected rate-limited request increments `RejectedInputs` and logs the specified message. Server HUD panels are added on start and removed on stop/destruction so reconnects do not leak stale counters.

## Step 5 - Cheat explanations

1. **C:** twenty RPCs only try to replace the stored `_serverAxis`; they never move the paddle themselves, and the server simulates movement once per tick at its own speed, so even without the rate limit twenty requests cannot multiply speed by twenty.
2. **T:** the client writes its own visual copy, while ball collisions use the unchanged authoritative paddle in the separate server process; client position is never sent upstream.
3. **Without the rate limit:** X still fails because the server clamps 50 to 1, and T still fails because the server controls NetworkTransform state; C still cannot increase movement speed, but its excessive requests would no longer be refused or counted.

## Lecture self-review (Week 4, Section 12)

1. **Week 3 cheats:** teleporting to the ball, moving faster than the permitted speed, and moving outside the court were possible when the server relayed owner positions. Turn off Client Authoritative and accept validated input for server simulation to remove this position authority.
2. **Editing score locally:** `LeftScore.Value = 99` in a client process cannot send a server-authoritative SyncVar write to other machines. It may change only the local display (or be refused by that SyncVar's write permissions); it never changes the authoritative score. A subsequent authoritative synchronization restores the received value. With change-only replication, do not assume an immediate correction while the server value stays unchanged.
3. **Authority does not fix:** a wallhack reading positions already delivered to a client requires observer/visibility rules that avoid delivering hidden information. Aimbots and macros using legal inputs require detection and game-specific policy. Request spam needs a per-client rate limit in addition to state authority.
4. **Input delay:** key -> next owner tick -> input packet to server -> validation -> server simulation tick -> position packet back -> interpolation/render frame -> screen. Outbound plus inbound transport is approximately one RTT, with tick/frame queues and interpolation added.
5. **Request checks in this game:** owner authentication (FishNet RPC dispatch); well-formed NaN/range checks; per-paddle rate check; game permission (spectators cannot move), then write the accepted axis and simulate. The homework specifically places its spectator early-return alongside the Step 3 checks, before time accounting. For serve: valid active player, rate if implemented, your turn/two players/ball idle, then serve and announce. The optional one-second serve limiter is not implemented.
6. **Cloud ports:** the provider's security list and the guest OS firewall are independent network barriers; both must permit incoming UDP 7770. Container hosting may map a different public UDP port to the game's internal 7770, so a remote client uses the public mapping. This answer does not claim a deployment.
7. **Dedicated Server paths:** do not require a renderer, camera, UI, audio or `Keyboard.current` on the server. Use `#if !UNITY_SERVER` for presentation and keyboard code, `#if UNITY_SERVER` for server startup, and null/role checks for shared code. In this homework the owner guard keeps keyboard reads out of a server-only paddle tick; `Keyboard.current` is also null-checked. The verified build is a macOS Development player, not a Linux Dedicated Server build.

## Two vocabulary words and the pointed-at line

- **Authority:** the machine permitted to decide a piece of game state. Here the server decides paddle position, ball simulation and score.
- **Rate limit:** a server-enforced minimum interval between accepted requests for one caller. Here each owned paddle stores its own last accepted server time.

Be ready to point at and explain:

```csharp
if (_lastInputTime >= 0 && now - _lastInputTime < MinInputGap)
```

`now` comes from the server tick clock, `_lastInputTime` belongs to this paddle, and `MinInputGap` is 0.030 seconds. A request inside that interval increments the counter and returns before changing `_serverAxis`. Also explain `speed * (float)TimeManager.TickDelta`: distance per server simulation tick, independent of client frame rate or number of RPCs.

The local guard `_inputSubmittedForTick != TimeManager.LocalTick` means this client sends the keyboard axis only if that local tick has no input yet; C/X may already have supplied it in Update. This client marker is not a trusted server timestamp and never replaces the server's rate check.

## Server Console evidence

![Server Console after one C: 19 rejects](Captures/server-console-c.png)

The current real Console image shows counter 9 → 28 (+19) after one C press, with both clients active. The capture occurs after the twenty C requests complete and before the next ordinary request is validated; no sender is paused and no rejection is hidden from the raw log. C sends twenty RPCs in Update. Its first request fulfills the current local tick, so OnTick does not add a 21st request for that same tick; the next tick still gets input normally. The latest runtime trace verified 20 C requests with 19 rejected in all four C trials and no inputless ticks across 232 LEFT and 193 RIGHT ticks. The 30 ms server limiter, counter and logging are unchanged. Unrelated ordinary requests arriving in the same server tick may add further rejections; raw traces preserve them. Full measured results and scope are in README.md.

The two vocabulary words and pointed-at line must also be explained orally; this document does not claim that the oral demonstration or deadline requirement has been completed.
