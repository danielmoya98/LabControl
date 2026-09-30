using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LabControl.Application.Common.Interfaces;

public interface IWakeOnLanService
{
    Task<bool> EnviarMagicPacketAsync(string macAddress, CancellationToken cancellationToken = default);
    Task<int> EnviarMagicPacketBatchAsync(IEnumerable<string> macAddresses, CancellationToken cancellationToken = default);
}
