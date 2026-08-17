# Open Media Transport for Unity

Send and receive [Open Media Transport](https://www.openmediatransport.org/) video and UTF-8 XML metadata in Unity. The components follow the same usage pattern as [KlakNDI](https://github.com/keijiro/KlakNDI): drop `OmtSender` / `OmtReceiver` on a GameObject, pick a source, and route frames to a Renderer or RenderTexture.

## Requirements

- Unity 2022.3 or later
- Windows x64 (D3D11 / D3D12), Mono or IL2CPP
- macOS x64 / arm64 Universal (Metal), Mono or IL2CPP

Audio, HDR (P216/PA16), Android, iOS, and Linux Players are out of scope for 0.1.0.

## Install

In Unity Package Manager, add this Git URL:

```
https://github.com/MikanseiLaboratory/unity-omt.git?path=/Packages/org.openmediatransport.unity
```

Or copy `Packages/org.openmediatransport.unity` into your project's `Packages` folder.

Windows may prompt for firewall access the first time a sender or receiver starts. Allow local network traffic for discovery and media.

## Quick start

### Receive

```csharp
var receiver = gameObject.AddComponent<OmtReceiver>();
receiver.omtName = "HOSTNAME (Source Name)";
receiver.targetRenderer = GetComponent<Renderer>();
receiver.targetMaterialProperty = "_MainTex";
```

`receiver.texture` is the latest decoded `RenderTexture`. `receiver.metadata` holds the latest XML.

### Send

```csharp
var sender = gameObject.AddComponent<OmtSender>();
sender.omtName = "Unity Sender";
sender.captureMethod = OmtCaptureMethod.Texture;
sender.sourceTexture = renderTexture;
sender.metadata = "<OMTMetadata Note=\"hello\" />";
```

Capture methods:

| Method | Notes |
| --- | --- |
| Game View | Uses `ScreenCapture`. Works on Built-in / URP / HDRP. |
| Camera | Built-in uses a camera command buffer. URP/HDRP use `RenderPipelineManager.endCameraRendering`. |
| Texture | Pipeline-agnostic path. Prefer this when you already have a RenderTexture. |

### Discovery

```csharp
foreach (var name in OmtFinder.sourceNames)
    Debug.Log(name);
```

The Receiver Inspector **Select** menu lists the same names.

## Performance

- Receive runs on a worker thread and keeps only the latest video frame.
- Send converts on the GPU, then uses `AsyncGPUReadback` with a 4-slot pool. If the pool is full, the frame is dropped.
- Expect about one extra frame of send latency from readback, plus VMX encode/decode.
- 1080p60 is the validation target. Keep alpha off when you do not need it.

## Known limits

- VMX is lossy; colors will not round-trip bit-exactly.
- Direct `omt://host:port` URLs avoid mDNS clashes with other OMT apps on the same machine.
- `omt_shutdown` is missing from official 1.0.0.16 binaries and is skipped when absent.
- Camera capture on SRP is a generic end-of-camera blit, not a custom URP/HDRP pass.

## Native libraries

Windows and macOS binaries come from [libomtnet v1.0.0.16](https://github.com/openmediatransport/libomtnet/releases/tag/v1.0.0.16). Hashes are listed in `Third Party Notices.md`. Refresh them with `Tools~/scripts/FetchBinaries.ps1`.
