using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LabControl.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LabControl.Infrastructure.Services;

public class WakeOnLanService : IWakeOnLanService
{
    private readonly ILogger<WakeOnLanService> _logger;

    public WakeOnLanService(ILogger<WakeOnLanService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> EnviarMagicPacketAsync(string macAddress, CancellationToken cancellationToken = default)
    {
        var packet = ConstruirMagicPacket(macAddress);
        if (packet == null)
        {
            _logger.LogWarning("Dirección MAC inválida para Wake-on-LAN: {MacAddress}", macAddress);
            return false;
        }

        try
        {
            using var client = new UdpClient();
            client.EnableBroadcast = true;

            // Transmitir en broadcast a los puertos estándar de Wake-on-LAN: puerto 9 y puerto 7
            await client.SendAsync(packet, packet.Length, new IPEndPoint(IPAddress.Broadcast, 9));
            await client.SendAsync(packet, packet.Length, new IPEndPoint(IPAddress.Broadcast, 7));

            _logger.LogInformation("Magic packet Wake-on-LAN enviado a MAC: {MacAddress}", macAddress);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar Magic Packet Wake-on-LAN a {MacAddress}", macAddress);
            return false;
        }
    }

    public async Task<int> EnviarMagicPacketBatchAsync(IEnumerable<string> macAddresses, CancellationToken cancellationToken = default)
    {
        int enviados = 0;
        foreach (var mac in macAddresses)
        {
            if (await EnviarMagicPacketAsync(mac, cancellationToken))
            {
                enviados++;
            }
        }
        return enviados;
    }

    private static byte[]? ConstruirMagicPacket(string macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress)) return null;

        var clean = Regex.Replace(macAddress, @"[^0-9a-fA-F]", "");
        if (clean.Length != 12) return null;

        byte[] macBytes = new byte[6];
        for (int i = 0; i < 6; i++)
        {
            macBytes[i] = Convert.ToByte(clean.Substring(i * 2, 2), 16);
        }

        // Magic Packet: 6 bytes de 0xFF seguidos de 16 repeticiones de la dirección MAC
        byte[] packet = new byte[102];
        for (int i = 0; i < 6; i++)
        {
            packet[i] = 0xFF;
        }

        for (int i = 1; i <= 16; i++)
        {
            Buffer.BlockCopy(macBytes, 0, packet, i * 6, 6);
        }

        return packet;
    }
}
