using System;

/// <summary>
/// Base classes needed to 
/// - Get adapters
/// - Construct packets
/// - Send packets
/// </summary>
namespace WinLLDPService
{
    internal static class NativeMethods
    {
        // Uptime
        [System.Runtime.InteropServices.DllImport("kernel32")]
        internal static extern UInt64 GetTickCount64();

    }
}