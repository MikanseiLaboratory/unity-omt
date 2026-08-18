using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenMediaTransport.Interop;

internal static class Program
{
    private const int Width = 256;
    private const int Height = 144;
    private const int Port = 6521;
    private const string SourceName = "UnityNativeSpike";

    private static int Main()
    {
        Console.WriteLine("OMT native spike starting");
        Environment.SetEnvironmentVariable("OMT_STORAGE_PATH", Path.Combine(Path.GetTempPath(), "omt-unity-spike"));

        try
        {
            AssertAbi();
            RunLoopback();
            Console.WriteLine("PASS");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("FAIL: " + ex);
            return 1;
        }
        finally
        {
            try { OmtNative.omt_shutdown(); }
            catch (EntryPointNotFoundException)
            {
                Console.WriteLine("omt_shutdown is unavailable in this libomt build");
            }
            catch (Exception ex) { Console.Error.WriteLine("shutdown: " + ex.Message); }
        }
    }

    private static void AssertAbi()
    {
        var frameSize = Marshal.SizeOf<OmtMediaFrame>();
        var statsSize = Marshal.SizeOf<OmtStatistics>();
        var tallySize = Marshal.SizeOf<OmtTally>();
        Console.WriteLine($"ABI Type={Marshal.OffsetOf<OmtMediaFrame>(nameof(OmtMediaFrame.Type))} Timestamp={Marshal.OffsetOf<OmtMediaFrame>(nameof(OmtMediaFrame.Timestamp))} Data={Marshal.OffsetOf<OmtMediaFrame>(nameof(OmtMediaFrame.Data))} size={frameSize}");
        Console.WriteLine($"ABI OmtStatistics={statsSize} OmtTally={tallySize} IntPtr={IntPtr.Size}");

        if (IntPtr.Size == 8 && frameSize != OmtAbi.MediaFrameSizeX64)
            throw new InvalidOperationException($"Unexpected OmtMediaFrame size {frameSize}, expected {OmtAbi.MediaFrameSizeX64}");
        if (statsSize != OmtAbi.StatisticsSize)
            throw new InvalidOperationException($"Unexpected OmtStatistics size {statsSize}, expected {OmtAbi.StatisticsSize}");
        if (tallySize != OmtAbi.TallySize)
            throw new InvalidOperationException($"Unexpected OmtTally size {tallySize}, expected {OmtAbi.TallySize}");
        if ((int)Marshal.OffsetOf<OmtMediaFrame>(nameof(OmtMediaFrame.Data)) != 64)
            throw new InvalidOperationException("Unexpected Data offset");
    }

    private static void SetInteger(string name, int value)
    {
        var ptr = OmtUtf8.Alloc(name);
        try { OmtNative.omt_settings_set_integer(ptr, value); }
        finally { OmtUtf8.Free(ptr); }
    }

