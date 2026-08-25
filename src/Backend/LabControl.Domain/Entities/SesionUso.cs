using LabControl.Domain.Common;
using LabControl.Domain.Enums;
using LabControl.Domain.ValueObjects;

namespace LabControl.Domain.Entities;

public class SesionUso : AggregateRoot
{
    public int ComputadoraId { get; private set; }
    public Computadora Computadora { get; private set; } = default!;
    public string EmailEstudiante { get; private set; } = default!;
    public DateTime FechaHoraInicio { get; private set; }
    public DateTime? FechaHoraFin { get; private set; }
    public int? DuracionMinutos { get; private set; }
    public TipoCierreSesion TipoCierre { get; private set; }
    public SyncStatus SyncStatus { get; private set; } = SyncStatus.Online;
    public DateTime FechaSincronizacion { get; private set; } = DateTime.UtcNow;

    private SesionUso() { }

    public static Result<SesionUso> Iniciar(int computadoraId, EmailInstitucional email, DateTime? fechaInicio = null, SyncStatus syncStatus = SyncStatus.Online)
    {
        if (computadoraId <= 0)
        {
            return Result<SesionUso>.Failure(Error.Validation("SesionUso.ComputadoraRequired", "Debe indicar la computadora."));
        }

        var sesion = new SesionUso
        {
            ComputadoraId = computadoraId,
            EmailEstudiante = email.Value,
            FechaHoraInicio = fechaInicio ?? DateTime.UtcNow,
            TipoCierre = TipoCierreSesion.Manual,
            SyncStatus = syncStatus,
            FechaSincronizacion = DateTime.UtcNow
        };

        return Result<SesionUso>.Success(sesion);
    }

    public void Finalizar(TipoCierreSesion tipoCierre, DateTime? fechaFin = null)
    {
        FechaHoraFin = fechaFin ?? DateTime.UtcNow;
        TipoCierre = tipoCierre;
        DuracionMinutos = (int)Math.Max(0, (FechaHoraFin.Value - FechaHoraInicio).TotalMinutes);
        FechaModificacionUtc = DateTime.UtcNow;
    }
}
