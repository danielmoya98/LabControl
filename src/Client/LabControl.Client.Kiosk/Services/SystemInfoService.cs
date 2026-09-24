using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace LabControl.Client.Kiosk.Services;

public record DiscoUnidadInfo(string Nombre, string Etiqueta, string Tipo, int TotalGb, int LibreGb);

public record HardwareInfoDto(
    string CpuModelo,
    int RamTotalGb,
    int DiscoTotalGb,
    int DiscoLibreGb,
    string SistemaOperativo,
    double UptimeHoras,
    string? DiscosDetalleJson = null
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
            var allNics = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                            n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .ToList();

            static bool EsValida(byte[] bytes) =>
                bytes.Length == 6 &&
                bytes.Any(b => b != 0) &&
                bytes.Any(b => b != 0xFF) &&
                (bytes[0] & 0x01) == 0; // Descarta multicast y broadcast

            static bool EsFisica(NetworkInterface nic)
            {
                var desc = (nic.Description ?? "").ToLowerInvariant();
                var name = (nic.Name ?? "").ToLowerInvariant();
                if (desc.Contains("virtual") || desc.Contains("vmware") || desc.Contains("hyper-v") ||
                    desc.Contains("vethernet") || desc.Contains("virtualbox") || desc.Contains("bluetooth") ||
                    desc.Contains("tailscale") || desc.Contains("zerotier") || desc.Contains("tap") ||
                    desc.Contains("npcap") || desc.Contains("tunnel") || desc.Contains("wireguard") ||
                    desc.Contains("fortinet") || desc.Contains("cisco") || desc.Contains("vpn") ||
                    name.Contains("vethernet") || name.Contains("bluetooth") || name.Contains("loopback"))
                {
                    return false;
                }
                return nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                       nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211;
            }

            // 1. Prioridad: Tarjeta física Ethernet o Wi-Fi que esté conectada y activa (Up)
            var physicalUp = allNics.FirstOrDefault(n => EsFisica(n) && n.OperationalStatus == OperationalStatus.Up && EsValida(n.GetPhysicalAddress().GetAddressBytes()));
            if (physicalUp != null)
            {
                return string.Join(":", physicalUp.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));
            }

            // 2. Tarjeta física Ethernet o Wi-Fi (incluso si está desconectada temporalmente)
            var physicalAny = allNics.FirstOrDefault(n => EsFisica(n) && EsValida(n.GetPhysicalAddress().GetAddressBytes()));
            if (physicalAny != null)
            {
                return string.Join(":", physicalAny.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));
            }

            // 3. Cualquier interfaz activa con MAC válida
            var anyUp = allNics.FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up && EsValida(n.GetPhysicalAddress().GetAddressBytes()));
            if (anyUp != null)
            {
                return string.Join(":", anyUp.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));
            }

            // 4. Cualquier interfaz no vacía
            var anyValid = allNics.FirstOrDefault(n => EsValida(n.GetPhysicalAddress().GetAddressBytes()));
            if (anyValid != null)
            {
                return string.Join(":", anyValid.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));
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

    public static List<DiscoUnidadInfo> GetAllDrives()
    {
        var result = new List<DiscoUnidadInfo>();
        try
        {
            foreach (var d in DriveInfo.GetDrives())
            {
                if (d.IsReady && (d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable))
                {
                    var totalGb = (int)(d.TotalSize / (1024L * 1024L * 1024L));
                    var libreGb = (int)(d.AvailableFreeSpace / (1024L * 1024L * 1024L));
                    var etiqueta = string.IsNullOrWhiteSpace(d.VolumeLabel) ? d.DriveType.ToString() : d.VolumeLabel;
                    result.Add(new DiscoUnidadInfo(d.Name.TrimEnd('\\'), etiqueta, d.DriveType.ToString(), totalGb, libreGb));
                }
            }
        }
        catch
        {
            // Fallback
        }
        return result;
    }

    public static HardwareInfoDto GetHardwareInfo()
    {
        var cpu = GetCpuModel();
        var (ramTotal, _) = GetMemoryMetrics();
        var (discoTotal, discoLibre) = GetDiskMetrics();
        var os = GetOperatingSystem();
        var uptime = GetUptimeHours();
        var drives = GetAllDrives();
        string? discosJson = null;
        try
        {
            discosJson = System.Text.Json.JsonSerializer.Serialize(drives);
        }
        catch { }

        return new HardwareInfoDto(cpu, ramTotal, discoTotal, discoLibre, os, uptime, discosJson);
    }
}
