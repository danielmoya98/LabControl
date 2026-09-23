using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LabControl.Application.Common.Interfaces;

namespace LabControl.Api.Controllers;

public record SedeDetalleDto(
    int Id,
    string Nombre,
    string Codigo,
    string Ciudad,
    string? Direccion,
    string? SegmentoRed,
    bool OnboardingCompletado,
    int TotalBloques,
    int TotalAulas
);

public record UpdateSedeRequest(
    string Nombre,
    string Codigo,
    string Ciudad,
    string? Direccion,
    string? SegmentoRed
);

public record UpdateSegmentoRedRequest(
    string? SegmentoRed
);

public class SedesController : ApiControllerBase
{
    private readonly IApplicationDbContext _context;

    public SedesController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetSedes(CancellationToken cancellationToken)
    {
        var sedes = await _context.Sedes
            .Include(s => s.Bloques)
                .ThenInclude(b => b.Aulas)
            .Select(s => new SedeDetalleDto(
                s.Id,
                s.Nombre,
                s.Codigo,
                s.Ciudad,
                s.Direccion,
                s.SegmentoRed,
                s.OnboardingCompletado,
                s.Bloques.Count,
                s.Bloques.SelectMany(b => b.Aulas).Count()
            ))
            .ToListAsync(cancellationToken);

        return Ok(sedes);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetSedeById(int id, CancellationToken cancellationToken)
    {
        var sede = await _context.Sedes
            .Include(s => s.Bloques)
                .ThenInclude(b => b.Aulas)
            .Where(s => s.Id == id)
            .Select(s => new SedeDetalleDto(
                s.Id,
                s.Nombre,
                s.Codigo,
                s.Ciudad,
                s.Direccion,
                s.SegmentoRed,
                s.OnboardingCompletado,
                s.Bloques.Count,
                s.Bloques.SelectMany(b => b.Aulas).Count()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (sede == null) return NotFound(new { error = $"Sede con ID {id} no encontrada." });
        return Ok(sede);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateSede(int id, [FromBody] UpdateSedeRequest request, CancellationToken cancellationToken)
    {
        var sede = await _context.Sedes.FindAsync([id], cancellationToken);
        if (sede == null) return NotFound(new { error = $"Sede con ID {id} no encontrada." });

        sede.Update(request.Nombre, request.Codigo, request.Ciudad, request.Direccion, request.SegmentoRed);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { success = true, message = "Sede actualizada exitosamente." });
    }

    [HttpPut("{id:int}/segmento-red")]
    public async Task<IActionResult> UpdateSegmentoRed(int id, [FromBody] UpdateSegmentoRedRequest request, CancellationToken cancellationToken)
    {
        var sede = await _context.Sedes.FindAsync([id], cancellationToken);
        if (sede == null) return NotFound(new { error = $"Sede con ID {id} no encontrada." });

        sede.UpdateSegmentoRed(request.SegmentoRed);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { success = true, segmentoRed = sede.SegmentoRed, message = "Segmento de red actualizado exitosamente." });
    }
}
