using System.Text.RegularExpressions;
using LabControl.Domain.Common;

namespace LabControl.Domain.ValueObjects;

public partial record EmailInstitucional
{
    public string Value { get; }

    [GeneratedRegex(@"^[a-zA-Z0-9._%+-]+@est\.univalle\.edu$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    private EmailInstitucional(string value)
    {
        Value = value;
    }

    public static Result<EmailInstitucional> Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<EmailInstitucional>.Failure(
                Error.Validation("Email.Empty", "El correo institucional no puede estar vacío."));
        }

        string emailLimpio = email.Trim().ToLowerInvariant();

        if (!EmailRegex().IsMatch(emailLimpio))
        {
            return Result<EmailInstitucional>.Failure(
                Error.Validation("Email.InvalidDomain", "Acceso restringido: Ingrese un correo @est.univalle.edu válido."));
        }

        return Result<EmailInstitucional>.Success(new EmailInstitucional(emailLimpio));
    }

    public override string ToString() => Value;
}
