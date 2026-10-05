using LabControl.Domain.Enums;

namespace LabControl.Api.Hubs;

public interface ILaboratorioClient
{
    Task RecibirEstadoComputadora(int computadoraId, string hostname, EstadoComputadora nuevoEstado, string? emailEstudiante);
    Task RecibirAlertaTerminal(string mensaje);
    Task RecibirComandoCierreSesion(TipoCierreSesion motivo);
    Task RecibirComandoEnergia(string tipoComando, string motivo);
    Task RecibirTelemetria(int computadoraId, string hostname, int cpuUso, int ramUso, int discoLibreGb);
    Task RecibirMiniaturaPantalla(int computadoraId, string hostname, string imagenBase64);
    Task RecibirActualizacionPoliticaAula(int aulaId, string aulaNombre, int minutosInactividad, int accionInactividad);
    Task RecibirComandoModoEvento(int aulaId, bool activar, string motivo, int duracionMinutos);
    Task RecibirComandoFreeze(string accion, string? claveSeguridad);
    Task RecibirComandoActualizacion(string urlDescarga, string nuevaVersion, string sha256);
    Task SolicitarHeartbeat();
}
