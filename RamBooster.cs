using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace WinCleaner
{
    public sealed class RamBoostResult
    {
        public long AvailableBeforeBytes { get; internal set; }
        public long AvailableAfterBytes { get; internal set; }
        public int UsagePercentAfter { get; internal set; }
        public int ProcessesOptimized { get; internal set; }
        public int ProcessesSkipped { get; internal set; }

        public long ObservedAvailableBytesDelta => AvailableAfterBytes - AvailableBeforeBytes;
    }

    public static class RamBooster
    {
        private const uint ProcessSetQuota = 0x0100;
        private const uint ProcessQueryInformation = 0x0400;

        private static readonly HashSet<string> CriticalProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Idle", "System", "SYSTEM", "csrss", "smss", "wininit", "winlogon", "services", "lsass", "Registry"
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MemoryStatusEx
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhysical;
            public ulong AvailablePhysical;
            public ulong TotalPageFile;
            public ulong AvailablePageFile;
            public ulong TotalVirtual;
            public ulong AvailableVirtual;
            public ulong AvailableExtendedVirtual;
        }

        [DllImport("psapi.dll")]
        public static extern bool EmptyWorkingSet(IntPtr hProcess);

        [DllImport("kernel32.dll")]
        public static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

        public static RamBoostResult Optimize(CancellationToken cancellationToken)
        {
            var before = GetMemoryStatus();
            var result = new RamBoostResult { AvailableBeforeBytes = checked((long)before.AvailablePhysical) };

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var processes = Process.GetProcesses();
            try
            {
                foreach (var process in processes)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        if (IsCriticalProcess(process))
                        {
                            result.ProcessesSkipped++;
                            continue;
                        }

                        var handle = OpenProcess(ProcessSetQuota | ProcessQueryInformation, false, process.Id);
                        if (handle == IntPtr.Zero)
                        {
                            result.ProcessesSkipped++;
                            continue;
                        }

                        try
                        {
                            var setWorkingSet = SetProcessWorkingSetSize(handle, new IntPtr(-1), new IntPtr(-1));
                            var emptiedWorkingSet = EmptyWorkingSet(handle);

                            if (setWorkingSet || emptiedWorkingSet)
                                result.ProcessesOptimized++;
                            else
                                result.ProcessesSkipped++;
                        }
                        finally
                        {
                            CloseHandle(handle);
                        }
                    }
                    catch
                    {
                        result.ProcessesSkipped++;
                    }
                }
            }
            finally
            {
                foreach (var process in processes)
                    process.Dispose();
            }

            cancellationToken.ThrowIfCancellationRequested();

            var after = GetMemoryStatus();
            result.AvailableAfterBytes = checked((long)after.AvailablePhysical);
            result.UsagePercentAfter = (int)after.MemoryLoad;
            return result;
        }

        private static bool IsCriticalProcess(Process process)
        {
            return process.Id == 0 || process.Id == 4 || CriticalProcessNames.Contains(process.ProcessName);
        }

        private static MemoryStatusEx GetMemoryStatus()
        {
            var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf(typeof(MemoryStatusEx)) };
            if (!GlobalMemoryStatusEx(ref status))
                throw new InvalidOperationException("Fiziksel bellek durumu okunamadı.");

            return status;
        }
    }
}
