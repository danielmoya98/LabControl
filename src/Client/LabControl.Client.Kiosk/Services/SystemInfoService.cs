using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace LabControl.Client.Kiosk.Services;

public record HardwareInfoDto(
    string CpuModelo,
    int RamTotalGb,
    int DiscoTotalGb,
    int DiscoLibreGb,
    string SistemaOperativo,
    double UptimeHoras
);

public static class SystemInfoService
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
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

        public MEMORYSTATUSEX()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    public static string GetHostname()
    {
        return Environment.MachineName.ToUpperInvariant();
    }

    public static string GetLocalIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                {
                    return ip.ToString();
                }
            }
        }
        catch
        {
            // Fallback
        }
        return "127.0.0.1";
    }

    public static string GetMacAddress()
    {
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up &&
                    nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                {
                    var bytes = nic.GetPhysicalAddress().GetAddressBytes();
                    if (bytes.Length == 6)
                    {
                        return string.Join(":", bytes.Select(b => b.ToString("X2")));
                    }
                }
            }
        }
        catch
        {
            // Fallback
        }
        return "00:1A:2B:3C:01:01";
    }

    public static string GetCpuModel()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            var name = key?.GetValue("ProcessorNameString")?.ToString();
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name.Trim();
            }
        }
        catch
        {
            // Fallback
        }

        return $"{Environment.ProcessorCount} Cores CPU";
    }

    public static (int RamTotalGb, int RamUsoPorcentaje) GetMemoryMetrics()
    {
        try
        {
            var memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                var totalGb = (int)Math.Max(1, Math.Round((double)memStatus.ullTotalPhys / (1024 * 1024 * 1024)));
                var usoPorcentaje = (int)Math.Clamp(memStatus.dwMemoryLoad, 0, 100);
                return (totalGb, usoPorcentaje);
            }
        }
        catch
        {
            // Fallback
        }

        return (8, 0);
    }

    public static (int DiscoTotalGb, int DiscoLibreGb) GetDiskMetrics()
    {
        try
        {
            var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && string.Equals(d.Name, @"C:\", StringComparison.OrdinalIgnoreCase))
                     ?? DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.DriveType == DriveType.Fixed);

            if (drive != null)
            {
                var totalGb = (int)(drive.TotalSize / (1024L * 1024L * 1024L));
                var libreGb = (int)(drive.AvailableFreeSpace / (1024L * 1024L * 1024L));
                return (totalGb, libreGb);
            }
        }
        catch
        {
            // Fallback
        }

        return (256, 100);
    }

    public static string GetOperatingSystem()
    {
        try
        {
            return RuntimeInformation.OSDescription.Trim();
        }
        catch
        {
            return "Windows";
        }
    }

    public static double GetUptimeHours()
    {
        try
        {
            return Math.Round(TimeSpan.FromMilliseconds(Environment.TickCount64).TotalHours, 1);
        }
        catch
        {
            return 0.0;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out long lpIdleTime, out long lpKernelTime, out long lpUserTime);

    private static long _prevIdleTime;
    private static long _prevKernelTime;
    private static long _prevUserTime;
    private static int _lastCpuPercentage = 0;

    public static int GetCpuUsagePercentage()
    {
        try
        {
            if (GetSystemTimes(out long idleTime, out long kernelTime, out long userTime))
            {
                if (_prevKernelTime == 0 && _prevUserTime == 0)
                {
                    _prevIdleTime = idleTime;
                    _prevKernelTime = kernelTime;
                    _prevUserTime = userTime;
                    return 0;
                }

                var usr = userTime - _prevUserTime;
                var ker = kernelTime - _prevKernelTime;
                var idl = idleTime - _prevIdleTime;

                var sys = ker + usr;
                if (sys > 0)
                {
                    var cpu = (int)Math.Clamp(Math.Round(((double)(sys - idl) / sys) * 100), 0, 100);
                    _lastCpuPercentage = cpu;
                }

                _prevIdleTime = idleTime;
                _prevKernelTime = kernelTime;
                _prevUserTime = userTime;
            }
        }
        catch
        {
            // Fallback
        }

        return _lastCpuPercentage;
    }

    public static HardwareInfoDto GetHardwareInfo()
    {
        var cpu = GetCpuModel();
        var (ramTotal, _) = GetMemoryMetrics();
        var (discoTotal, discoLibre) = GetDiskMetrics();
        var os = GetOperatingSystem();
        var uptime = GetUptimeHours();

        return new HardwareInfoDto(cpu, ramTotal, discoTotal, discoLibre, os, uptime);
    }
}
