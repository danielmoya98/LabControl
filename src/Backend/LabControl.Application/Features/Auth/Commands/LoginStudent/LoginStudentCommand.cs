using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;
using LabControl.Domain.Enums;
using LabControl.Domain.ValueObjects;

namespace LabControl.Application.Features.Auth.Commands.LoginStudent;

public record LoginStudentCommand(
    string Email,
    string Hostname,
    string MacAddress,
    string IpAddress
) : IRequest<Result<LoginStudentResponseDto>>;

public record LoginStudentResponseDto(
    string Token,
    string Email,
    int ComputadoraId,
    string Hostname,
    int AulaId
);

public class LoginStudentCommandValidator : AbstractValidator<LoginStudentCommand>
{
    public LoginStudentCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo es requerido.")
            .Must(email => EmailInstitucional.Create(email).IsSuccess)
            .WithMessage("Acceso restringido: Ingrese un correo institucional válido (@est.univalle.edu o @univalle.edu).");

        RuleFor(x => x.Hostname)
            .NotEmpty().WithMessage("El hostname de la terminal es requerido.");

        RuleFor(x => x.MacAddress)
            .NotEmpty().WithMessage("La dirección MAC es requerida.");
    }
}

public class LoginStudentCommandHandler : IRequestHandler<LoginStudentCommand, Result<LoginStudentResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly ISignalRNotificationService _signalRService;

    public LoginStudentCommandHandler(
        IApplicationDbContext context,
        IJwtTokenGenerator jwtGenerator,
        ISignalRNotificationService signalRService)
    {
        _context = context;
        _jwtGenerator = jwtGenerator;
        _signalRService = signalRService;
    }

    public async Task<Result<LoginStudentResponseDto>> Handle(LoginStudentCommand request, CancellationToken cancellationToken)
    {
        var emailResult = EmailInstitucional.Create(request.Email);
        if (emailResult.IsFailure)
        {
            return Result<LoginStudentResponseDto>.Failure(emailResult.Error);
        }

        var computadora = await _context.Computadoras
            .FirstOrDefaultAsync(c => c.Hostname == request.Hostname.ToUpperInvariant(), cancellationToken);

        if (computadora == null)
        {
            return Result<LoginStudentResponseDto>.Failure(
                Error.NotFound("Computadora.NotFound", $"La computadora con Hostname '{request.Hostname}' no está registrada."));
        }

        computadora.CambiarEstado(EstadoComputadora.EnUso);
        await _context.SaveChangesAsync(cancellationToken);

        await _signalRService.NotifyEstadoComputadoraCambiadoAsync(
            computadora.Id,
            computadora.Hostname,
            EstadoComputadora.EnUso,
            emailResult.Value.Value,
            cancellationToken);

        string token = _jwtGenerator.GenerateToken(
            computadora.Id.ToString(),
            emailResult.Value.Value,
            "Estudiante",
            new Dictionary<string, string>
            {
                { "Hostname", computadora.Hostname },
                { "AulaId", computadora.AulaId.ToString() }
            });

        var response = new LoginStudentResponseDto(
            token,
            emailResult.Value.Value,
            computadora.Id,
            computadora.Hostname,
            computadora.AulaId
        );

        return Result<LoginStudentResponseDto>.Success(response);
    }
}
