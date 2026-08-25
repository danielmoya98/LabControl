using FluentValidation;
using MediatR;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;

namespace LabControl.Application.Features.Auth.Commands.LoginAdmin;

public record LoginAdminCommand(
    string Email,
    string Password
) : IRequest<Result<LoginAdminResponseDto>>;

public record LoginAdminResponseDto(
    string Token,
    string Email,
    string NombreCompleto,
    string Rol
);

public class LoginAdminCommandValidator : AbstractValidator<LoginAdminCommand>
{
    public LoginAdminCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Correo electrónico inválido.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("La contraseña es requerida.");
    }
}

public class LoginAdminCommandHandler : IRequestHandler<LoginAdminCommand, Result<LoginAdminResponseDto>>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _jwtGenerator;

    public LoginAdminCommandHandler(
        IIdentityService identityService,
        IJwtTokenGenerator jwtGenerator)
    {
        _identityService = identityService;
        _jwtGenerator = jwtGenerator;
    }

    public async Task<Result<LoginAdminResponseDto>> Handle(LoginAdminCommand request, CancellationToken cancellationToken)
    {
        var authResult = await _identityService.ValidateAdminPasswordAsync(request.Email, request.Password, cancellationToken);
        if (!authResult.Success)
        {
            return Result<LoginAdminResponseDto>.Failure(
                Error.Unauthorized("Auth.InvalidCredentials", "Credenciales inválidas o usuario inactivo."));
        }

        string token = _jwtGenerator.GenerateToken(
            authResult.UserId,
            authResult.Email,
            authResult.Role,
            new Dictionary<string, string>
            {
                { "NombreCompleto", authResult.NombreCompleto }
            });

        return Result<LoginAdminResponseDto>.Success(new LoginAdminResponseDto(
            token,
            authResult.Email,
            authResult.NombreCompleto,
            authResult.Role
        ));
    }
}
