using System;
using UnityEngine;

namespace OpenMediaTransport
{
    public readonly struct OmtVideoFrameInfo
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int Stride;
        public readonly long Timestamp;
        public readonly bool HasAlpha;
        public readonly string Metadata;

        public OmtVideoFrameInfo(int width, int height, int stride, long timestamp, bool hasAlpha, string metadata)
        {
            Width = width;
            Height = height;
            Stride = stride;
            Timestamp = timestamp;
            HasAlpha = hasAlpha;
            Metadata = metadata;
        }
    }

    internal sealed class OmtLatestVideoBuffer : IDisposable
    {
        private readonly object _gate = new object();
        private byte[] _front = Array.Empty<byte>();
        private byte[] _back = Array.Empty<byte>();
        private OmtVideoFrameInfo _info;
        private int _version;
        private int _dropped;

        public int Dropped => _dropped;

        public void Publish(IntPtr data, int length, OmtVideoFrameInfo info)
        {
            if (data == IntPtr.Zero || length <= 0)
                return;

            lock (_gate)
            {
                if (_back.Length < length)
                    _back = new byte[length];
                System.Runtime.InteropServices.Marshal.Copy(data, _back, 0, length);
                _info = info;
                var tmp = _front;
                _front = _back;
                _back = tmp;
                _version++;
            }
        }

        public void NotifyDropped()
        {
            _dropped++;
        }

        public bool TryConsume(ref int seenVersion, out OmtVideoFrameInfo info, out byte[] pixels)
        {
            lock (_gate)
            {
                if (_version == seenVersion || _front.Length == 0)
                {
                    info = default;
                    pixels = null;
                    return false;
                }

                if (_version > seenVersion + 1)
                    _dropped += _version - seenVersion - 1;

                seenVersion = _version;
                info = _info;
                pixels = _front;
                return true;
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                _front = Array.Empty<byte>();
                _back = Array.Empty<byte>();
                _version = 0;
            }
        }
    }
}
