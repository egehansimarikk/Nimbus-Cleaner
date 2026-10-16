using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WinCleaner
{
    internal static class WinApi
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MemoryStatusEx
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        public const uint SHERB_NOCONFIRMATION = 0x1;
        public const uint SHERB_NOPROGRESSUI = 0x2;
        public const uint SHERB_NOSOUND = 0x4;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

        public static bool TryGetMemoryUsage(out int usagePercent)
        {
            var status = new MemoryStatusEx { dwLength = (uint)Marshal.SizeOf(typeof(MemoryStatusEx)) };
            if (!GlobalMemoryStatusEx(ref status))
            {
                usagePercent = 0;
                return false;
            }

            usagePercent = (int)status.dwMemoryLoad;
            return true;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);

        [DllImport("shell32.dll")]
        public static extern bool IsUserAnAdmin();

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr ShellExecute(IntPtr hwnd, string lpOperation, string lpFile, string lpParameters, string lpDirectory, int nShowCmd);

        [DllImport("dnsapi.dll")]
        public static extern uint DnsFlushResolverCache();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool GetComputerName(StringBuilder lpBuffer, ref uint nSize);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static void TryDarkTitle(IntPtr hwnd)
        {
            try
            {
                int on = 1;
                DwmSetWindowAttribute(hwnd, 20, ref on, 4);
                DwmSetWindowAttribute(hwnd, 19, ref on, 4);
            }
            catch { }
        }
    }
}
