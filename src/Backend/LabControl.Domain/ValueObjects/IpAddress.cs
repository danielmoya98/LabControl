using System.Net;
using LabControl.Domain.Common;

namespace LabControl.Domain.ValueObjects;

public record IpAddress
{
    public string Value { get; }

    private IpAddress(string value)
    {
        Value = value;
    }

    public static Result<IpAddress> Create(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            return Result<IpAddress>.Failure(
                Error.Validation("IpAddress.Empty", "La dirección IP no puede estar vacía."));
        }

        string ipLimpia = ip.Trim();

        if (!IPAddress.TryParse(ipLimpia, out _))
        {
            return Result<IpAddress>.Failure(
                Error.Validation("IpAddress.Invalid", "Dirección IP con formato inválido."));
        }

        return Result<IpAddress>.Success(new IpAddress(ipLimpia));
    }

    public override string ToString() => Value;
}
