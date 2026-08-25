using System.Text.RegularExpressions;
using LabControl.Domain.Common;

namespace LabControl.Domain.ValueObjects;

public partial record MacAddress
{
    public string Value { get; }

    [GeneratedRegex(@"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$", RegexOptions.Compiled)]
    private static partial Regex MacRegex();

    private MacAddress(string value)
    {
        Value = value;
    }

    public static Result<MacAddress> Create(string mac)
    {
        if (string.IsNullOrWhiteSpace(mac))
        {
            return Result<MacAddress>.Failure(
                Error.Validation("MacAddress.Empty", "La dirección MAC no puede estar vacía."));
        }

        string macFormateada = mac.Trim().ToUpperInvariant().Replace('-', ':');

        if (!MacRegex().IsMatch(macFormateada))
        {
            return Result<MacAddress>.Failure(
                Error.Validation("MacAddress.Invalid", "Formato de dirección MAC inválido."));
        }

        return Result<MacAddress>.Success(new MacAddress(macFormateada));
    }

    public override string ToString() => Value;
}
