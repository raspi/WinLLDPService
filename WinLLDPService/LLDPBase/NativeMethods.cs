namespace WinLLDPService
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// Native methods
    /// </summary>
    internal static class NativeMethods
    {
        // Uptime
        [DllImport("kernel32")]
        internal static extern ulong GetTickCount64();
    }
}