# Sender sample

1. Import this sample from Package Manager.
2. Add `Omt Sender` to any GameObject.
3. Choose a capture method:
   - **Game View** — sends the game view.
   - **Camera** — Built-in uses a camera command buffer; URP/HDRP use `endCameraRendering`.
   - **Texture** — sends a Texture or RenderTexture. This path works on every supported pipeline.
4. Enter Play Mode and connect an OMT receiver to `HOSTNAME (Unity Sender)`.
