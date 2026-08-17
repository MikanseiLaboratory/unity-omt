using System;
using System.Collections;
using OpenMediaTransport.Interop;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenMediaTransport
{
    public enum OmtCaptureMethod
    {
        GameView = 0,
        Camera = 1,
        Texture = 2
    }

    [ExecuteAlways]
    [AddComponentMenu("OMT/OMT Sender")]
    public sealed class OmtSender : MonoBehaviour
    {
        [SerializeField] string _omtName = "Unity Sender";
        [SerializeField] OmtQuality _quality = OmtQuality.Default;
        [SerializeField] bool _keepAlpha;
        [SerializeField] OmtCaptureMethod _captureMethod = OmtCaptureMethod.GameView;
        [SerializeField] Camera _sourceCamera;
        [SerializeField] Texture _sourceTexture;
        [SerializeField] int _frameRateN = 60;
        [SerializeField] int _frameRateD = 1;
        [SerializeField, HideInInspector] OmtResources _resources;

        string _runtimeName;
        OmtCaptureMethod _runtimeMethod;
        OmtSendHandle _handle;
        bool _runtimeHeld;
        OmtFormatConverter _converter;
        OmtReadbackPool _pool;
        CommandBuffer _cameraBuffer;
        Camera _attachedCamera;
        IntPtr _metadataPtr;
        string _metadata;
        long _timestamp = 1;

        public string omtName
        {
            get => _runtimeName ?? _omtName;
            set
            {
                if (_runtimeName == value)
                    return;
                _omtName = _runtimeName = value;
                Restart();
            }
        }

        public OmtQuality quality
        {
            get => _quality;
            set
            {
                if (_quality == value)
                    return;
                _quality = value;
                Restart();
            }
        }

        public bool keepAlpha
        {
            get => _keepAlpha;
            set => _keepAlpha = value;
        }

        public OmtCaptureMethod captureMethod
        {
            get => _runtimeMethod;
            set
            {
                if (_runtimeMethod == value)
                    return;
                _captureMethod = _runtimeMethod = value;
                Restart();
            }
        }

        public Camera sourceCamera
        {
            get => _sourceCamera;
            set
            {
                _sourceCamera = value;
                ResetState();
            }
        }

        public Texture sourceTexture
        {
            get => _sourceTexture;
            set => _sourceTexture = value;
        }

        public string metadata
        {
            get => _metadata;
            set
            {
                if (_metadata == value)
                    return;
                _metadata = value;
                RebuildMetadataPointer();
            }
        }

        public int connections => _handle != null && !_handle.IsInvalid ? OmtNative.omt_send_connections(_handle.Raw) : 0;

        public void SetResources(OmtResources resources) => _resources = resources;

        public void AddConnectionMetadata(string xml)
        {
            if (_handle == null || _handle.IsInvalid || string.IsNullOrEmpty(xml))
                return;
            var ptr = OmtUtf8.Alloc(xml);
            try { OmtNative.omt_send_addconnectionmetadata(_handle.Raw, ptr); }
            finally { OmtUtf8.Free(ptr); }
        }

        public void ClearConnectionMetadata()
        {
            if (_handle != null && !_handle.IsInvalid)
                OmtNative.omt_send_clearconnectionmetadata(_handle.Raw);
        }

        public OmtStatistics GetVideoStatistics()
        {
            if (_handle == null || _handle.IsInvalid)
                return default;
            OmtNative.omt_send_getvideostatistics(_handle.Raw, out var stats);
            return stats;
        }

        public string incomingMetadata
        {
            get
            {
                if (_handle == null || _handle.IsInvalid)
                    return null;
                var ptr = OmtNative.omt_send_receive(_handle.Raw, 0);
                if (ptr == IntPtr.Zero)
                    return null;
                var frame = System.Runtime.InteropServices.Marshal.PtrToStructure<OmtMediaFrame>(ptr);
                return OmtUtf8.FromPtr(frame.Data, frame.DataLength);
            }
        }

#if UNITY_EDITOR
        void OnValidate()
#else
        void Awake()
#endif
        {
            omtName = _omtName;
            captureMethod = _captureMethod;
        }

        void OnEnable() => ResetState();
        void OnDisable() => Restart(false);
        void OnDestroy() => Restart(false);

        internal void Restart() => Restart(isActiveAndEnabled);

        void Restart(bool active)
        {
            StopCapture();
            ReleaseSender();
            if (active)
                ResetState();
        }

        void ResetState()
        {
            StopCapture();
            if (!isActiveAndEnabled)
                return;
            if (_resources == null)
                _resources = OmtResources.LoadDefault();
            if (_resources == null || !_resources.IsValid)
            {
                Debug.LogError("OmtSender requires OmtResources with compute shaders assigned.", this);
                return;
            }

            PrepareSender();
            if (captureMethod == OmtCaptureMethod.Camera)
                AttachCamera();
            else
                StartCoroutine(CaptureCoroutine());
        }

        void PrepareSender()
        {
            if (_handle != null && !_handle.IsInvalid)
                return;
            OmtRuntime.AddRef();
            _runtimeHeld = true;
            var name = OmtUtf8.Alloc(string.IsNullOrEmpty(omtName) ? gameObject.name : omtName);
            try
            {
                var ptr = OmtNative.omt_send_create(name, _quality);
                if (ptr == IntPtr.Zero)
                    throw new InvalidOperationException("Failed to create OMT sender");
                _handle = new OmtSendHandle(ptr);
            }
            finally
            {
                OmtUtf8.Free(name);
            }

            _converter = new OmtFormatConverter(_resources);
            _pool = new OmtReadbackPool();
            var info = System.Runtime.InteropServices.Marshal.AllocHGlobal(OmtSenderInfoNative.Size);
            try
            {
                OmtNativeUtil.WriteSenderInfo(info, "Open Media Transport for Unity", "Open Media Transport", Application.unityVersion);
                OmtNative.omt_send_setsenderinformation(_handle.Raw, info);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(info);
            }
        }

        void ReleaseSender()
        {
            _pool?.Dispose();
            _pool = null;
            _converter?.Dispose();
            _converter = null;
            _handle?.Dispose();
            _handle = null;
            RebuildMetadataPointer(true);
            if (_runtimeHeld)
            {
                _runtimeHeld = false;
                OmtRuntime.Release();
            }
        }

        void StopCapture()
        {
            StopAllCoroutines();
            DetachCamera();
        }

        IEnumerator CaptureCoroutine()
        {
            var eof = new WaitForEndOfFrame();
            while (enabled)
            {
                yield return eof;
                if (captureMethod == OmtCaptureMethod.Texture && _sourceTexture != null)
                    CaptureTexture(_sourceTexture, true);
                else if (captureMethod == OmtCaptureMethod.GameView)
                    CaptureGameView();
            }
        }

        void CaptureGameView()
        {
            var rt = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
            ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);
            CaptureTexture(rt, false);
            RenderTexture.ReleaseTemporary(rt);
        }

        void CaptureTexture(Texture texture, bool vflip)
        {
            if (_handle == null || _handle.IsInvalid || _converter == null || _pool == null)
                return;
            var buffer = _converter.Encode(texture, vflip);
            if (!_pool.TryBegin(texture.width, texture.height, _metadata, buffer, OnReadback, null))
                return;
        }

        void AttachCamera()
        {
            if (_sourceCamera == null)
                return;
            _attachedCamera = _sourceCamera;

#if OMT_HAS_SRP
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
#endif
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                _cameraBuffer = new CommandBuffer { name = "OMT Camera Capture" };
                _attachedCamera.AddCommandBuffer(CameraEvent.AfterEverything, _cameraBuffer);
                Camera.onPostRender += OnBuiltinPostRender;
            }
        }

        void DetachCamera()
        {
#if OMT_HAS_SRP
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
#endif
            Camera.onPostRender -= OnBuiltinPostRender;
            if (_attachedCamera != null && _cameraBuffer != null)
                _attachedCamera.RemoveCommandBuffer(CameraEvent.AfterEverything, _cameraBuffer);
            _cameraBuffer?.Release();
            _cameraBuffer = null;
            _attachedCamera = null;
        }

