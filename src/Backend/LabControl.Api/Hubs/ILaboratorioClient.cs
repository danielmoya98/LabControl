using LabControl.Domain.Enums;

namespace LabControl.Api.Hubs;

public interface ILaboratorioClient
{
    Task RecibirEstadoComputadora(int computadoraId, string hostname, EstadoComputadora nuevoEstado, string? emailEstudiante);
    Task RecibirAlertaTerminal(string mensaje);
    Task RecibirComandoCierreSesion(TipoCierreSesion motivo);
    Task RecibirComandoEnergia(string tipoComando, string motivo);
    Task RecibirTelemetria(int computadoraId, string hostname, int cpuUso, int ramUso, int discoLibreGb);
}
