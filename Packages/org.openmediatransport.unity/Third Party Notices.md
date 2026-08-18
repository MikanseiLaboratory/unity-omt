This package redistributes native binaries from Open Media Transport.

## libomt and libvmx

- Source: https://github.com/openmediatransport/libomtnet/releases/tag/v1.0.0.16
- Package: `OpenMediaTransport.Binaries.Release.v1.0.0.16.zip`
- Zip SHA-256: `c70e67f7e2a7ed5b4c389d99af62796a8c9c7be23c8debfae3fd8020c1dc66b9`
- License: MIT (see `LICENSE.md` and upstream `LICENSE.txt`)

### Plugin hashes

```
bf8ce200fd150b6453a5bad079bd0e392f3cd6eeb0a0934cbee9d0bc0c0ae7ec  Runtime/Plugins/macOS/libomt.dylib
09814f17948dec3012ace953a85df697a841c842aabbd48cc4aa4b797acfebe2  Runtime/Plugins/macOS/libvmx.dylib
83830687a9eb79630af16f8b1cb1cd5b2f6c36423fa0bdfd40ec7f5ed90a448d  Runtime/Plugins/Windows/x86_64/libomt.dll
a33167041939bce24729343963ca8fca373b5878a36ea475a76c2393c48b70ce  Runtime/Plugins/Windows/x86_64/libvmx.dll
```

libomt statically includes libomtnet. `libomtnet.dll` is not required at runtime.
`omt_shutdown` is not exported by the 1.0.0.16 build; the Unity wrapper treats it as optional.
