# Verification

## Native spike (no Unity)

From the repository root:

```powershell
dotnet run --project "Tools~/NativeSpike/NativeSpike.csproj" -c Release
```

This loads `libomt`/`libvmx`, checks the `OmtMediaFrame` ABI, and loopbacks BGRA over `omt://127.0.0.1:6521`.

Windows x64 result (2026-08-17): **PASS** after using a dedicated port. Pixel values differ slightly because VMX is lossy.

## Sample scenes

Open this repository in Unity 2022.3 and play:

- `Assets/Scenes/Loopback.unity` — same-Player send/receive
- `Assets/Scenes/Sender.unity` — publishes `Unity Sender`
- `Assets/Scenes/Receiver.unity` — pick a discovered source

## Unity Player matrix

Unity 2022.3 is not present in this workspace, so Player runs are scripted for a machine that has the editor installed.

| OS | Backend | Graphics API | Status |
| --- | --- | --- | --- |
| Windows x64 | Mono | D3D11 | Requires Unity 2022.3 |
| Windows x64 | IL2CPP | D3D11 | Requires Unity 2022.3 |
| Windows x64 | Mono | D3D12 | Requires Unity 2022.3 |
| Windows x64 | IL2CPP | D3D12 | Requires Unity 2022.3 |
| macOS arm64 | Mono | Metal | Requires Unity 2022.3 |
| macOS arm64 | IL2CPP | Metal | Requires Unity 2022.3 |
| macOS x64 | Mono | Metal | Requires Unity 2022.3 |

Pass criteria: plugin load, discovery, 1080p loopback, no thread leftover after Stop, bounded readback queue, no unbounded memory growth over 10 minutes.

Batchmode example:

```text
Unity.exe -batchmode -nographics -projectPath <unity-project> -runTests -testPlatform EditMode -testResults TestResults-EditMode.xml
Unity.exe -batchmode -projectPath <unity-project> -runTests -testPlatform PlayMode -testResults TestResults-PlayMode.xml
```

`Tools~/scripts/ValidatePlugins.ps1` checks hashes, PE machine type, and macOS lipo architectures when `llvm-objdump`/`lipo` are available.
