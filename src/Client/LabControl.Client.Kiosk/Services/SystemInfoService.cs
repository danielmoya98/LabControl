using System.Net;
using System.Net.NetworkInformation;

namespace LabControl.Client.Kiosk.Services;

public static class SystemInfoService
{
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
}
