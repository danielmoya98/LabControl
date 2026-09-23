using MediatR;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;
using LabControl.Domain.Common;

namespace LabControl.Application.Features.Onboarding.Queries.GetOnboardingStatus;

public record OnboardingStatusDto(
    bool OnboardingCompletado,
    string? SedeNombre,
    string? SedeCodigo,
    string? SedeCiudad,
    int TotalBloques,
    int TotalAulas,
    int TotalComputadoras
);

public record GetOnboardingStatusQuery : IRequest<Result<OnboardingStatusDto>>;

public class GetOnboardingStatusQueryHandler : IRequestHandler<GetOnboardingStatusQuery, Result<OnboardingStatusDto>>
{
    private readonly IApplicationDbContext _context;

    public GetOnboardingStatusQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<OnboardingStatusDto>> Handle(GetOnboardingStatusQuery request, CancellationToken cancellationToken)
    {
        var sede = await _context.Sedes.FirstOrDefaultAsync(cancellationToken);

        if (sede == null)
        {
            return Result<OnboardingStatusDto>.Success(new OnboardingStatusDto(
                false,
                null,
                null,
                null,
                0,
                0,
                0
            ));
        }

        int totalBloques = await _context.Bloques.CountAsync(b => b.SedeId == sede.Id, cancellationToken);
        int totalAulas = await _context.Aulas.CountAsync(cancellationToken);
        int totalComputadoras = await _context.Computadoras.CountAsync(cancellationToken);

        return Result<OnboardingStatusDto>.Success(new OnboardingStatusDto(
            sede.OnboardingCompletado,
            sede.Nombre,
            sede.Codigo,
            sede.Ciudad,
            totalBloques,
            totalAulas,
            totalComputadoras
        ));
    }
}
