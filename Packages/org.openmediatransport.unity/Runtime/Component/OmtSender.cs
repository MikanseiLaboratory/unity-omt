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
        bool _methodReady;
        OmtSendHandle _handle;
        bool _runtimeHeld;
        OmtFormatConverter _converter;
        OmtReadbackPool _pool;
        RenderTexture _cameraRt;
        IntPtr _metadataPtr;
        string _metadata;
        long _timestamp = 1;
        float _lastReadbackTime;
        int _lastCaptureWidth;
        int _lastCaptureHeight;

        public string omtName
        {
            get => _runtimeName ?? _omtName;
            set
            {
                if (_runtimeName == value)
                    return;
                _omtName = _runtimeName = value;
                if (_handle != null && isActiveAndEnabled)
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
            get => _methodReady ? _runtimeMethod : _captureMethod;
            set
            {
                _captureMethod = value;
                if (_methodReady && _runtimeMethod == value)
                    return;
                _runtimeMethod = value;
                _methodReady = true;
                if (_handle != null && isActiveAndEnabled)
                    Restart();
            }
        }

        public Camera sourceCamera
        {
            get => _sourceCamera;
            set
            {
                if (_sourceCamera == value)
                    return;
                _sourceCamera = value;
                if (Application.isPlaying && isActiveAndEnabled)
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
        {
            if (Application.isPlaying && _handle != null && isActiveAndEnabled)
            {
                if (_runtimeName != _omtName)
                    omtName = _omtName;
                if (_runtimeMethod != _captureMethod)
                    captureMethod = _captureMethod;
                return;
            }

            _runtimeName = _omtName;
            _runtimeMethod = _captureMethod;
            _methodReady = true;
        }
#else
        void Awake()
        {
            _runtimeName = _omtName;
            _runtimeMethod = _captureMethod;
            _methodReady = true;
        }
#endif

        void OnEnable()
        {
            EnsureRuntimeMethod();
            if (!Application.isPlaying)
                return;
            ResetState();
        }

        void Start()
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
                return;
            if (_handle == null || _handle.IsInvalid)
                ResetState();
        }

        void OnDisable() => Restart(false);
        void OnDestroy() => Restart(false);

        void OnApplicationFocus(bool focus)
        {
            if (focus)
                RecoverReadbacks();
        }

        void OnApplicationPause(bool pause)
        {
            if (!pause)
                RecoverReadbacks();
        }

        internal void Restart() => Restart(isActiveAndEnabled);

        void Restart(bool active)
        {
            StopCapture();
            ReleaseSender();
            if (active)
                ResetState();
        }

        void EnsureRuntimeMethod()
        {
            if (_methodReady)
                return;
            _runtimeMethod = _captureMethod;
            _methodReady = true;
        }

        void ResetState()
        {
            StopCapture();
            EnsureRuntimeMethod();
            if (!Application.isPlaying || !isActiveAndEnabled)
                return;
            if (_resources == null)
                _resources = OmtResources.LoadDefault();
            if (_resources == null || !_resources.IsValid)
            {
                Debug.LogError("OmtSender requires OmtResources with compute shaders assigned.", this);
                return;
            }

            PrepareSender();
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
            ReleaseCameraRt();
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
        }

        void RecoverReadbacks()
        {
            _pool?.Recover();
            _lastReadbackTime = Time.realtimeSinceStartup;
        }

        IEnumerator CaptureCoroutine()
        {
            var eof = new WaitForEndOfFrame();
            _lastReadbackTime = Time.realtimeSinceStartup;
            while (enabled)
            {
                // WaitForEndOfFrame never completes while the Game View is not presenting
                // (Unity in the background, or Test Patterns in front).
                if (captureMethod == OmtCaptureMethod.GameView)
                {
                    if (!Application.isFocused)
                    {
                        yield return null;
                        continue;
                    }
                    yield return eof;
                }
                else
                    yield return null;

                if (!Application.isPlaying || _handle == null || _handle.IsInvalid)
                    continue;
                if (_pool != null && _pool.InFlight > 0 &&
                    Time.realtimeSinceStartup - _lastReadbackTime > 1f)
                    RecoverReadbacks();

                try
                {
                    switch (captureMethod)
                    {
                        case OmtCaptureMethod.Texture:
                            if (_sourceTexture != null)
                                CaptureTexture(_sourceTexture, true);
                            break;
                        case OmtCaptureMethod.Camera:
                            CaptureCamera();
                            break;
                        default:
                            CaptureGameView();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, this);
                }
            }
        }

        void CaptureGameView()
        {
            if (Screen.width < 1 || Screen.height < 1)
                return;
            var rt = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
            ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);
            CaptureTexture(rt, false);
            RenderTexture.ReleaseTemporary(rt);
        }

        void CaptureCamera()
        {
            var camera = _sourceCamera != null ? _sourceCamera : Camera.main;
            if (camera == null)
                return;

            var srcW = camera.pixelWidth;
            var srcH = camera.pixelHeight;
            if (srcW < 16)
                srcW = Screen.width;
            if (srcH < 16)
                srcH = Screen.height;
            if (srcW < 16)
                srcW = _lastCaptureWidth > 0 ? _lastCaptureWidth : 1280;
            if (srcH < 16)
                srcH = _lastCaptureHeight > 0 ? _lastCaptureHeight : 720;
            _lastCaptureWidth = srcW;
            _lastCaptureHeight = srcH;

            OmtFormatConverter.AlignVmxSize(srcW, srcH, out var width, out var height);
            if (!EnsureCameraRt(width, height))
                return;

            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            camera.targetTexture = _cameraRt;
            camera.Render();
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            CaptureTexture(_cameraRt, true);
        }

        bool EnsureCameraRt(int width, int height)
        {
            if (_cameraRt != null && _cameraRt.width == width && _cameraRt.height == height)
                return true;
            ReleaseCameraRt();
            _cameraRt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1,
                hideFlags = HideFlags.DontSave,
                filterMode = FilterMode.Point,
                name = "OMT Camera Capture"
            };
            return _cameraRt.Create();
        }

        void ReleaseCameraRt()
        {
            if (_cameraRt == null)
                return;
            _cameraRt.Release();
            Destroy(_cameraRt);
            _cameraRt = null;
        }

        void CaptureTexture(Texture texture, bool vflip)
        {
            if (_handle == null || _handle.IsInvalid || _converter == null || _pool == null)
                return;
            if (texture == null || texture.width < 1 || texture.height < 1)
                return;
            // The encoder writes a single ComputeBuffer. Overwriting it while a readback
            // is in flight makes AsyncGPUReadback fail, so no video frames are sent.
            if (_pool.InFlight > 0)
                return;
            var buffer = _converter.Encode(texture, vflip, out var width, out var height);
            if (!_pool.TryBegin(width, height, _metadata, buffer, OnReadback, null))
                return;
        }

        void OnReadback(AsyncGPUReadbackRequest request, OmtReadbackEntry entry)
        {
            try
            {
                if (request.hasError || _handle == null || _handle.IsInvalid || entry == null || !entry.Pixels.IsCreated)
                    return;

                _lastReadbackTime = Time.realtimeSinceStartup;

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
