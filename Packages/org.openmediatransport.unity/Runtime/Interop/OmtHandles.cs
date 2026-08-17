using System;
using Microsoft.Win32.SafeHandles;

namespace OpenMediaTransport.Interop
{
    internal sealed class OmtSendHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal OmtSendHandle() : base(true) {}

        internal OmtSendHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        internal IntPtr Raw => handle;

        protected override bool ReleaseHandle()
        {
            if (!IsInvalid)
                OmtNative.omt_send_destroy(handle);
            return true;
        }
    }

    internal sealed class OmtReceiveHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal OmtReceiveHandle() : base(true) {}

        internal OmtReceiveHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        internal IntPtr Raw => handle;

        protected override bool ReleaseHandle()
        {
            if (!IsInvalid)
                OmtNative.omt_receive_destroy(handle);
            return true;
        }
    }
}
