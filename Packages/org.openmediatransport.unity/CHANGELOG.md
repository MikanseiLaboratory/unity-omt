# Changelog

## [Unreleased]

### Added
- Point native libomt file logs at the official OMT storage path (`%ProgramData%\OMT\logs` on Windows, `~/.OMT/logs` on macOS / Linux, or `OMT_STORAGE_PATH`).

### Fixed
- Preload `libvmx` from the plugin directory so Native AOT send/receive can resolve the codec DLL in the Unity Editor and Players.
- Snap sender frames to even dimensions of at least 16x16 before VMX encode. Game View Free Aspect sizes were odd, so `VMX_Create` returned null and every `SendVideo` logged `Encoding failed`.
- Blit Camera / Game View into a resolved linear RT before compute encode. Direct `Texture2D` loads from `CameraTarget` and screenshot RTs were sampling black.
- Keep sending while Unity is unfocused: enable `runInBackground`, render Camera capture to an offscreen RT instead of Game View events, and recover stalled GPU readbacks. Inspector `OnValidate` no longer tears down a live sender.
- Keep Camera capture sending after focus loss: do not wait on `WaitForEndOfFrame`, reuse the last size when `pixelWidth` is 0, and keep the capture coroutine alive if encode throws.
- Leave package tests out of the Editor compile unless `OMT_COMPILE_PACKAGE_TESTS` is defined, so missing NUnit cannot block Play Mode.

## [0.1.0] - 2026-08-17

### Added
- UPM package for Windows x64 and macOS Universal.
- `OmtSender` with Game View, Camera, and Texture capture.
- `OmtReceiver` with background receive, latest-frame drop policy, and GPU upload.
- `OmtFinder` source discovery and Inspector selectors.
- Native libomt/libvmx plugins from official 1.0.0.16 binaries.
