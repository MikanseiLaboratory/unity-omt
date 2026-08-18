using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenMediaTransport
{
    internal sealed class OmtFormatConverter : IDisposable
    {
        private readonly OmtResources _resources;
        private ComputeBuffer _encodeBuffer;
        private ComputeBuffer _decodeBuffer;
        private RenderTexture _decodeOutput;
        private RenderTexture _alignTarget;
        private int _encodeWidth;
        private int _encodeHeight;
        private int _decodeWidth;
        private int _decodeHeight;
        private int _loggedSourceWidth = -1;
        private int _loggedSourceHeight = -1;

        internal OmtFormatConverter(OmtResources resources)
        {
            _resources = resources;
        }

        internal RenderTexture LastDecoderOutput => _decodeOutput;

        internal ComputeBuffer Encode(CommandBuffer cmd, RenderTargetIdentifier source, int width, int height, bool vflip, out int encodedWidth, out int encodedHeight)
        {
            AlignVmxSize(width, height, out encodedWidth, out encodedHeight);
            if (encodedWidth != width || encodedHeight != height)
                LogAlign(width, height, encodedWidth, encodedHeight);

            EnsureAlignTarget(encodedWidth, encodedHeight);
            cmd.Blit(source, _alignTarget);
            cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            DispatchEncode(cmd, encodedWidth, encodedHeight, vflip);
            return _encodeBuffer;
        }

        internal ComputeBuffer Encode(Texture source, bool vflip, out int encodedWidth, out int encodedHeight)
        {
            AlignVmxSize(source.width, source.height, out encodedWidth, out encodedHeight);
            if (encodedWidth != source.width || encodedHeight != source.height)
                LogAlign(source.width, source.height, encodedWidth, encodedHeight);

            EnsureAlignTarget(encodedWidth, encodedHeight);
            Graphics.Blit(source, _alignTarget);
            RenderTexture.active = null;

            var cmd = new CommandBuffer { name = "OMT Encode" };
            DispatchEncode(cmd, encodedWidth, encodedHeight, vflip);
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();
            return _encodeBuffer;
        }

        /// <summary>
        /// libvmx rejects widths that are not even, and both axes must be at least 16.
        /// Game View in the Editor is frequently an odd Free Aspect size.
        /// </summary>
        internal static void AlignVmxSize(int width, int height, out int alignedWidth, out int alignedHeight)
        {
            alignedWidth = Math.Max(16, width & ~1);
            alignedHeight = Math.Max(16, height & ~1);
        }

        private void DispatchEncode(CommandBuffer cmd, int width, int height, bool vflip)
        {
            EnsureEncodeBuffer(width, height);
            var shader = _resources.encoderCompute;
            var kernel = QualitySettings.activeColorSpace == ColorSpace.Linear
                ? shader.FindKernel("EncodeLinear")
                : shader.FindKernel("EncodeGamma");
            cmd.SetComputeIntParam(shader, "Width", width);
            cmd.SetComputeIntParam(shader, "Height", height);
            cmd.SetComputeFloatParam(shader, "VFlip", vflip ? 1f : 0f);
            cmd.SetComputeTextureParam(shader, kernel, "Source", _alignTarget);
            cmd.SetComputeBufferParam(shader, kernel, "Encoded", _encodeBuffer);
            cmd.DispatchCompute(shader, kernel, (width + 7) / 8, (height + 7) / 8, 1);
        }

        internal RenderTexture Decode(int width, int height, byte[] pixels, int stride)
        {
            EnsureDecodeTargets(width, height);
            var count = width * height;
            if (pixels == null || pixels.Length < count * 4)
                return _decodeOutput;

            if (stride == width * 4)
            {
                var packed = new uint[count];
                Buffer.BlockCopy(pixels, 0, packed, 0, count * 4);
                _decodeBuffer.SetData(packed);
            }
            else
            {
                var packed = new uint[count];
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var i = y * stride + x * 4;
                        packed[y * width + x] =
                            pixels[i] |
                            ((uint)pixels[i + 1] << 8) |
                            ((uint)pixels[i + 2] << 16) |
                            ((uint)pixels[i + 3] << 24);
                    }
                }
                _decodeBuffer.SetData(packed);
            }

            var shader = _resources.decoderCompute;
            var kernel = QualitySettings.activeColorSpace == ColorSpace.Linear && SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal
                ? shader.FindKernel("DecodeLinear")
                : shader.FindKernel("DecodeGamma");
            shader.SetInt("Width", width);
            shader.SetInt("Height", height);
            shader.SetFloat("VFlip", 1f);
            shader.SetBuffer(kernel, "EncodedIn", _decodeBuffer);
            shader.SetTexture(kernel, "Destination", _decodeOutput);
            shader.Dispatch(kernel, (width + 7) / 8, (height + 7) / 8, 1);
            return _decodeOutput;
        }

        private void EnsureEncodeBuffer(int width, int height)
        {
            if (_encodeBuffer != null && _encodeWidth == width && _encodeHeight == height)
                return;
            _encodeBuffer?.Release();
            _encodeBuffer = new ComputeBuffer(Math.Max(1, width * height), 4);
            _encodeWidth = width;
            _encodeHeight = height;
        }

        private void EnsureAlignTarget(int width, int height)
        {
            if (_alignTarget != null && _alignTarget.width == width && _alignTarget.height == height)
                return;
            if (_alignTarget != null)
            {
                _alignTarget.Release();
                UnityEngine.Object.Destroy(_alignTarget);
            }

            _alignTarget = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
            {
                hideFlags = HideFlags.DontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1
            };
            _alignTarget.Create();
        }

        private void LogAlign(int sourceWidth, int sourceHeight, int alignedWidth, int alignedHeight)
        {
            if (_loggedSourceWidth == sourceWidth && _loggedSourceHeight == sourceHeight)
                return;
            _loggedSourceWidth = sourceWidth;
            _loggedSourceHeight = sourceHeight;
            Debug.Log(
                "[OMT] VMX requires even dimensions of at least 16x16; sending " +
                alignedWidth + "x" + alignedHeight +
                " (source " + sourceWidth + "x" + sourceHeight + ").");
        }

        private void EnsureDecodeTargets(int width, int height)
        {
            if (_decodeBuffer != null && _decodeWidth == width && _decodeHeight == height)
                return;

            _decodeBuffer?.Release();
            if (_decodeOutput != null)
            {
                _decodeOutput.Release();
                UnityEngine.Object.Destroy(_decodeOutput);
            }

            _decodeBuffer = new ComputeBuffer(Math.Max(1, width * height), 4);
            _decodeOutput = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                enableRandomWrite = true,
                hideFlags = HideFlags.DontSave
            };
            _decodeOutput.Create();
            _decodeWidth = width;
            _decodeHeight = height;
        }

        public void Dispose()
        {
            _encodeBuffer?.Release();
            _encodeBuffer = null;
            _decodeBuffer?.Release();
            _decodeBuffer = null;
            if (_alignTarget != null)
            {
                _alignTarget.Release();
                UnityEngine.Object.Destroy(_alignTarget);
                _alignTarget = null;
            }
            if (_decodeOutput != null)
            {
                _decodeOutput.Release();
                UnityEngine.Object.Destroy(_decodeOutput);
                _decodeOutput = null;
            }
        }
    }
}
