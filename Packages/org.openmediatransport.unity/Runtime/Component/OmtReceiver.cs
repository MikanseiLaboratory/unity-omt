using System;
using System.Runtime.InteropServices;
using System.Threading;
using OpenMediaTransport.Interop;
using UnityEngine;

namespace OpenMediaTransport
{
    [ExecuteAlways]
    [AddComponentMenu("OMT/OMT Receiver")]
    public sealed class OmtReceiver : MonoBehaviour
    {
        [SerializeField] string _omtName;
        [SerializeField] OmtQuality _quality = OmtQuality.Default;
        [SerializeField] bool _preview;
        [SerializeField] RenderTexture _targetTexture;
        [SerializeField] Renderer _targetRenderer;
        [SerializeField] string _targetMaterialProperty = "_MainTex";
        [SerializeField, HideInInspector] OmtResources _resources;

        string _runtimeName;
        OmtReceiveHandle _handle;
        bool _runtimeHeld;
        Thread _thread;
        CancellationTokenSource _cts;
        readonly OmtLatestVideoBuffer _buffer = new OmtLatestVideoBuffer();
        readonly object _metadataGate = new object();
        string _connectionMetadata;
        string _frameMetadata;
        OmtFormatConverter _converter;
        MaterialPropertyBlock _block;
        int _seenVersion;
        bool _connected;
        int _width;
        int _height;

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
                _quality = value;
                if (_handle != null && !_handle.IsInvalid)
                    OmtNative.omt_receive_setsuggestedquality(_handle.Raw, value);
            }
        }

        public RenderTexture targetTexture
        {
            get => _targetTexture;
            set => _targetTexture = value;
        }

        public Renderer targetRenderer
        {
            get => _targetRenderer;
            set => _targetRenderer = value;
        }

        public string targetMaterialProperty
        {
            get => _targetMaterialProperty;
            set => _targetMaterialProperty = value;
        }

        public RenderTexture texture => _converter?.LastDecoderOutput;
        public string metadata
        {
            get
            {
                lock (_metadataGate)
                    return _frameMetadata ?? _connectionMetadata;
            }
        }
        public bool isConnected => _connected;
        public int width => _width;
        public int height => _height;
        public int droppedFrames => _buffer.Dropped;

        public event Action<RenderTexture> VideoReceived;
        public event Action<string> MetadataReceived;

        public void SetResources(OmtResources resources) => _resources = resources;

        public void SendMetadata(string xml)
        {
            if (_handle == null || _handle.IsInvalid || string.IsNullOrEmpty(xml))
                return;
            var utf8 = OmtUtf8.Alloc(xml);
            try
            {
                var frame = new OmtMediaFrame
                {
                    Type = OmtFrameType.Metadata,
                    Data = utf8,
                    DataLength = System.Text.Encoding.UTF8.GetByteCount(xml) + 1
                };
                OmtNativeUtil.ReceiveSend(_handle.Raw, ref frame);
            }
            finally
            {
                OmtUtf8.Free(utf8);
            }
        }

        public OmtStatistics GetVideoStatistics()
        {
            if (_handle == null || _handle.IsInvalid)
                return default;
            OmtNative.omt_receive_getvideostatistics(_handle.Raw, out var stats);
            return stats;
        }

#if UNITY_EDITOR
        void OnValidate()
#else
        void Awake()
