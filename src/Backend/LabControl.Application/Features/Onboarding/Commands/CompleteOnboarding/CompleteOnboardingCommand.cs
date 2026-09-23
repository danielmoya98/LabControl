using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;
using LabControl.Domain.Entities;
using LabControl.Domain.ValueObjects;

namespace LabControl.Application.Features.Onboarding.Commands.CompleteOnboarding;

public record BloqueOnboardingDto(
    string Nombre,
    string Codigo,
    bool TienePisos,
    int? TotalPisos,
    string? Descripcion,
    List<AulaOnboardingDto> Aulas
);

public record AulaOnboardingDto(
    string Nombre,
    string? Piso,
    int Capacidad,
    int MinutosInactividad = 15,
    bool AutoGenerarComputadoras = true
);

public record CompleteOnboardingCommand(
    string SedeNombre,
    string SedeCodigo,
    string SedeCiudad,
    string? SedeDireccion,
    string EncargadoNombreCompleto,
    string EncargadoEmail,
    string EncargadoPassword,
    List<BloqueOnboardingDto> Bloques
) : IRequest<Result<CompleteOnboardingResponseDto>>;

public record CompleteOnboardingResponseDto(
    int SedeId,
    string SedeNombre,
    string EncargadoEmail,
    string EncargadoNombre,
    string Token,
    int TotalBloquesCreados,
    int TotalAulasCreadas,
    int TotalComputadorasCreadas
);

public class CompleteOnboardingCommandValidator : AbstractValidator<CompleteOnboardingCommand>
{
    public CompleteOnboardingCommandValidator()
    {
        RuleFor(x => x.SedeNombre).NotEmpty().WithMessage("El nombre de la sede es requerido.");
        RuleFor(x => x.SedeCodigo).NotEmpty().WithMessage("El código de la sede es requerido.");
        RuleFor(x => x.SedeCiudad).NotEmpty().WithMessage("La ciudad de la sede es requerida.");
        RuleFor(x => x.EncargadoNombreCompleto).NotEmpty().WithMessage("El nombre completo del encargado es requerido.");
        RuleFor(x => x.EncargadoEmail).NotEmpty().EmailAddress().WithMessage("El correo electrónico del encargado es inválido.");
        RuleFor(x => x.EncargadoPassword).MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres.");
        RuleFor(x => x.Bloques).NotEmpty().WithMessage("Debe registrar al menos un bloque.");
    }
}

public class CompleteOnboardingCommandHandler : IRequestHandler<CompleteOnboardingCommand, Result<CompleteOnboardingResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _jwtGenerator;

    public CompleteOnboardingCommandHandler(
        IApplicationDbContext context,
        IIdentityService identityService,
        IJwtTokenGenerator jwtGenerator)
    {
        _context = context;
        _identityService = identityService;
        _jwtGenerator = jwtGenerator;
    }

    public async Task<Result<CompleteOnboardingResponseDto>> Handle(CompleteOnboardingCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar que no haya un onboarding completado previamente
        var sedeExistente = await _context.Sedes.FirstOrDefaultAsync(cancellationToken);
        if (sedeExistente != null && sedeExistente.OnboardingCompletado)
        {
            return Result<CompleteOnboardingResponseDto>.Failure(
                Error.Conflict("Onboarding.AlreadyCompleted", "El onboarding del sistema ya ha sido completado anteriormente."));
        }

        // 2. Crear al usuario Encargado
        var (userSuccess, userId, userErrors) = await _identityService.CreateUserAsync(
            request.EncargadoEmail,
            request.EncargadoPassword,
            request.EncargadoNombreCompleto,
            "Encargado"
        );

        if (!userSuccess)
        {
            return Result<CompleteOnboardingResponseDto>.Failure(
                Error.Validation("Onboarding.UserCreationFailed", string.Join(" ", userErrors)));
        }

        // 3. Crear la Sede
        Sede sede;
        if (sedeExistente == null)
        {
            var sedeResult = Sede.Create(
                request.SedeNombre,
                request.SedeCodigo,
                request.SedeCiudad,
                request.SedeDireccion,
                onboardingCompletado: true
            );

            if (sedeResult.IsFailure)
            {
                return Result<CompleteOnboardingResponseDto>.Failure(sedeResult.Error);
            }

            sede = sedeResult.Value;
            _context.Sedes.Add(sede);
        }
        else
        {
            sede = sedeExistente;
            sede.Update(request.SedeNombre, request.SedeCodigo, request.SedeCiudad, request.SedeDireccion);
            sede.MarcarOnboardingCompletado();
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 4. Crear los Bloques, Aulas y Computadoras
        int totalAulasCreadas = 0;
        int totalComputadorasCreadas = 0;
        int bloqueIndex = 1;

        foreach (var bloqueDto in request.Bloques)
        {
            var bloqueResult = Bloque.Create(
                sede.Id,
                bloqueDto.Nombre,
                bloqueDto.Codigo,
                bloqueDto.TienePisos,
                bloqueDto.TotalPisos,
                bloqueDto.Descripcion
            );

            if (bloqueResult.IsFailure) continue;

            var bloque = bloqueResult.Value;
            _context.Bloques.Add(bloque);
            await _context.SaveChangesAsync(cancellationToken);

            int aulaIndex = 1;
            foreach (var aulaDto in bloqueDto.Aulas)
            {
                var aulaResult = Aula.Create(
                    aulaDto.Nombre,
                    aulaDto.Capacidad,
                    pabellon: $"{bloque.Nombre}{(string.IsNullOrWhiteSpace(aulaDto.Piso) ? "" : " - " + aulaDto.Piso)}",
                    minutosInactividad: aulaDto.MinutosInactividad,
                    bloqueId: bloque.Id,
                    piso: aulaDto.Piso
                );

                if (aulaResult.IsFailure) continue;

                var aula = aulaResult.Value;
                _context.Aulas.Add(aula);
                await _context.SaveChangesAsync(cancellationToken);
                totalAulasCreadas++;
                aulaIndex++;
            }
            bloqueIndex++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 5. Emitir Token JWT inmediato para el Encargado recién registrado
        string token = _jwtGenerator.GenerateToken(
            userId,
            request.EncargadoEmail.Trim().ToLowerInvariant(),
            "Encargado",
            new Dictionary<string, string>
            {
                { "NombreCompleto", request.EncargadoNombreCompleto.Trim() },
                { "SedeNombre", sede.Nombre }
            }
        );

        return Result<CompleteOnboardingResponseDto>.Success(new CompleteOnboardingResponseDto(
            sede.Id,
            sede.Nombre,
            request.EncargadoEmail.Trim().ToLowerInvariant(),
            request.EncargadoNombreCompleto.Trim(),
            token,
            request.Bloques.Count,
            totalAulasCreadas,
            totalComputadorasCreadas
        ));
    }
}
