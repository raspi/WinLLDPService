namespace WinLLDPService
{
    using System;
    using System.Runtime.InteropServices;
    using System.ServiceProcess;

    /// <summary>
    /// Native methods
    /// </summary>
    public partial class WinLLDPService : ServiceBase
    {
        /// <summary>
        /// The native methods.
        /// </summary>
        internal static class NativeMethods
        {
            [DllImport("psapi.dll")]
            public static extern bool EmptyWorkingSet(IntPtr hProcess);

        }
    }
}
