using System;
using System.ServiceProcess;
using System.Runtime.InteropServices;

namespace WinLLDPService
{
    public partial class WinLLDPService : ServiceBase
    {

        internal static class NativeMethods
        {
            [DllImport("psapi.dll")]
            public static extern bool EmptyWorkingSet(IntPtr hProcess);

        }
    }
}
