namespace LabControl.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(string userId, string email, string role, IDictionary<string, string>? additionalClaims = null);
}
