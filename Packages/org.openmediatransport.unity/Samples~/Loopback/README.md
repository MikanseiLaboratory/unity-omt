# Loopback sample

Creates an `OmtSender` and `OmtReceiver` in the same Player.

The repository also includes a ready-to-play scene at `Assets/Scenes/Loopback.unity`. Prefer that when you clone this repo.

1. Import the sample.
2. Add `OmtLoopbackSample` to a GameObject with a Renderer.
3. Enter Play Mode. Discovery looks up `HOSTNAME (Unity Loopback)` and connects the receiver.

Use this to verify plugin load, discovery, GPU conversion, and teardown without a second machine.
