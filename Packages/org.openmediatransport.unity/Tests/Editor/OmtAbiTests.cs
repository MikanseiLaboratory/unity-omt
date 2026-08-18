using System.Runtime.InteropServices;
using NUnit.Framework;
using OpenMediaTransport.Interop;

namespace OpenMediaTransport.Tests
{
    public class OmtAbiTests
    {
        [Test]
        public void MediaFrameLayoutMatchesNativeAotAbi()
        {
            Assert.AreEqual(112, Marshal.SizeOf<OmtMediaFrame>());
            Assert.AreEqual(0, (int)Marshal.OffsetOf<OmtMediaFrame>(nameof(OmtMediaFrame.Type)));
            Assert.AreEqual(8, (int)Marshal.OffsetOf<OmtMediaFrame>(nameof(OmtMediaFrame.Timestamp)));
            Assert.AreEqual(16, (int)Marshal.OffsetOf<OmtMediaFrame>(nameof(OmtMediaFrame.Codec)));
            Assert.AreEqual(64, (int)Marshal.OffsetOf<OmtMediaFrame>(nameof(OmtMediaFrame.Data)));
            Assert.AreEqual(96, (int)Marshal.OffsetOf<OmtMediaFrame>(nameof(OmtMediaFrame.FrameMetadata)));
        }

        [Test]
        public void StatisticsAndTallySizesMatchHeader()
        {
            Assert.AreEqual(128, Marshal.SizeOf<OmtStatistics>());
            Assert.AreEqual(8, Marshal.SizeOf<OmtTally>());
        }

        [Test]
        public void FourCcValuesMatchLibomt()
        {
            Assert.AreEqual(0x41524742, (int)OmtCodec.BGRA);
            Assert.AreEqual(0x31584D56, (int)OmtCodec.VMX1);
            Assert.AreEqual(2, (int)OmtPreferredVideoFormat.BGRA);
        }

        [Test]
        public void Utf8RoundTrip()
        {
            var ptr = OmtUtf8.Alloc("HOSTNAME (Unity Sender)");
            try
            {
                Assert.AreEqual("HOSTNAME (Unity Sender)", OmtUtf8.FromPtr(ptr));
            }
            finally
            {
                OmtUtf8.Free(ptr);
            }
        }
    }
}