#if OMT_HAS_SRP
        void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _attachedCamera)
                return;
            var cmd = new CommandBuffer { name = "OMT SRP Capture" };
            EncodeFromCamera(cmd, camera);
            context.ExecuteCommandBuffer(cmd);
            cmd.Release();
        }
#endif

        void OnBuiltinPostRender(Camera camera)
        {
            if (camera != _attachedCamera || _cameraBuffer == null)
                return;
            _cameraBuffer.Clear();
            EncodeFromCamera(_cameraBuffer, camera);
        }

        void EncodeFromCamera(CommandBuffer cmd, Camera camera)
        {
            if (_converter == null || _pool == null)
                return;
            var w = camera.pixelWidth;
            var h = camera.pixelHeight;
            var target = camera.targetTexture != null
                ? (RenderTargetIdentifier)camera.targetTexture
                : BuiltinRenderTextureType.CameraTarget;
            var buffer = _converter.Encode(cmd, target, w, h, true);
            _pool.TryBegin(w, h, _metadata, buffer, OnReadback, cmd);
        }

        void OnReadback(AsyncGPUReadbackRequest request, OmtReadbackEntry entry)
        {
            try
            {
                if (request.hasError || _handle == null || _handle.IsInvalid || entry == null || !entry.Pixels.IsCreated)
                    return;

                var metadata = entry.Metadata;
                IntPtr metaPtr = IntPtr.Zero;
                var metaLen = 0;
                if (!string.IsNullOrEmpty(metadata))
                {
                    metaPtr = _metadataPtr;
                    metaLen = System.Text.Encoding.UTF8.GetByteCount(metadata) + 1;
                }

                var frame = new OmtMediaFrame
                {
                    Type = OmtFrameType.Video,
                    Timestamp = _timestamp,
                    Codec = OmtCodec.BGRA,
                    Width = entry.Width,
                    Height = entry.Height,
                    Stride = entry.Width * 4,
                    Flags = _keepAlpha ? OmtVideoFlags.Alpha : OmtVideoFlags.None,
                    FrameRateN = Math.Max(1, _frameRateN),
                    FrameRateD = Math.Max(1, _frameRateD),
                    AspectRatio = entry.Width / (float)Math.Max(1, entry.Height),
                    ColorSpace = entry.Height < 720 ? OmtColorSpace.BT601 : OmtColorSpace.BT709,
                    Data = GetPointer(entry),
                    DataLength = entry.Width * entry.Height * 4,
                    FrameMetadata = metaPtr,
                    FrameMetadataLength = metaLen
                };
                _timestamp += OmtAbi.TimestampTicksPerSecond * _frameRateD / Math.Max(1, _frameRateN);
                OmtNativeUtil.SendVideo(_handle.Raw, ref frame);
            }
            finally
            {
                _pool?.Complete(entry);
            }
        }

        static unsafe IntPtr GetPointer(OmtReadbackEntry entry)
        {
            if (!entry.Pixels.IsCreated)
                return IntPtr.Zero;
            return (IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(entry.Pixels);
        }

        void RebuildMetadataPointer(bool freeOnly = false)
        {
            if (_metadataPtr != IntPtr.Zero)
            {
                OmtUtf8.Free(_metadataPtr);
                _metadataPtr = IntPtr.Zero;
            }
            if (!freeOnly && !string.IsNullOrEmpty(_metadata))
                _metadataPtr = OmtUtf8.Alloc(_metadata);
        }
    }
}