    private static void RunLoopback()
    {
        SetInteger("NetworkPortStart", Port);
        SetInteger("NetworkPortEnd", Port);

        var logPtr = OmtUtf8.Alloc(Path.Combine(Path.GetTempPath(), "omt-unity-spike.log"));
        OmtNative.omt_setloggingfilename(logPtr);
        OmtUtf8.Free(logPtr);

        var namePtr = OmtUtf8.Alloc(SourceName);
        var send = IntPtr.Zero;
        var receive = IntPtr.Zero;
        var pixels = IntPtr.Zero;
        var metadata = IntPtr.Zero;
        var framePtr = Marshal.AllocHGlobal(Marshal.SizeOf<OmtMediaFrame>());
        try
        {
            send = OmtNative.omt_send_create(namePtr, OmtQuality.Medium);
            if (send == IntPtr.Zero)
                throw new InvalidOperationException("omt_send_create returned null");

            var addressBuf = Marshal.AllocHGlobal(1024);
            try
            {
                OmtNative.omt_send_getaddress(send, addressBuf, 1024);
                Console.WriteLine("Sender address '" + OmtUtf8.FromPtr(addressBuf) + "'");
            }
            finally
            {
                Marshal.FreeHGlobal(addressBuf);
            }

            var receiveAddress = OmtUtf8.Alloc("omt://127.0.0.1:" + Port);
            try
            {
                receive = OmtNative.omt_receive_create(receiveAddress, OmtFrameType.Video | OmtFrameType.Metadata, OmtPreferredVideoFormat.BGRA, OmtReceiveFlags.None);
            }
            finally
            {
                OmtUtf8.Free(receiveAddress);
            }

            if (receive == IntPtr.Zero)
                throw new InvalidOperationException("omt_receive_create returned null");

            var stride = Width * 4;
            var dataLength = stride * Height;
            pixels = Marshal.AllocHGlobal(dataLength);
            FillBgra(pixels, dataLength, 0xFF, 0x33, 0xCC, 0xFF);
            metadata = OmtUtf8.Alloc("<OMTMetadata Test=\"spike\" />");

            var frame = new OmtMediaFrame
            {
                Type = OmtFrameType.Video,
                Timestamp = 1,
                Codec = OmtCodec.BGRA,
                Width = Width,
                Height = Height,
                Stride = stride,
                Flags = OmtVideoFlags.Alpha,
                FrameRateN = 60,
                FrameRateD = 1,
                AspectRatio = Width / (float)Height,
                ColorSpace = OmtColorSpace.BT709,
                Data = pixels,
                DataLength = dataLength,
                FrameMetadata = metadata,
                FrameMetadataLength = EncodingLength(metadata)
            };

            var sw = Stopwatch.StartNew();
            OmtNative.omt_receive(receive, OmtFrameType.Video, 200);
            Console.WriteLine("receive(200ms) elapsed " + sw.ElapsedMilliseconds + "ms connections=" + OmtNative.omt_send_connections(send));

            var received = false;
            string receivedMetadata = null;
            var deadline = DateTime.UtcNow.AddSeconds(8);
            while (DateTime.UtcNow < deadline)
            {
                Marshal.StructureToPtr(frame, framePtr, false);
                var sent = OmtNative.omt_send(send, framePtr);
                var connections = OmtNative.omt_send_connections(send);
                frame.Timestamp += OmtAbi.TimestampTicksPerSecond / 60;

                var ptr = OmtNative.omt_receive(receive, OmtFrameType.Video | OmtFrameType.Metadata, 40);
                if (ptr == IntPtr.Zero)
                {
                    if (connections > 0)
                        Console.WriteLine("connected send=" + sent);
                    continue;
                }

                var incoming = Marshal.PtrToStructure<OmtMediaFrame>(ptr);
                if (incoming.Type == OmtFrameType.Metadata)
                {
                    receivedMetadata = OmtUtf8.FromPtr(incoming.Data, incoming.DataLength);
                    continue;
                }

                if (incoming.Type != OmtFrameType.Video)
                    continue;
                if (incoming.Width != Width || incoming.Height != Height)
                    throw new InvalidOperationException($"Unexpected size {incoming.Width}x{incoming.Height}");
                if (incoming.Data == IntPtr.Zero || incoming.DataLength < 4)
                    throw new InvalidOperationException("Received empty video payload");

                var b = Marshal.ReadByte(incoming.Data, 0);
                var g = Marshal.ReadByte(incoming.Data, 1);
                var r = Marshal.ReadByte(incoming.Data, 2);
                if (b < 200 || g > 80 || r < 160)
                    throw new InvalidOperationException($"Unexpected BGRA {b},{g},{r}");

                if (incoming.FrameMetadata != IntPtr.Zero)
                    receivedMetadata = OmtUtf8.FromPtr(incoming.FrameMetadata, incoming.FrameMetadataLength);

                received = true;
                Console.WriteLine($"Received video {incoming.Width}x{incoming.Height} codec=0x{((int)incoming.Codec):X8} metadata='{receivedMetadata}' connections={connections} send={sent}");
                break;
            }

            Console.WriteLine("Discovery: " + string.Join(", ", ListSources()));
            if (!received)
                throw new TimeoutException("Did not receive a loopback video frame");
        }
        finally
        {
            if (receive != IntPtr.Zero)
                OmtNative.omt_receive_destroy(receive);
            if (send != IntPtr.Zero)
                OmtNative.omt_send_destroy(send);
            if (pixels != IntPtr.Zero)
                Marshal.FreeHGlobal(pixels);
            Marshal.FreeHGlobal(framePtr);
            OmtUtf8.Free(metadata);
            OmtUtf8.Free(namePtr);
        }
    }

    private static string[] ListSources()
    {
        var array = OmtNative.omt_discovery_getaddresses(out var count);
        if (array == IntPtr.Zero || count <= 0)
            return Array.Empty<string>();

        var names = new string[count];
        for (var i = 0; i < count; i++)
        {
            var item = Marshal.ReadIntPtr(array, i * IntPtr.Size);
            names[i] = OmtUtf8.FromPtr(item) ?? string.Empty;
        }
        return names;
    }

    private static void FillBgra(IntPtr pixels, int length, byte b, byte g, byte r, byte a)
    {
        var data = new byte[length];
        for (var i = 0; i < length; i += 4)
        {
            data[i] = b;
            data[i + 1] = g;
            data[i + 2] = r;
            data[i + 3] = a;
        }
        Marshal.Copy(data, 0, pixels, length);
    }

    private static int EncodingLength(IntPtr utf8)
    {
        if (utf8 == IntPtr.Zero)
            return 0;
        var length = 0;
        while (Marshal.ReadByte(utf8, length) != 0)
            length++;
        return length + 1;
    }
}
