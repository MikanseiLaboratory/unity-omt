using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenMediaTransport
{
    internal sealed class OmtReadbackEntry
    {
        internal NativeArray<byte> Pixels;
        internal int Width;
        internal int Height;
        internal string Metadata;
        internal bool InFlight;

        internal void Allocate(int width, int height, string metadata)
        {
            var length = width * height * 4;
            if (!Pixels.IsCreated || Pixels.Length != length)
            {
                if (Pixels.IsCreated)
                    Pixels.Dispose();
                Pixels = new NativeArray<byte>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }
            Width = width;
            Height = height;
            Metadata = metadata;
            InFlight = true;
        }

        internal void Release()
        {
            InFlight = false;
            Metadata = null;
        }

        internal void Dispose()
        {
            if (Pixels.IsCreated)
                Pixels.Dispose();
            Pixels = default;
            InFlight = false;
        }
    }

    internal sealed class OmtReadbackPool : IDisposable
    {
        internal const int Capacity = 4;
        private readonly List<OmtReadbackEntry> _entries = new List<OmtReadbackEntry>(Capacity);
        private int _inFlight;

        internal int InFlight => _inFlight;

        internal bool TryBegin(int width, int height, string metadata, ComputeBuffer source, Action<AsyncGPUReadbackRequest, OmtReadbackEntry> callback, CommandBuffer cmd)
        {
            if (_inFlight >= Capacity)
                return false;

            var entry = _entries.Find(e => !e.InFlight);
            if (entry == null)
            {
                entry = new OmtReadbackEntry();
                _entries.Add(entry);
            }

            entry.Allocate(width, height, metadata);
            _inFlight++;
            Action<AsyncGPUReadbackRequest> wrapped = request => callback(request, entry);
            if (cmd != null)
                cmd.RequestAsyncReadbackIntoNativeArray(ref entry.Pixels, source, wrapped);
            else
                AsyncGPUReadback.RequestIntoNativeArray(ref entry.Pixels, source, wrapped);
            return true;
        }

        internal void Complete(OmtReadbackEntry entry)
        {
            if (entry == null)
                return;
            entry.Release();
            if (_inFlight > 0)
                _inFlight--;
        }

        internal void Recover()
        {
            AsyncGPUReadback.WaitAllRequests();
            foreach (var entry in _entries)
                entry.Release();
            _inFlight = 0;
        }

        public void Dispose()
        {
            AsyncGPUReadback.WaitAllRequests();
            foreach (var entry in _entries)
                entry.Dispose();
            _entries.Clear();
            _inFlight = 0;
        }
    }
}
