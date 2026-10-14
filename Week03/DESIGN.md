# Week 3 design sheet

| Thing | State / event | FishNet piece | Object | Writer / caller |
| --- | --- | --- | --- | --- |
| Ball position | State | NetworkTransform | Ball | Server |
| Ball in play | State | SyncVar<bool> | Ball | Server |
| Two scores | State | SyncVar<int> | GameState | Server |
| Whose serve | State | SyncVar<int> | GameState | Server |
| Let me serve | Event | ServerRpc | Owner paddle | Owner client |
| LEFT scored, to all | Event | ObserversRpc | Scorer paddle | Server |
| YOU SERVE, to one | Event | TargetRpc | Conceder paddle | Server |
| Event feed | State | SyncList<string> | GameState | Server |

BallSpawner on Court creates the one ball in OnStartServer at `(0, Court.BallY, 0)`, then calls Spawn. NetworkBall.Update starts with `if (!IsServerInitialized) return;`.

This is a separate design record committed during final preparation. The implementation already existed; this commit does not claim the design was pushed before the original Step 7 work.