#endif
        {
            omtName = _omtName;
        }

        void OnEnable()
        {
            if (_resources == null)
                _resources = OmtResources.LoadDefault();
            StartReceiver();
        }

        void OnDisable() => StopReceiver();
        void OnDestroy() => StopReceiver();

        void Update()
        {
            if (_resources == null || !_resources.IsValid)
                return;
            if (_converter == null)
                _converter = new OmtFormatConverter(_resources);
            if (_block == null)
                _block = new MaterialPropertyBlock();

            if (!_buffer.TryConsume(ref _seenVersion, out var info, out var pixels))
                return;

            _width = info.Width;
            _height = info.Height;
            _connected = true;
            lock (_metadataGate)
                _frameMetadata = info.Metadata;

            var rt = _converter.Decode(info.Width, info.Height, pixels, info.Stride);
            if (rt == null)
                return;

            if (_targetRenderer != null && !string.IsNullOrEmpty(_targetMaterialProperty))
            {
                _targetRenderer.GetPropertyBlock(_block);
                _block.SetTexture(_targetMaterialProperty, rt);
                _targetRenderer.SetPropertyBlock(_block);
            }

            if (_targetTexture != null)
                Graphics.Blit(rt, _targetTexture);

            VideoReceived?.Invoke(rt);
            if (!string.IsNullOrEmpty(info.Metadata))
                MetadataReceived?.Invoke(info.Metadata);
        }

        internal void Restart()
        {
            if (!isActiveAndEnabled)
                return;
            StopReceiver();
            StartReceiver();
        }

        void StartReceiver()
        {
            if (string.IsNullOrEmpty(omtName))
                return;

            OmtRuntime.AddRef();
            _runtimeHeld = true;
            var address = OmtUtf8.Alloc(omtName);
            try
            {
                var flags = _preview ? OmtReceiveFlags.Preview : OmtReceiveFlags.None;
                var ptr = OmtNative.omt_receive_create(address, OmtFrameType.Video | OmtFrameType.Metadata, OmtPreferredVideoFormat.BGRA, flags);
                if (ptr == IntPtr.Zero)
                    throw new InvalidOperationException("Failed to create OMT receiver for " + omtName);
                _handle = new OmtReceiveHandle(ptr);
                OmtNative.omt_receive_setsuggestedquality(_handle.Raw, _quality);
            }
            finally
            {
                OmtUtf8.Free(address);
            }

            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            var handle = _handle;
            _thread = new Thread(() => ReceiveLoop(handle, token))
            {
                IsBackground = true,
                Name = "OMT Receiver " + omtName
            };
            _thread.Start();
        }

        void StopReceiver()
        {
            _cts?.Cancel();
            if (_thread != null)
            {
                if (!_thread.Join(1000))
                    Debug.LogWarning("OMT receiver thread did not stop in time.");
                _thread = null;
            }
            _cts?.Dispose();
            _cts = null;
            _handle?.Dispose();
            _handle = null;
            _converter?.Dispose();
            _converter = null;
            _buffer.Dispose();
            _connected = false;
            if (_runtimeHeld)
            {
                _runtimeHeld = false;
                OmtRuntime.Release();
            }
        }

        void ReceiveLoop(OmtReceiveHandle handle, CancellationToken token)
        {
            while (!token.IsCancellationRequested && handle != null && !handle.IsInvalid)
            {
                IntPtr ptr;
                try
                {
                    ptr = OmtNative.omt_receive(handle.Raw, OmtFrameType.Video | OmtFrameType.Metadata, 50);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    break;
                }

                if (ptr == IntPtr.Zero)
                    continue;

                var frame = Marshal.PtrToStructure<OmtMediaFrame>(ptr);
                if (frame.Type == OmtFrameType.Metadata)
                {
                    var xml = OmtUtf8.FromPtr(frame.Data, frame.DataLength);
                    lock (_metadataGate)
                        _connectionMetadata = xml;
                    continue;
                }

                if (frame.Type != OmtFrameType.Video || frame.Data == IntPtr.Zero || frame.Width <= 0 || frame.Height <= 0)
                    continue;

                var meta = frame.FrameMetadata != IntPtr.Zero
                    ? OmtUtf8.FromPtr(frame.FrameMetadata, frame.FrameMetadataLength)
                    : null;
                _buffer.Publish(frame.Data, frame.DataLength, new OmtVideoFrameInfo(
                    frame.Width, frame.Height, frame.Stride <= 0 ? frame.Width * 4 : frame.Stride,
                    frame.Timestamp, (frame.Flags & OmtVideoFlags.Alpha) != 0, meta));
            }
        }
    }
}
