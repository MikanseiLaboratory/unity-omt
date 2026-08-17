using System;
using System.Runtime.InteropServices;

namespace OpenMediaTransport.Interop
{
    public enum OmtFrameType
    {
        None = 0,
        Metadata = 1,
        Video = 2,
        Audio = 4
    }

    public enum OmtCodec
    {
        VMX1 = 0x31584D56,
        FPA1 = 0x31415046,
        UYVY = 0x59565955,
        YUY2 = 0x32595559,
        BGRA = 0x41524742,
        NV12 = 0x3231564E,
        YV12 = 0x32315659,
        UYVA = 0x41565955,
        P216 = 0x36313250,
        PA16 = 0x36314150
    }

    public enum OmtQuality
    {
        Default = 0,
        Low = 1,
        Medium = 50,
        High = 100
    }

    public enum OmtColorSpace
    {
        Undefined = 0,
        BT601 = 601,
        BT709 = 709
    }

    [Flags]
    public enum OmtVideoFlags
    {
        None = 0,
        Interlaced = 1,
        Alpha = 2,
        PreMultiplied = 4,
        Preview = 8,
        HighBitDepth = 16
    }

    public enum OmtPreferredVideoFormat
    {
        UYVY = 0,
        UYVYorBGRA = 1,
        BGRA = 2,
        UYVYorUYVA = 3,
        UYVYorUYVAorP216orPA16 = 4,
        P216 = 5
    }

    [Flags]
    public enum OmtReceiveFlags
    {
        None = 0,
        Preview = 1,
        IncludeCompressed = 2,
        CompressedOnly = 4
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OmtTally
    {
        public int Preview;
        public int Program;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OmtStatistics
    {
        public long BytesSent;
        public long BytesReceived;
        public long BytesSentSinceLast;
        public long BytesReceivedSinceLast;
        public long Frames;
        public long FramesSinceLast;
        public long FramesDropped;
        public long CodecTime;
        public long CodecTimeSinceLast;
        public long Reserved1;
        public long Reserved2;
        public long Reserved3;
        public long Reserved4;
        public long Reserved5;
        public long Reserved6;
        public long Reserved7;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OmtSenderInfoNative
    {
        public const int MaxStringLength = 1024;
        public const int Size = MaxStringLength * 6;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxStringLength)]
        public byte[] ProductName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxStringLength)]
        public byte[] Manufacturer;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxStringLength)]
        public byte[] Version;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxStringLength)]
        public byte[] Reserved1;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxStringLength)]
        public byte[] Reserved2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MaxStringLength)]
        public byte[] Reserved3;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OmtMediaFrame
    {
        public OmtFrameType Type;
        public long Timestamp;
        public OmtCodec Codec;
        public int Width;
        public int Height;
        public int Stride;
        public OmtVideoFlags Flags;
        public int FrameRateN;
        public int FrameRateD;
        public float AspectRatio;
        public OmtColorSpace ColorSpace;
        public int SampleRate;
        public int Channels;
        public int SamplesPerChannel;
        public IntPtr Data;
        public int DataLength;
        public IntPtr CompressedData;
        public int CompressedLength;
        public IntPtr FrameMetadata;
        public int FrameMetadataLength;
    }

    public static class OmtAbi
    {
        public const int MediaFrameSizeX64 = 112;
        public const int StatisticsSize = 128;
        public const int TallySize = 8;
        public const int TimestampTicksPerSecond = 10000000;
    }
}
